using MediRappel.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MediRappel;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
    {
        // Toute l'application s'exprime en français (dates, mois, nombres).
        var fr = new CultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = fr;
        CultureInfo.DefaultThreadCurrentUICulture = fr;
        CultureInfo.CurrentCulture = fr;
        CultureInfo.CurrentUICulture = fr;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("MaterialIcons.ttf", "MaterialIcons");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Client HTTP vers l'API MediRappel (10.0.2.2 depuis l'émulateur Android).
        var http = new HttpClient
        {
            BaseAddress = new Uri(ApiClient.UrlParDefaut),
            Timeout = TimeSpan.FromSeconds(20),
        };
        var api = new ApiClient(http);
        Medi.Api = api;
        Medi.Data = new MediRappelData(api);

        // Restaure une éventuelle session (« Se souvenir de moi »).
        Session.Charger();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
