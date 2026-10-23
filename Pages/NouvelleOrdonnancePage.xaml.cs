using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MediRappel.Models;
using MediRappel.Services;
using MediRappel.Vues;

namespace MediRappel.Pages;

/// <summary>Ligne de saisie d'un médicament (nouvelle ordonnance).
/// Le compteur −/+ et les heures éditables définissent les prises à planifier
/// à l'enregistrement (0 = aucune prise).</summary>
public sealed class LigneMedicament : INotifyPropertyChanged
{
    public int Index { get; set; }

    string _nom = "", _dosage = "", _posologie = "";
    public string Nom { get => _nom; set => Set(ref _nom, value); }
    public string Dosage { get => _dosage; set => Set(ref _dosage, value); }
    public string Posologie { get => _posologie; set => Set(ref _posologie, value); }

    public ObservableCollection<PriseHoraire> Prises { get; } = new();

    public int NbPrises => Prises.Count;
    public bool Plusieurs => Prises.Count > 1;
    public bool AucunePrise => Prises.Count == 0;
    public bool PeutAjouter => Prises.Count < PriseHoraire.MaxParJour;
    public bool PeutRetirer => Prises.Count > 0;

    public string Numero => $"Médicament {Index}";

    /// <summary>Heures saisies (format « HH:mm »), triées, ou liste vide.</summary>
    public List<string> Heures() =>
        Prises.Select(p => p.Libelle).Distinct(StringComparer.Ordinal)
              .OrderBy(h => h, StringComparer.Ordinal).ToList();

    /// <summary>Met à jour les compteurs affichés après ajout/suppression d'une heure.</summary>
    public void MajPrises()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NbPrises)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Plusieurs)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AucunePrise)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutAjouter)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutRetirer)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void Set<T>(ref T champ, T valeur, [CallerMemberName] string? nom = null)
    {
        if (EqualityComparer<T>.Default.Equals(champ, valeur)) return;
        champ = valeur;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
    }
}

public partial class NouvelleOrdonnancePage : ContentPage
{
    public NouvelleOrdonnancePage()
    {
        InitializeComponent();
        BindingContext = this;
        Ajouter();
    }

    public ObservableCollection<LigneMedicament> Lignes { get; } = new();
    public string[] Statuts { get; } = { "Active", "Terminée" };

    /// <summary>Durée du planning des prises créées à partir des créneaux cochés.</summary>
    public List<string> Durees { get; } = new() { "7 jours", "30 jours", "90 jours" };

    string _duree = "30 jours";
    public string Duree
    {
        get => _duree;
        set { _duree = value; OnPropertyChanged(); }
    }

    int DureeJours => _duree switch { "7 jours" => 7, "90 jours" => 90, _ => 30 };

    public string Medecin { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string Cabinet { get; set; } = "";
    public DateTime DateOrdonnance { get; set; } = DateTime.Today;
    public string Statut { get; set; } = "Active";
    public string Periode { get; set; } = "";
    public string Resume { get; set; } = "";
    public bool Favori { get; set; }

    public bool MedecinErreur { get; private set; }
    public bool MedsErreur { get; private set; }
    public bool ErreurVisible { get; private set; }
    public string ErreurMessage { get; private set; } = "";

    bool _occupe;
    public bool NonOccupe => !_occupe;
    public string BoutonTexte => _occupe ? "Enregistrement…" : "Enregistrer l'ordonnance";

    protected override void OnAppearing()
    {
        base.OnAppearing();
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
    }

    void AjouterLigne(object? s, TappedEventArgs e) => Ajouter();
    void AjouterLigneBouton(object? s, EventArgs e) => Ajouter();

    /// <summary>Ajoute une heure de prise sur un médicament (heure par défaut 08:00).</summary>
    void AjouterCreneau(object? s, EventArgs e)
    {
        if (VueAccueil.Trouver<LigneMedicament>(s) is not { } ligne) return;
        if (ligne.Prises.Count >= PriseHoraire.MaxParJour) return;
        ligne.Prises.Add(PriseHoraire.Creer(ligne.Prises.Select(p => p.Heure)));
        ligne.MajPrises();
    }

    /// <summary>Retire la dernière heure de prise d'un médicament.</summary>
    void RetirerCreneau(object? s, EventArgs e)
    {
        if (VueAccueil.Trouver<LigneMedicament>(s) is not { } ligne) return;
        if (ligne.Prises.Count == 0) return;
        ligne.Prises.RemoveAt(ligne.Prises.Count - 1);
        ligne.MajPrises();
    }

    /// <summary>Applique un créneau rapide (matin/midi/soir/coucher) à l'heure de la ligne.</summary>
    void Presets(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<PriseHoraire>(s) is not { } p) return;
        if (TimeSpan.TryParse(e.Parameter as string, out var t)) p.Heure = t;
    }

