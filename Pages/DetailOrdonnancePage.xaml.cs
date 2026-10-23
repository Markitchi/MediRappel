using System.Collections.ObjectModel;
using MediRappel.Models;
using MediRappel.Services;

namespace MediRappel.Pages;

// NB : ne pas nommer la propriété « Id » : Element expose déjà Id, la résolution
// du paramètre de requête par reflexion lèverait une AmbiguousMatchException.
[QueryProperty(nameof(OrdonnanceId), "id")]
public partial class DetailOrdonnancePage : ContentPage
{
    Ordonnance? _ordonnance;

    public int OrdonnanceId { get; set; }
    public ObservableCollection<Medicament> Meds { get; } = new();

    public DetailOrdonnancePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string Medecin { get; private set; } = "";
    public string SousTitre { get; private set; } = "";
    public string Statut { get; private set; } = "";
    public string DateTexte { get; private set; } = "";
    public string NbMedicamentsTexte { get; private set; } = "";
    public string Periode { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public string Resume { get; private set; } = "";

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

        try { _ordonnance = await Medi.Data.Ordonnance(OrdonnanceId); }
        catch (ApiException ex) { await Ui.Avertir("Ordonnance indisponible", ex.Message); }

        if (_ordonnance is null)
        {
            await Ui.Avertir("Introuvable", "Cette ordonnance n'existe plus.");
            await Shell.Current!.GoToAsync("..");
            return;
        }

        Medecin = _ordonnance.Medecin;
        SousTitre = _ordonnance.SousTitre;
        Statut = _ordonnance.Statut;
        DateTexte = _ordonnance.DateTexte;
        NbMedicamentsTexte = _ordonnance.NbMedicamentsTexte;
        Periode = _ordonnance.Periode;
        Reference = $"MR-{_ordonnance.Date:yyyyMMdd}-{_ordonnance.Id:000}";
        Resume = _ordonnance.Resume;

        Meds.Clear();
        foreach (var m in _ordonnance.Medicaments) Meds.Add(m);

        RafraichirProprietes();
    }

    void RafraichirProprietes()
    {
        OnPropertyChanged(nameof(Medecin));
        OnPropertyChanged(nameof(SousTitre));
        OnPropertyChanged(nameof(Statut));
        OnPropertyChanged(nameof(DateTexte));
        OnPropertyChanged(nameof(NbMedicamentsTexte));
        OnPropertyChanged(nameof(Periode));
        OnPropertyChanged(nameof(Reference));
        OnPropertyChanged(nameof(Resume));
    }

    string TextePartage()
    {
        if (_ordonnance is null) return "";
        var lignes = _ordonnance.Medicaments
            .Select(m => $"• {m.Nom} {m.Dosage} — {m.Posologie}");
        return
            $"Ordonnance MediRappel — {Reference}\n" +
            $"Médecin : {_ordonnance.Medecin} ({_ordonnance.Specialite})\n" +
            $"Cabinet : {_ordonnance.Cabinet}\n" +
            $"Délivrée le : {_ordonnance.DateTexte}\n" +
            $"Durée : {_ordonnance.Periode}\n\n" +
            "Médicaments :\n" + string.Join("\n", lignes);
    }

    async void Partager(object? s, EventArgs e)
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Ordonnance MediRappel",
            Subject = $"Ordonnance {Reference}",
            Text = TextePartage(),
        });
    }

    async void Copier(object? s, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(Reference);
        await Ui.Avertir("Copié", $"La référence {Reference} a été copiée.");
    }

    async void Menu(object? s, TappedEventArgs e)
    {
        var choix = await Ui.Choix("Ordonnance",
            "Partager au médecin", "Copier la référence", "Supprimer");
        switch (choix)
        {
            case "Partager au médecin":
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Title = "Ordonnance MediRappel",
                    Subject = $"Ordonnance {Reference}",
                    Text = TextePartage(),
                });
                break;
            case "Copier la référence":
                await Clipboard.Default.SetTextAsync(Reference);
                await Ui.Avertir("Copié", $"La référence {Reference} a été copiée.");
                break;
            case "Supprimer":
                await SupprimerAsync();
                break;
        }
    }

    async void Supprimer(object? s, EventArgs e) => await SupprimerAsync();

    async Task SupprimerAsync()
    {
        if (_ordonnance is null) return;
        if (!await Ui.Confirmer("Supprimer l'ordonnance",
                $"Supprimer définitivement l'ordonnance {Reference} ?", "Supprimer")) return;

        try
        {
            await Medi.Data.SupprimerOrdonnance(_ordonnance.Id);
            await Shell.Current!.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            await Ui.Avertir("Suppression impossible", ex.Message);
        }
    }

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");
}
