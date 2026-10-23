using Android.App;
using Android.Content;

namespace MediRappel.Platforms.Android;

/// <summary>Récepteur d'alarme : affiche la notification de rappel de prise.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { "medirappel.rappel" })]
public class RappelReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? contexte, Intent? intent)
    {
        if (contexte is null || intent is null) return;

        var titre = intent.GetStringExtra("titre") ?? "Rappel MediRappel";
        var message = intent.GetStringExtra("message") ?? "";
        var code = intent.GetIntExtra("code", 0);

        var builder = new global::Android.App.Notification.Builder(contexte, "medirappel.rappel")
            .SetContentTitle(titre)
            .SetContentText(message)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true)
            .SetPriority((int)global::Android.App.NotificationPriority.High);

        var manager = (NotificationManager)contexte.GetSystemService(Context.NotificationService);
        manager.Notify(code, builder.Build());
    }
}
