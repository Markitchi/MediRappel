using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediRappel.Models;

namespace MediRappel.Services;

public record LoginRequest(string Email, string MotDePasse);
public record RegisterRequest(string Email, string Nom, string MotDePasse);
public record AuthResponse(string Token, string Email, string Nom, int Id);

public record MedicamentSaisie(string Nom, string Dosage, string Posologie, string Depuis);
public record OrdonnanceSaisie(
    string Medecin, string Specialite, string Cabinet, DateTime Date,
    string Statut, string Resume, string Periode, bool Favori, List<MedicamentSaisie> Medicaments);
public record RendezVousSaisie(
    DateTime Date, string Medecin, string Specialite, string Lieu,
    string Motif, string Notes, string Telephone, bool Rappel);

/// <summary>Saisie d'une prise. <c>Jours</c> répète la prise chaque jour (planification).</summary>
public record DoseSerieSaisie(
    string Medicament, string Dosage, string Posologie, List<string> Heures, string Detail, DateTime Date,
    int Jours = 1);

/// <summary>Erreur métier renvoyée par l'API (message affichable tel quel).</summary>
public sealed class ApiException(string message) : Exception(message);

public interface IApiClient
{
    void DefinirToken(string? token);
    Task<AuthResponse> Login(LoginRequest req, CancellationToken ct = default);
    Task<AuthResponse> Register(RegisterRequest req, CancellationToken ct = default);
    Task<bool> MotDePasseOublie(string email, CancellationToken ct = default);
    Task<Profil> Profil(CancellationToken ct = default);

    Task<List<Ordonnance>> Ordonnances(CancellationToken ct = default);
    Task<Ordonnance> Ordonnance(int id, CancellationToken ct = default);
    Task<Ordonnance> CreerOrdonnance(OrdonnanceSaisie saisie, CancellationToken ct = default);
    Task SupprimerOrdonnance(int id, CancellationToken ct = default);

    Task<List<Dose>> Doses(DateTime jour, CancellationToken ct = default);
    Task<List<Dose>> DosesAujourdhui(CancellationToken ct = default);
    Task<List<Dose>> CreerDosesSerie(DoseSerieSaisie saisie, CancellationToken ct = default);
    Task<Dose> MarquerDose(int id, EtatDose etat, CancellationToken ct = default);

    Task<List<RendezVous>> RendezVousListe(CancellationToken ct = default);
    Task<RendezVous> RendezVous(int id, CancellationToken ct = default);
    Task<RendezVous> CreerRendezVous(RendezVousSaisie saisie, CancellationToken ct = default);
    Task<RendezVous> ModifierRendezVous(int id, RendezVousSaisie saisie, CancellationToken ct = default);
    Task SupprimerRendezVous(int id, CancellationToken ct = default);

    Task<Historique> Historique(int jours, CancellationToken ct = default);
    Task<Reglage> Reglages(CancellationToken ct = default);
    Task<Reglage> EnregistrerReglages(Reglage reglage, CancellationToken ct = default);
}

public sealed class ApiClient(HttpClient http) : IApiClient
{
    public const string UrlEmulateurAndroid = "http://10.0.2.2:5050";
    public const string UrlLocale = "http://127.0.0.1:5050";

    /// <summary>Adresse de base selon la cible : l'émulateur Android atteint la
    /// machine hôte via 10.0.2.2, les autres cibles via 127.0.0.1.</summary>
    public static string UrlParDefaut =>
#if ANDROID
        DeviceInfo.DeviceType == DeviceType.Virtual ? UrlEmulateurAndroid : UrlLocale;
#else
        UrlLocale;
#endif

