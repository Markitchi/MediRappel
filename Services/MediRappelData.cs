using MediRappel.Models;

namespace MediRappel.Services;

/// <summary>Point d'accès unique aux services (client API + données + session).</summary>
public static class Medi
{
    public static IApiClient Api { get; set; } = null!;
    public static MediRappelData Data { get; set; } = null!;

#if ANDROID
    public static NotificationService? Notification { get; set; }
#endif
}

/// <summary>
/// Dépôt de données « online-first + cache hors ligne ».
/// Lecture : l'API d'abord, le cache JSON local en secours si le réseau manque.
/// Écriture : l'API ; en cas d'absence de réseau, l'action est conservée en
/// attente puis rejouée à la prochaine occasion (marquage des prises).
/// </summary>
public sealed class MediRappelData(IApiClient api)
{
    const string CleOrdonnances = "ordonnances";
    const string CleRendezVous = "rendezvous";
    const string CleProfil = "profil";
    const string CleReglages = "reglages";
    const string CleAttentes = "doses_attente";

    sealed class AttenteDose
    {
        public int Id { get; set; }
        public EtatDose Etat { get; set; }
        public DateTime Jour { get; set; }
    }

    public static bool EnLigne => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    static string CleDoses(DateTime jour) => $"doses_{jour:yyyyMMdd}";

    // ---------------- Ordonnances ----------------
    public async Task<List<Ordonnance>> Ordonnances()
    {
        try
        {
            var liste = await api.Ordonnances();
            CacheLocal.Sauver(CleOrdonnances, liste);
            return liste;
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return CacheLocal.Lire<List<Ordonnance>>(CleOrdonnances) ?? new();
        }
    }

    public Task<Ordonnance> Ordonnance(int id) => api.Ordonnance(id);

    public async Task<Ordonnance> CreerOrdonnance(OrdonnanceSaisie saisie)
    {
        var o = await api.CreerOrdonnance(saisie);
        Invalider(CleOrdonnances);
        return o;
    }

    public async Task SupprimerOrdonnance(int id)
    {
        await api.SupprimerOrdonnance(id);
        Invalider(CleOrdonnances);
    }

    // ---------------- Prises / doses ----------------
    public async Task<List<Dose>> Doses(DateTime jour)
    {
        try
        {
            await RejouerAttentes();
            var liste = await api.Doses(jour);
            CacheLocal.Sauver(CleDoses(jour), liste);
            return liste;
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return CacheLocal.Lire<List<Dose>>(CleDoses(jour)) ?? new();
        }
    }

    public Task<List<Dose>> DosesAujourdhui() => Doses(DateTime.Today);

    /// <summary>Ajoute une série de prises (plusieurs heures par jour, répétées sur
    /// <c>Jours</c> jours). Écriture en ligne uniquement : une prise créée doit
    /// avoir un identifiant serveur.</summary>
    public async Task<List<Dose>> AjouterPrisesSerie(DoseSerieSaisie saisie)
    {
        var creees = await api.CreerDosesSerie(saisie);
        int jours = saisie.Jours <= 0 ? 1 : saisie.Jours;
        for (int i = 0; i < jours; i++)
            Invalider(CleDoses(saisie.Date.Date.AddDays(i)));
        foreach (int p in new[] { 7, 30, 90 })
            Invalider($"historique_{p}");
        return creees;
    }

    /// <summary>Marque une prise. Met à jour le cache immédiatement ; si le
    /// réseau est absent, l'opération est mise en attente et rejouée plus tard.</summary>
    public async Task MarquerDose(Dose dose, EtatDose etat)
    {
        AppliquerLocalement(dose, etat);
        MettreAJourCache(dose.Date, dose);
        try
        {
            var maj = await api.MarquerDose(dose.Id, etat);
            dose.Etat = maj.Etat;
            dose.Validee = maj.Validee;
            dose.DatePrise = maj.DatePrise;
            MettreAJourCache(dose.Date, dose);
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            MettreEnAttente(dose, etat);
        }
    }

