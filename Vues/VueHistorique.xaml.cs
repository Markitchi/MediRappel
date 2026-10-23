using System.Collections.ObjectModel;
using System.Text;
using MediRappel.Models;
using MediRappel.Services;

namespace MediRappel.Vues;

public partial class VueHistorique : ContentView, IRafraichir
{
    int _jours = 30;
    Historique _donnees = new();

    public ObservableCollection<SemaineStat> Semaines { get; } = new();

    public VueHistorique()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public bool Filtre7 => _jours == 7;
    public bool Filtre30 => _jours == 30;
    public bool Filtre90 => _jours == 90;
    public string ObsTexte { get; private set; } = "0 %";
    public string Resume { get; private set; } = "";
    public string NonPrises { get; private set; } = "";
    public double ObsNorm { get; private set; }
    public bool Vide => _donnees.Vide;
    public bool NonVide => !_donnees.Vide;
    public string PeriodeTexte => _jours switch
    {
        7 => "7 derniers jours",
        30 => "30 derniers jours",
        _ => "90 derniers jours",
    };

    public Task RafraichirAsync() => Charger(_jours);

    async Task Charger(int jours)
    {
        _jours = jours;
        try { _donnees = await Medi.Data.Historique(jours); }
        catch (ApiException ex) { await Ui.Avertir("Données indisponibles", ex.Message); }

        Semaines.Clear();
        foreach (var s in _donnees.Semaines) Semaines.Add(s);

        ObsTexte = $"{_donnees.Observance} %";
        Resume = _donnees.Resume;
        NonPrises = _donnees.NonPrises;
        ObsNorm = _donnees.Observance / 100.0;
        RafraichirProprietes();
    }

    void RafraichirProprietes()
    {
        OnPropertyChanged(nameof(Filtre7));
        OnPropertyChanged(nameof(Filtre30));
        OnPropertyChanged(nameof(Filtre90));
        OnPropertyChanged(nameof(ObsTexte));
        OnPropertyChanged(nameof(Resume));
        OnPropertyChanged(nameof(NonPrises));
        OnPropertyChanged(nameof(ObsNorm));
        OnPropertyChanged(nameof(Vide));
        OnPropertyChanged(nameof(NonVide));
        OnPropertyChanged(nameof(PeriodeTexte));
    }

    async void Filtre7Tapped(object? s, TappedEventArgs e) => await Charger(7);
    async void Filtre30Tapped(object? s, TappedEventArgs e) => await Charger(30);
    async void Filtre90Tapped(object? s, TappedEventArgs e) => await Charger(90);

    async void Exporter(object? s, EventArgs e) => await ExporterAsync();

    public async Task ExporterAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("MediRappel - historique d'observance");
        sb.AppendLine($"Periode;{_jours} jours");
        sb.AppendLine($"Observance;{_donnees.Observance} %");
        sb.AppendLine();
        sb.AppendLine("Semaine;Validees;Prevues;Manquees;Partielles;Pourcent");
        foreach (var x in _donnees.Semaines)
            sb.AppendLine($"{x.Libelle};{x.Validees};{x.Prevues};{x.Manquees};{x.Partielles};{x.Pourcent}");

        try
        {
            var chemin = Path.Combine(FileSystem.CacheDirectory,
                $"medirappel-historique-{DateTime.Now:yyyyMMdd-HHmm}.csv");
            await File.WriteAllTextAsync(chemin, sb.ToString());

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Historique MediRappel",
                File = new ShareFile(chemin),
            });
        }
        catch (Exception ex)
        {
            await Ui.Avertir("Export impossible", ex.Message);
        }
    }
}