    public void DefinirToken(string? token)
    {
        http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<AuthResponse> Login(LoginRequest req, CancellationToken ct = default)
    {
        var r = await http.PostAsJsonAsync("/api/auth/login", req, ct);
        return await Lire<AuthResponse>(r);
    }

    public async Task<AuthResponse> Register(RegisterRequest req, CancellationToken ct = default)
    {
        var r = await http.PostAsJsonAsync("/api/auth/register", req, ct);
        return await Lire<AuthResponse>(r);
    }

    public async Task<bool> MotDePasseOublie(string email, CancellationToken ct = default)
    {
        var r = await http.PostAsJsonAsync("/api/auth/motdepasse-oublie", new { email }, ct);
        await Verifier(r);
        return true;
    }

    public Task<Profil> Profil(CancellationToken ct = default) =>
        Lire<Profil>(http, "/api/auth/moi", ct);

    public Task<List<Ordonnance>> Ordonnances(CancellationToken ct = default) =>
        Lire<List<Ordonnance>>(http, "/api/ordonnances", ct);

    public Task<Ordonnance> Ordonnance(int id, CancellationToken ct = default) =>
        Lire<Ordonnance>(http, $"/api/ordonnances/{id}", ct);

    public async Task<Ordonnance> CreerOrdonnance(OrdonnanceSaisie saisie, CancellationToken ct = default)
    {
        var r = await http.PostAsJsonAsync("/api/ordonnances", saisie, ct);
        return await Lire<Ordonnance>(r);
    }

    public async Task SupprimerOrdonnance(int id, CancellationToken ct = default)
    {
        var r = await http.DeleteAsync($"/api/ordonnances/{id}", ct);
        await Verifier(r);
    }

    public Task<List<Dose>> Doses(DateTime jour, CancellationToken ct = default) =>
        Lire<List<Dose>>(http, $"/api/doses?date={jour:yyyy-MM-dd}", ct);

    public Task<List<Dose>> DosesAujourdhui(CancellationToken ct = default) =>
        Lire<List<Dose>>(http, "/api/doses/aujourdhui", ct);

    public async Task<List<Dose>> CreerDosesSerie(DoseSerieSaisie saisie, CancellationToken ct = default)
    {
        var r = await http.PostAsJsonAsync("/api/doses/serie", saisie, ct);
        return await Lire<List<Dose>>(r);
    }

    public async Task<Dose> MarquerDose(int id, EtatDose etat, CancellationToken ct = default)
    {
        var r = await http.PutAsJsonAsync($"/api/doses/{id}/etat", new { etat = (int)etat }, ct);
        return await Lire<Dose>(r);
    }

    public Task<List<RendezVous>> RendezVousListe(CancellationToken ct = default) =>
        Lire<List<RendezVous>>(http, "/api/rendezvous", ct);

    public Task<RendezVous> RendezVous(int id, CancellationToken ct = default) =>
        Lire<RendezVous>(http, $"/api/rendezvous/{id}", ct);

    public async Task<RendezVous> CreerRendezVous(RendezVousSaisie saisie, CancellationToken ct = default)
    {
        var r = await http.PostAsJsonAsync("/api/rendezvous", saisie, ct);
        return await Lire<RendezVous>(r);
    }

    public async Task<RendezVous> ModifierRendezVous(int id, RendezVousSaisie saisie, CancellationToken ct = default)
    {
        var r = await http.PutAsJsonAsync($"/api/rendezvous/{id}", saisie, ct);
        return await Lire<RendezVous>(r);
    }

    public async Task SupprimerRendezVous(int id, CancellationToken ct = default)
    {
        var r = await http.DeleteAsync($"/api/rendezvous/{id}", ct);
        await Verifier(r);
    }

    public Task<Historique> Historique(int jours, CancellationToken ct = default) =>
        Lire<Historique>(http, $"/api/historique?jours={jours}", ct);

    public Task<Reglage> Reglages(CancellationToken ct = default) =>
        Lire<Reglage>(http, "/api/reglages", ct);

    public async Task<Reglage> EnregistrerReglages(Reglage reglage, CancellationToken ct = default)
    {
        var r = await http.PutAsJsonAsync("/api/reglages", reglage, ct);
        return await Lire<Reglage>(r);
    }

    // ---------- Helpers ----------

    static async Task<T> Lire<T>(HttpClient http, string url, CancellationToken ct)
        => await Lire<T>(await http.GetAsync(url, ct));

    static async Task<T> Lire<T>(HttpResponseMessage r)
    {
        await Verifier(r);
        var valeur = await r.Content.ReadFromJsonAsync<T>();
        if (valeur is null) throw new ApiException("Réponse vide du serveur.");
        return valeur;
    }

    static async Task Verifier(HttpResponseMessage r)
    {
        if (r.IsSuccessStatusCode) return;
        var corps = await r.Content.ReadAsStringAsync();
        string message = r.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Session expirée, veuillez vous reconnecter.",
            HttpStatusCode.NotFound => "Élément introuvable.",
            _ => "Une erreur est survenue.",
        };
        try
        {
            using var doc = JsonDocument.Parse(corps);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.GetString() is { Length: > 0 } m)
                message = m;
        }
        catch (JsonException) { /* corps non JSON : on garde le message par défaut */ }
        throw new ApiException(message);
    }
}
