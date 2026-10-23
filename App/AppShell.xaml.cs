using MediRappel.Pages;
using MediRappel.Services;

namespace MediRappel;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Écrans poussés (hors arborescence Shell), avec retour natif
        Routing.RegisterRoute("inscription", typeof(InscriptionPage));
        Routing.RegisterRoute("reglages", typeof(ReglagesPage));
        Routing.RegisterRoute("ordonnance", typeof(DetailOrdonnancePage));
        Routing.RegisterRoute("nouvelle-ordonnance", typeof(NouvelleOrdonnancePage));
        Routing.RegisterRoute("nouvelle-prise", typeof(NouvellePrisePage));
        Routing.RegisterRoute("rdv", typeof(DetailRendezVousPage));
        Routing.RegisterRoute("nouveau-rdv", typeof(NouveauRendezVousPage));
        Routing.RegisterRoute("prises", typeof(MesPrisesPage));

        // Session retrouvée (« Se souvenir de moi ») : on file directement à l'accueil.
        if (Session.Connecte)
            Dispatcher.Dispatch(async () => { await Aller("main"); await ActiverRappels(); });
    }

    /// <summary>Initialise (ou réactive) le service de rappels locaux.</summary>
    public static async Task ActiverRappels()
    {
#if ANDROID
        try
        {
            if (Medi.Notification is null)
                Medi.Notification = new NotificationService(Android.App.Application.Context);

            await Medi.Notification.InitialiserAsync();

            var reglage = await Medi.Data.Reglages();
            if (reglage.RappelsActifs)
                await Medi.Notification.ProgrammerDosesDuJourAsync();
        }
        catch { /* rappels indiscrets : on ignore silencieusement */ }
#endif
    }

    // Routes déclarées comme éléments Shell (ShellContent) : y accéder exige
    // une route absolue (« //route »), sinon MAUI lève « Relative routing to
    // shell elements is currently not supported ». Les autres routes sont des
    // pages enregistrées via Routing.RegisterRoute et se poussent relativement.
    public static Task Aller(string route) =>
        Current is null ? Task.CompletedTask : Current.GoToAsync($"//{route}");

    public static Task AllerOnglet(int idx)
    {
        MainPage.OngletDemande = idx;
        return Aller("main");
    }
}
