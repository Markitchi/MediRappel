using MediRappel.Controls;
using MediRappel.Services;
using MediRappel.Vues;

namespace MediRappel.Pages;

public partial class MainPage : ContentPage
{
    /// <summary>Onglet à sélectionner à l'arrivée (écrans poussés qui gardent la barre).</summary>
    public static int OngletDemande { get; set; } = 0;

    readonly VisualElement[] _vues;
    int _onglet = -1;

    public MainPage()
    {
        InitializeComponent();

        _vues = new VisualElement[] { vAccueil, vOrdo, vRdv, vHist };

        nav.DestinationChoisie += async (_, idx) => await AllerOnglet(idx);
        fab.ActionCliquee += async (_, _) => await ActionFab();

        Connectivity.ConnectivityChanged += (_, _) => MajReseau();
        MajReseau();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _ = AllerOnglet(OngletDemande);
    }

    void MajReseau()
    {
        bool enLigne = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        barre.HorsLigne = !enLigne;
    }

    async Task AllerOnglet(int idx)
    {
        idx = Math.Clamp(idx, 0, _vues.Length - 1);

        // Mémorise l'onglet choisi : OnNavigatedTo s'en sert pour restaurer le
        // même onglet au retour d'un écran poussé. Sans cette affectation, tout
        // retour depuis un écran poussé remonte systématiquement à l'onglet 0
        // (Accueil), quel que soit l'onglet d'où l'utilisateur venait.
        OngletDemande = idx;
        for (int i = 0; i < _vues.Length; i++)
            _vues[i].IsVisible = i == idx;

        nav.ActiveIndex = idx;

        (_onglet, fab.Libelle, fab.Icone) = idx switch
        {
            0 => (0, "Mes prises", "event_note"),
            1 => (1, "Ajouter", "add"),
            2 => (2, "Ajouter", "add"),
            _ => (3, "Exporter", "ios_share"),
        };
        // L'historique porte déjà son propre bouton d'export : pas de FAB redondant.
        fab.IsVisible = idx != 3;

        if (_vues[idx] is IRafraichir r)
        {
            try { await r.RafraichirAsync(); }
            catch (Exception) { /* le rafraîchissement ne doit jamais bloquer la navigation */ }
        }
    }

    async Task ActionFab()
    {
        switch (_onglet)
        {
            case 0:
                await Shell.Current!.GoToAsync("prises");
                break;
            case 1:
                await Shell.Current!.GoToAsync("nouvelle-ordonnance");
                break;
            case 2:
                await Shell.Current!.GoToAsync("nouveau-rdv");
                break;
            default:
                await vHist.ExporterAsync();
                break;
        }
    }
}