    static void AppliquerLocalement(Dose dose, EtatDose etat)
    {
        dose.Etat = etat;
        dose.DatePrise = etat is EtatDose.Faite or EtatDose.Partielle ? DateTime.Now : null;
        dose.Validee = etat is EtatDose.Faite or EtatDose.Partielle
            ? DateTime.Now.ToString("HH:mm")
            : "";
    }

    void MettreEnAttente(Dose dose, EtatDose etat)
    {
        var liste = CacheLocal.Lire<List<AttenteDose>>(CleAttentes) ?? new();
        liste.RemoveAll(a => a.Id == dose.Id);
        liste.Add(new AttenteDose { Id = dose.Id, Etat = etat, Jour = dose.Date.Date });
        CacheLocal.Sauver(CleAttentes, liste);
    }

    /// <summary>Rejoue les marquages effectués hors connexion.</summary>
    public async Task RejouerAttentes()
    {
        if (!EnLigne) return;
        var liste = CacheLocal.Lire<List<AttenteDose>>(CleAttentes);
        if (liste is null || liste.Count == 0) return;

        var restantes = new List<AttenteDose>();
        foreach (var a in liste)
        {
            try
            {
                var maj = await api.MarquerDose(a.Id, a.Etat);
                MettreAJourCache(a.Jour, maj);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                restantes.Add(a);
            }
            catch (ApiException) { /* dose supprimée côté serveur : on abandonne */ }
        }
        CacheLocal.Sauver(CleAttentes, restantes);
    }

    static void MettreAJourCache(DateTime jour, Dose maj)
    {
        var cache = CacheLocal.Lire<List<Dose>>(CleDoses(jour));
        if (cache is null) return;
        var i = cache.FindIndex(d => d.Id == maj.Id);
        if (i >= 0) cache[i] = maj;
        CacheLocal.Sauver(CleDoses(jour), cache);
    }

    // ---------------- Rendez-vous ----------------
    public async Task<List<RendezVous>> RendezVousListe()
    {
        try
        {
            var liste = await api.RendezVousListe();
            CacheLocal.Sauver(CleRendezVous, liste);
            return liste;
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return CacheLocal.Lire<List<RendezVous>>(CleRendezVous) ?? new();
        }
    }

    public Task<RendezVous> RendezVous(int id) => api.RendezVous(id);

    public async Task<RendezVous> EnregistrerRendezVous(int? id, RendezVousSaisie saisie)
    {
        var rdv = id is null ? await api.CreerRendezVous(saisie) : await api.ModifierRendezVous(id.Value, saisie);
        Invalider(CleRendezVous);
        return rdv;
    }

    public async Task SupprimerRendezVous(int id)
    {
        await api.SupprimerRendezVous(id);
        CacheLocal.Vider(); // simple et fiable : les caches se rechargeront à la demande
    }

    // ---------------- Historique ----------------
    public async Task<Historique> Historique(int jours)
    {
        try
        {
            var h = await api.Historique(jours);
            CacheLocal.Sauver($"historique_{jours}", h);
            return h;
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return CacheLocal.Lire<Historique>($"historique_{jours}") ?? new Historique();
        }
    }

    // ---------------- Profil ----------------
    public async Task<Profil> Profil()
    {
        try
        {
            var p = await api.Profil();
            CacheLocal.Sauver(CleProfil, p);
            return p;
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return CacheLocal.Lire<Profil>(CleProfil)
                   ?? new Profil { Id = Session.UtilisateurId, Nom = Session.Nom, Email = Session.Email };
        }
    }

    // ---------------- Réglages ----------------
    public async Task<Reglage> Reglages()
    {
        try
        {
            var r = await api.Reglages();
            CacheLocal.Sauver(CleReglages, r);
            return r;
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return CacheLocal.Lire<Reglage>(CleReglages) ?? new Reglage();
        }
    }

    public async Task<Reglage> EnregistrerReglages(Reglage reglage)
    {
        CacheLocal.Sauver(CleReglages, reglage);
        try
        {
            var r = await api.EnregistrerReglages(reglage);
            CacheLocal.Sauver(CleReglages, r);
            return r;
        }
        catch (ApiException) { throw; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return reglage;
        }
    }

    // ---------------- Divers ----------------
    static void Invalider(string cle) => CacheLocal.Supprimer(cle);
}
