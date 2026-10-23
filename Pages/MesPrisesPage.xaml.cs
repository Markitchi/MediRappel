using System.Collections.ObjectModel;
using MediRappel.Models;
using MediRappel.Services;
using MediRappel.Vues;

namespace MediRappel.Pages;

public partial class MesPrisesPage : ContentPage
{
    DateTime _jour = DateTime.Today;
    Reglage _reglage = new();
    bool _chargement;

    public ObservableCollection<JourItem> Jours { get; } = new();
    public ObservableCollection<Dose> Doses { get; } = new();

    public MesPrisesPage()
    {
        InitializeComponent();
        BindingContext = this;
        nav.DestinationChoisie += async (_, idx) => await AppShell.AllerOnglet(idx);
        fabAjout.ActionCliquee += async (_, _) => await Shell.Current!.GoToAsync("nouvelle-prise");
    }

    public string TitreJour { get; private set; } = "";
    public string TraitementTitre { get; private set; } = "";
    public string TraitementDetail { get; private set; } = "";
    public bool AucuneDose { get; private set; }

    public bool RappelsActifs { get; set; } = true;
    public string PlageSilence { get; private set; } = "22:00 – 07:00";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
        ConstruireSemaine();
        await ChargerJour(_jour);
        await ChargerReglages();
    }

    void ConstruireSemaine()
    {
        var lundi = _jour.AddDays(-(((int)_jour.DayOfWeek + 6) % 7));
        Jours.Clear();
        for (int i = 0; i < 7; i++)
        {
            var d = lundi.AddDays(i);
            Jours.Add(new JourItem { Date = d, Selectionne = d == _jour.Date });
        }
    }

    async Task ChargerJour(DateTime jour)
    {
        _jour = jour.Date;
        List<Dose> doses = new();
        try { doses = await Medi.Data.Doses(_jour); }
        catch (ApiException ex) { await Ui.Avertir("Données indisponibles", ex.Message); }

        Doses.Clear();
        foreach (var d in doses) Doses.Add(d);
        ConstruireSemaine();

        var meds = doses.Select(d => d.Medicament).Distinct().Count();
        TitreJour = Texte.Capitaliser(_jour.ToString("dddd d MMMM yyyy"));
        TraitementTitre = doses.Count == 0
            ? "Aucune prise planifiée"
            : $"{Texte.Pluriel(doses.Count, "prise")} · {Texte.Pluriel(meds, "médicament")}";
        TraitementDetail = _jour == DateTime.Today
            ? "Programme du jour"
            : $"Journée du {_jour:dd/MM/yyyy}";
        AucuneDose = doses.Count == 0;

        ConstruireSemaine();
        OnPropertyChanged(nameof(TitreJour));
        OnPropertyChanged(nameof(TraitementTitre));
        OnPropertyChanged(nameof(TraitementDetail));
        OnPropertyChanged(nameof(AucuneDose));
    }

    async Task ChargerReglages()
    {
        _chargement = true;
        try { _reglage = await Medi.Data.Reglages(); }
        catch (ApiException) { _reglage = new Reglage(); }

        RappelsActifs = _reglage.RappelsActifs;
        PlageSilence = $"{_reglage.PlageDebut} – {_reglage.PlageFin}";
        OnPropertyChanged(nameof(RappelsActifs));
        OnPropertyChanged(nameof(PlageSilence));
        _chargement = false;
    }

    async void JourTape(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<JourItem>(s) is not { } j) return;
        await ChargerJour(j.Date);
    }

    async void DoseTapee(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<Dose>(s) is not { } dose) return;
        await ActionsDose.Marquer(dose);
        await ChargerJour(_jour);
    }

    async void RappelsToggles(object? s, ToggledEventArgs e)
    {
        if (_chargement) return;
        _reglage.RappelsActifs = e.Value;
        await EnregistrerReglages();
    }

    async void PlageTapee(object? s, TappedEventArgs e)
    {
        var choix = await Ui.Choix("Plage de silence",
            "21:00 – 06:00", "22:00 – 07:00", "23:00 – 07:00", "Aucune");
        if (choix is null) return;

        var parties = choix.Split('–');
        if (parties.Length == 2)
        {
            _reglage.PlageDebut = parties[0].Trim();
            _reglage.PlageFin = parties[1].Trim();
        }
        else
        {
            _reglage.PlageDebut = "00:00";
            _reglage.PlageFin = "00:00";
        }

        PlageSilence = _reglage.PlageDebut == "00:00"
            ? "Désactivée"
            : $"{_reglage.PlageDebut} – {_reglage.PlageFin}";
        OnPropertyChanged(nameof(PlageSilence));
        await EnregistrerReglages();
    }

    async Task EnregistrerReglages()
    {
        try { await Medi.Data.EnregistrerReglages(_reglage); }
        catch (ApiException ex) { await Ui.Avertir("Réglage non enregistré", ex.Message); }
        await AppShell.ActiverRappels();
    }

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");
}
