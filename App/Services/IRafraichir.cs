using MediRappel.Models;

namespace MediRappel.Services;

/// <summary>Une vue qui recharge ses données à l'affichage.</summary>
public interface IRafraichir
{
    Task RafraichirAsync();
}

/// <summary>Feuille d'actions partagée pour changer l'état d'une prise.</summary>
public static class ActionsDose
{
    public static async Task Marquer(Dose dose)
    {
        var choix = await Ui.Choix(
            $"Prise de {dose.Titre} · {dose.Heure}",
            "Marquer comme prise",
            "Prise partielle",
            "Marquer comme manquée",
            "Remettre à venir");

        if (choix is null) return;

        var etat = choix switch
        {
            "Marquer comme prise" => EtatDose.Faite,
            "Prise partielle" => EtatDose.Partielle,
            "Marquer comme manquée" => EtatDose.Manquee,
            _ => EtatDose.Avenir,
        };

        try { await Medi.Data.MarquerDose(dose, etat); }
        catch (ApiException ex) { await Ui.Avertir("Impossible", ex.Message); }
    }
}
