using System;
using System.Threading.Tasks;
using MediRappel.Platforms.Android;
using MediRappel.Models;

namespace MediRappel.Services;

/// <summary>Rappels locaux (notifications Android) pour les prises du jour.</summary>
public sealed class NotificationService
{
    const int CodeRequeteBase = 1000;
    const string CanalId = "medirappel.rappel";
    const string CanalNom = "Rappels de prises";

    readonly global::Android.Content.Context _contexte;

    public NotificationService(global::Android.Content.Context contexte)
    {
        _contexte = contexte;
    }

    public Task InitialiserAsync()
    {
#if ANDROID
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
        {
            var canal = new global::Android.App.NotificationChannel(
                CanalId, CanalNom, global::Android.App.NotificationImportance.High)
            {
                Description = "Rappels de prises de médicaments",
                LockscreenVisibility = global::Android.App.NotificationVisibility.Public
            };
            canal.EnableVibration(true);
            var manager = (global::Android.App.NotificationManager)
                _contexte.GetSystemService(global::Android.Content.Context.NotificationService);
            manager.CreateNotificationChannel(canal);
        }
#endif
        return Task.CompletedTask;
    }

    public async Task ProgrammerDosesDuJourAsync()
    {
#if ANDROID
        AnnulerToutes();

        var doses = await Medi.Data.DosesAujourdhui();
        var maintenant = DateTime.Now;
        var id = CodeRequeteBase;

        foreach (var dose in doses.Where(d => d.Etat == EtatDose.Avenir))
        {
            if (!TimeSpan.TryParse(dose.Heure, out var heure)) continue;
            var declenchement = DateTime.Today + heure;
            if (declenchement <= maintenant) continue;

            ProgrammerAlarme(id++, declenchement, dose);
        }
#endif
    }

    void ProgrammerAlarme(int codeRequete, DateTime declenchement, Dose dose)
    {
#if ANDROID
        var intent = new global::Android.Content.Intent(_contexte, typeof(RappelReceiver));
        intent.PutExtra("titre", $"Prise de {dose.Titre}");
        intent.PutExtra("message", $"{dose.Posologie} à {dose.Heure}");
        intent.PutExtra("code", codeRequete);

        var pending = global::Android.App.PendingIntent.GetBroadcast(
            _contexte, codeRequete, intent,
            global::Android.App.PendingIntentFlags.UpdateCurrent |
            global::Android.App.PendingIntentFlags.Immutable);

        var alarme = (global::Android.App.AlarmManager)
            _contexte.GetSystemService(global::Android.Content.Context.AlarmService);

        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var millis = (long)(declenchement.ToUniversalTime() - epoch).TotalMilliseconds;

        alarme.SetExactAndAllowWhileIdle(
            global::Android.App.AlarmType.RtcWakeup, millis, pending);
#endif
    }

    public Task AnnulerToutesAsync()
    {
#if ANDROID
        var manager = (global::Android.App.NotificationManager)
            _contexte.GetSystemService(global::Android.Content.Context.NotificationService);
        manager.CancelAll();
#endif
        return Task.CompletedTask;
    }

    void AnnulerToutes()
    {
#if ANDROID
        var manager = (global::Android.App.NotificationManager)
            _contexte.GetSystemService(global::Android.Content.Context.NotificationService);
        manager.CancelAll();
#endif
    }
}
