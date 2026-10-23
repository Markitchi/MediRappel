using System.Globalization;
using MediRappel.Api.Data;
using MediRappel.Api.Dtos;
using MediRappel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediRappel.Api.Controllers;

[ApiController]
[Route("api/historique")]
[Authorize]
public class HistoriqueController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Statistiques d'observance sur les N derniers jours (doses échues).</summary>
    [HttpGet]
    public async Task<ActionResult<HistoriqueDto>> Get([FromQuery] int jours = 30)
    {
        jours = Math.Clamp(jours, 1, 365);
        var fin = DateTime.Today;
        var debut = fin.AddDays(-(jours - 1));

        var doses = await db.Doses
            .Where(d => d.UtilisateurId == UserId && d.Date >= debut && d.Date <= fin)
            .ToListAsync();

        // On ne compte que les doses échues (les prises « à venir » fausseraient le taux).
        var echues = doses.Where(d => d.Etat != EtatDose.Avenir).ToList();
        int validees = echues.Count(d => d.Etat == EtatDose.Faite);
        int manquees = echues.Count(d => d.Etat == EtatDose.Manquee);
        int partielles = echues.Count(d => d.Etat == EtatDose.Partielle);
        int prevues = validees + manquees + partielles;

        double score = prevues == 0 ? 1.0 : (validees + 0.5 * partielles) / prevues;
        int observance = (int)Math.Round(score * 100);

        var semaines = new List<SemaineStat>();
        var lundi = DebutSemaine(fin);
        int nbSemaines = Math.Min(5, (int)Math.Ceiling(jours / 7.0));
        for (int i = 0; i < nbSemaines; i++)
        {
            var d = lundi.AddDays(-7 * i);
            var f = d.AddDays(6);
            var sDoses = echues.Where(x => x.Date >= d && x.Date <= f).ToList();
            int v = sDoses.Count(x => x.Etat == EtatDose.Faite);
            int m = sDoses.Count(x => x.Etat == EtatDose.Manquee);
            int p = sDoses.Count(x => x.Etat == EtatDose.Partielle);
            int tot = v + m + p;
            int pct = sDoses.Count == 0 ? 0 : (int)Math.Round(100.0 * (v + 0.5 * p) / Math.Max(1, v + m + p));
            string libelle = i == 0 ? "Cette semaine" : $"Semaine du {d:dd/MM}";
            semaines.Add(new SemaineStat(libelle, v, v + m + p, m, p, pct));
        }

        return Ok(new HistoriqueDto(jours, observance, validees, prevues, manquees, partielles, semaines));
    }

    static DateTime DebutSemaine(DateTime d)
    {
        int delta = ((int)d.DayOfWeek + 6) % 7; // lundi = 0
        return d.AddDays(-delta).Date;
    }
}
