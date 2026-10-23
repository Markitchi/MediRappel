using System.Collections.ObjectModel;
using MediRappel.Models;
using MediRappel.Services;

namespace MediRappel.Vues;

public partial class VueAccueil : ContentView, IRafraichir
{
    public ObservableCollection<Dose> Doses { get; } = new();

    Dose? _doseManquee;
    int _rdvId;

    public VueAccueil()
    {
        InitializeComponent();
        BindingContext = this;
    }

    // ---- Propriétés liées ----
    public string DateTitre { get; private set; } = "";
    public string ResumePrises { get; private set; } = "";
    public string SousTitrePrises { get; private set; } = "";
    public string PctTexte { get; private set; } = "";
    public double PctNorm { get; private set; }
    public string ProchainePrise { get; private set; } = "";
    public bool AlerteVisible { get; private set; }
    public string AlerteTexte { get; private set; } = "";
    public bool RdvVisible { get; private set; }
    public string RdvJour { get; private set; } = "";
    public string RdvMois { get; private set; } = "";
    public string RdvLigne1 { get; private set; } = "";
    public string RdvSpecialite { get; private set; } = "";
    public string RdvLieu { get; private set; } = "";
    public string RdvJours { get; private set; } = "";
    public bool EnChargement { get; private set; }
    public bool AucuneDose => !EnChargement && Doses.Count == 0;

    public async Task RafraichirAsync()
    {
        var jour = DateTime.Today;
        DateTitre = $"AUJOURD'HUI · {Texte.Capitaliser(jour.ToString("dddd d MMMM")).ToUpperInvariant()}";

        EnChargement = true;
        RafraichirProprietes();

        List<Dose> doses = new();
        try { doses = await Medi.Data.Doses(jour); }
        catch (ApiException ex) { await Ui.Avertir("Données indisponibles", ex.Message); }

        Doses.Clear();
        foreach (var d in doses) Doses.Add(d);
        MajJour();

        try
        {
            var rdvs = await Medi.Data.RendezVousListe();
            var prochain = rdvs.Where(r => !r.Passe).OrderBy(r => r.Date).FirstOrDefault();
            if (prochain is not null)
            {
                _rdvId = prochain.Id;
                RdvJour = prochain.JourNum;
                RdvMois = prochain.MoisAbrev;
                RdvLigne1 = $"{prochain.Heure} · {prochain.Medecin}";
                RdvSpecialite = prochain.Specialite;
                RdvLieu = prochain.Lieu;
                RdvJours = prochain.JoursRestants;
                RdvVisible = true;
            }
            else RdvVisible = false;
        }
        catch (ApiException) { RdvVisible = false; }

        EnChargement = false;
        RafraichirProprietes();
    }

    void MajJour()
    {
        int total = Doses.Count;
        int faites = Doses.Count(d => d.Etat == EtatDose.Faite);
        int pct = total == 0 ? 0 : (int)Math.Round(100.0 * faites / total);

        ResumePrises = $"{faites} sur {total} prise{(total > 1 ? "s" : "")}";
        SousTitrePrises = $"{pct} % des prises du jour";
        PctTexte = $"{pct} %";
        PctNorm = total == 0 ? 0 : (double)faites / total;

        var suivante = Doses.Where(d => d.Etat == EtatDose.Avenir).OrderBy(d => d.Heure).FirstOrDefault();
        ProchainePrise = suivante is not null
            ? $"Prochaine prise à {suivante.Heure}"
            : (total == 0 ? "Aucune prise prévue" : "Toutes les prises sont faites");

        _doseManquee = Doses.FirstOrDefault(d => d.Etat == EtatDose.Manquee);
        AlerteVisible = _doseManquee is not null;
        AlerteTexte = _doseManquee is not null
            ? $"Dose « {_doseManquee.Titre} » de {_doseManquee.Heure} manquée. Touchez « Corriger »."
            : "";
    }

    void RafraichirProprietes()
    {
        OnPropertyChanged(nameof(DateTitre));
        OnPropertyChanged(nameof(ResumePrises));
        OnPropertyChanged(nameof(SousTitrePrises));
        OnPropertyChanged(nameof(PctTexte));
        OnPropertyChanged(nameof(PctNorm));
        OnPropertyChanged(nameof(ProchainePrise));
        OnPropertyChanged(nameof(AlerteVisible));
        OnPropertyChanged(nameof(AlerteTexte));
        OnPropertyChanged(nameof(RdvVisible));
        OnPropertyChanged(nameof(RdvJour));
        OnPropertyChanged(nameof(RdvMois));
        OnPropertyChanged(nameof(RdvLigne1));
        OnPropertyChanged(nameof(RdvSpecialite));
        OnPropertyChanged(nameof(RdvLieu));
        OnPropertyChanged(nameof(RdvJours));
        OnPropertyChanged(nameof(EnChargement));
        OnPropertyChanged(nameof(AucuneDose));
    }

    async void VersPrises(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("prises");

    async void VersNouvellePrise(object? s, EventArgs e) =>
        await Shell.Current!.GoToAsync("nouvelle-prise");

    async void VersReglages(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("reglages");

    async void VersRdv(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync($"rdv?id={_rdvId}");

    async void Corriger(object? s, TappedEventArgs e)
    {
        if (_doseManquee is null) return;
        await ActionsDose.Marquer(_doseManquee);
        await RafraichirAsync();
    }

    async void DoseTapped(object? s, TappedEventArgs e)
    {
        if (Trouver<Dose>(s) is not { } dose) return;
        await ActionsDose.Marquer(dose);
        await RafraichirAsync();
    }

    internal static T? Trouver<T>(object? source) where T : class
    {
        var v = source as VisualElement;
        while (v is not null)
        {
            if (v.BindingContext is T t) return t;
            v = v.Parent as VisualElement;
        }
        return null;
    }
}
