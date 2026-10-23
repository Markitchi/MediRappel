using System.Collections.ObjectModel;
using MediRappel.Models;
using MediRappel.Services;

namespace MediRappel.Vues;

public partial class VueOrdonnances : ContentView, IRafraichir
{
    readonly List<Ordonnance> _toutes = new();

    public ObservableCollection<Ordonnance> Liste { get; } = new();

    int _filtre; // 0 = toutes, 1 = actives, 2 = terminées

    public VueOrdonnances()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string Recherche { get; set; } = "";
    public string LabelToutes { get; private set; } = "Toutes";
    public string LabelActives { get; private set; } = "Actives";
    public string LabelTerminees { get; private set; } = "Terminées";
    public bool FiltreToutes => _filtre == 0;
    public bool FiltreActives => _filtre == 1;
    public bool FiltreTerminees => _filtre == 2;
    public bool VideVisible { get; private set; }

    public async Task RafraichirAsync()
    {
        try
        {
            _toutes.Clear();
            _toutes.AddRange(await Medi.Data.Ordonnances());
        }
        catch (ApiException ex) { await Ui.Avertir("Données indisponibles", ex.Message); }

        LabelToutes = $"Toutes {_toutes.Count}";
        LabelActives = $"Actives {_toutes.Count(o => o.IsActive)}";
        LabelTerminees = $"Terminées {_toutes.Count(o => !o.IsActive)}";
        Filtrer();
    }

    void Filtrer()
    {
        var q = (Recherche ?? "").Trim().ToLowerInvariant();
        Liste.Clear();
        foreach (var o in _toutes)
        {
            bool parFiltre = _filtre switch
            {
                1 => o.IsActive,
                2 => !o.IsActive,
                _ => true,
            };
            if (!parFiltre) continue;

            bool parRecherche = q.Length == 0
                || o.Medecin.ToLowerInvariant().Contains(q)
                || o.Specialite.ToLowerInvariant().Contains(q)
                || o.Cabinet.ToLowerInvariant().Contains(q)
                || o.Medicaments.Any(m => m.Nom.ToLowerInvariant().Contains(q));

            if (parRecherche) Liste.Add(o);
        }
        VideVisible = Liste.Count == 0;
        RafraichirProprietes();
    }

    void RafraichirProprietes()
    {
        OnPropertyChanged(nameof(LabelToutes));
        OnPropertyChanged(nameof(LabelActives));
        OnPropertyChanged(nameof(LabelTerminees));
        OnPropertyChanged(nameof(FiltreToutes));
        OnPropertyChanged(nameof(FiltreActives));
        OnPropertyChanged(nameof(FiltreTerminees));
        OnPropertyChanged(nameof(VideVisible));
    }

    void RechercheChangee(object? s, TextChangedEventArgs e)
    {
        Recherche = e.NewTextValue ?? "";
        Filtrer();
    }

    void FiltreToutesTapped(object? s, TappedEventArgs e) => ChoisirFiltre(0);
    void FiltreActivesTapped(object? s, TappedEventArgs e) => ChoisirFiltre(1);
    void FiltreTermineesTapped(object? s, TappedEventArgs e) => ChoisirFiltre(2);

    void ChoisirFiltre(int f)
    {
        _filtre = _filtre == f ? 0 : f;
        Filtrer();
    }

    async void Ouvrir(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<Ordonnance>(s) is not { } ord) return;
        await Shell.Current!.GoToAsync($"ordonnance?id={ord.Id}");
    }
}