    void Ajouter()
    {
        Lignes.Add(new LigneMedicament());
        Renumeroter();
    }

    void SupprimerLigne(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<LigneMedicament>(s) is not { } ligne) return;
        if (Lignes.Count == 1) return; // toujours au moins une ligne à l'écran
        Lignes.Remove(ligne);
        Renumeroter();
    }

    void Renumeroter()
    {
        var copie = Lignes.ToList();
        Lignes.Clear();
        for (int i = 0; i < copie.Count; i++)
        {
            copie[i].Index = i + 1;
            Lignes.Add(copie[i]);
        }
    }

    async void Enregistrer(object? s, EventArgs e)
    {
        MedecinErreur = string.IsNullOrWhiteSpace(Medecin);
        var meds = Lignes.Where(l => !string.IsNullOrWhiteSpace(l.Nom)).ToList();
        MedsErreur = meds.Count == 0;
        ErreurVisible = false;
        Notifier();
        if (MedecinErreur || MedsErreur) return;

        if (!MediRappelData.EnLigne)
        {
            Signaler("La création d'une ordonnance nécessite une connexion.");
            return;
        }

        _occupe = true;
        Notifier();

        try
        {
            var saisie = new OrdonnanceSaisie(
                Medecin.Trim(), Specialite.Trim(), Cabinet.Trim(), DateOrdonnance.Date,
                Statut, Resume.Trim(), Periode.Trim(), Favori,
                meds.Select(m => new MedicamentSaisie(
                    m.Nom.Trim(), m.Dosage.Trim(), m.Posologie.Trim(), "")).ToList());

            await Medi.Data.CreerOrdonnance(saisie);

            // Planifie les prises des médicaments dont un créneau a été coché.
            var (planifiees, echecs) = await PlanifierPrises(meds);

            await Ui.Avertir("Ordonnance ajoutée", MessageFin(planifiees, echecs));
            await Shell.Current!.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            Signaler(ex.Message);
        }
        catch (Exception)
        {
            Signaler("Serveur injoignable. Vérifiez votre connexion, puis réessayez.");
        }
        finally
        {
            _occupe = false;
            Notifier();
        }
    }

    /// <summary>Crée les prises des heures saisies (répétées sur la durée choisie).
    /// Renvoie le nombre de prises planifiées et le nombre d'échecs.</summary>
    async Task<(int planifiees, int echecs)> PlanifierPrises(List<LigneMedicament> meds)
    {
        int planifiees = 0, echecs = 0;
        int jours = DureeJours;

        foreach (var m in meds)
        {
            var heures = m.Heures();
            if (heures.Count == 0) continue; // 0 prise : aucun planning

            try
            {
                var creees = await Medi.Data.AjouterPrisesSerie(new DoseSerieSaisie(
                    m.Nom.Trim(), m.Dosage.Trim(), m.Posologie.Trim(), heures, "À prendre",
                    DateTime.Today, jours));
                planifiees += creees.Count;
            }
            catch (Exception) { echecs++; }
        }

        return (planifiees, echecs);
    }

    string MessageFin(int planifiees, int echecs)
    {
        if (echecs == 0 && planifiees == 0)
            return "L'ordonnance a été enregistrée.";
        if (echecs == 0)
            return $"L'ordonnance a été enregistrée · {Texte.Pluriel(planifiees, "prise planifiée", "prises planifiées")}.";
        if (planifiees > 0)
            return "L'ordonnance est enregistrée, mais certaines prises n'ont pas pu être planifiées.";
        return "L'ordonnance est enregistrée, mais les prises n'ont pas pu être planifiées.";
    }

    void Signaler(string message)
    {
        ErreurMessage = message;
        ErreurVisible = true;
        Notifier();
    }

    void Notifier()
    {
        OnPropertyChanged(nameof(MedecinErreur));
        OnPropertyChanged(nameof(MedsErreur));
        OnPropertyChanged(nameof(ErreurVisible));
        OnPropertyChanged(nameof(ErreurMessage));
        OnPropertyChanged(nameof(NonOccupe));
        OnPropertyChanged(nameof(BoutonTexte));
    }

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");
}
