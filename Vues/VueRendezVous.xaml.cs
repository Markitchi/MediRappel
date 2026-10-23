using System.Collections.ObjectModel;
using MediRappel.Models;
using MediRappel.Services;

namespace MediRappel.Vues;

/// <summary>Ligne de liste : en-tête de groupe ou carte de rendez-vous.</summary>
public sealed class RdvItem
{
    public string? Groupe { get; init; }
    public RendezVous? Rdv { get; init; }

    public bool EstGroupe => Groupe is not null;
    public string Jour => Rdv?.JourNum ?? "";
    public string Heure => Rdv?.Heure ?? "";
    public string Medecin => Rdv?.Medecin ?? "";
    public string Specialite => Rdv?.Specialite ?? "";
    public string Lieu => Rdv?.Lieu ?? "";
    public string JoursRestants => Rdv?.JoursRestants ?? "";
}

public partial class VueRendezVous : ContentView, IRafraichir
{
    List<RendezVous> _tous = new();

    public ObservableCollection<RdvItem> Items { get; } = new();

    public VueRendezVous()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public bool Passes { get; private set; }
    public bool FiltreAVenir => !Passes;
    public bool FiltrePasses => Passes;
    public string LabelAvenir { get; private set; } = "À venir";
    public string LabelPasses { get; private set; } = "Passés";
    public bool VideVisible { get; private set; }
    public string VideTitre => Passes ? "Aucun rendez-vous passé" : "Aucun rendez-vous à venir";
    public string VideTexte => Passes
        ? "Vos consultations archivées apparaîtront ici."
        : "Ajoutez un rendez-vous avec le bouton ci-dessous.";

    public async Task RafraichirAsync()
    {
        try { _tous = await Medi.Data.RendezVousListe(); }
        catch (ApiException ex) { await Ui.Avertir("Données indisponibles", ex.Message); }

        Construire();
    }

    void Construire()
    {
        var source = Passes
            ? _tous.Where(r => r.Passe).OrderByDescending(r => r.Date)
            : _tous.Where(r => !r.Passe).OrderBy(r => r.Date);

        Items.Clear();
        string groupe = "";
        foreach (var r in source)
        {
            if (r.Groupe != groupe)
            {
                groupe = r.Groupe;
                Items.Add(new RdvItem { Groupe = groupe });
            }
            Items.Add(new RdvItem { Rdv = r });
        }
        VideVisible = Items.Count == 0;
        RafraichirProprietes();
    }

    void RafraichirProprietes()
    {
        OnPropertyChanged(nameof(Passes));
        OnPropertyChanged(nameof(FiltreAVenir));
        OnPropertyChanged(nameof(FiltrePasses));
        OnPropertyChanged(nameof(LabelAvenir));
        OnPropertyChanged(nameof(LabelPasses));
        OnPropertyChanged(nameof(VideVisible));
        OnPropertyChanged(nameof(VideTitre));
        OnPropertyChanged(nameof(VideTexte));
    }

    void Chips(bool passes)
    {
        Passes = passes;
        Construire();
    }

    void AvenirTapped(object? s, TappedEventArgs e) => Chips(false);
    void PassesTapped(object? s, TappedEventArgs e) => Chips(true);

    async void Ouvrir(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<RdvItem>(s) is not { Rdv: { } rdv }) return;
        await Shell.Current!.GoToAsync($"rdv?id={rdv.Id}");
    }
}
