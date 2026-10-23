using MediRappel.Api.Data;
using MediRappel.Api.Dtos;
using MediRappel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediRappel.Api.Controllers;

[ApiController]
[Route("api/doses")]
[Authorize]
public class DosesController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Prises d'un jour donné (par défaut aujourd'hui).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DoseDto>>> Get([FromQuery] DateTime? date)
    {
        var jour = (date ?? DateTime.Today).Date;
        var doses = await db.Doses
            .Where(d => d.UtilisateurId == UserId && d.Date == jour)
            .OrderBy(d => d.Heure)
            .ToListAsync();
        return Ok(doses.Select(VersDto));
    }

    [HttpGet("aujourdhui")]
    public async Task<ActionResult<IEnumerable<DoseDto>>> Aujourdhui()
    {
        var jour = DateTime.Today;
        var doses = await db.Doses
            .Where(d => d.UtilisateurId == UserId && d.Date == jour)
            .OrderBy(d => d.Heure)
            .ToListAsync();
        return Ok(doses.Select(VersDto));
    }

    /// <summary>Ajoute une prise, éventuellement répétée chaque jour (planification).</summary>
    [HttpPost]
    public async Task<ActionResult<IEnumerable<DoseDto>>> Post(DoseSaisie req)
    {
        if (string.IsNullOrWhiteSpace(req.Medicament))
            return BadRequest(new { error = "Indiquez le nom du médicament." });

        var debut = req.Date == default ? DateTime.Today : req.Date.Date;
        int jours = Math.Clamp(req.Jours <= 0 ? 1 : req.Jours, 1, 365);
        var heure = string.IsNullOrWhiteSpace(req.Heure) ? "08:00" : req.Heure;

        var creees = new List<Dose>();
        for (int i = 0; i < jours; i++)
        {
            creees.Add(new Dose
            {
                UtilisateurId = UserId,
                Medicament = req.Medicament.Trim(),
                Dosage = req.Dosage?.Trim() ?? "",
                Posologie = req.Posologie?.Trim() ?? "",
                Heure = heure,
                Detail = string.IsNullOrWhiteSpace(req.Detail) ? "À prendre" : req.Detail,
                Date = debut.AddDays(i),
                Etat = EtatDose.Avenir,
            });
        }

        db.Doses.AddRange(creees);
        await db.SaveChangesAsync();
        return Ok(creees.Select(VersDto));
    }

    [HttpPost("serie")]
    public async Task<ActionResult<IEnumerable<DoseDto>>> PostSerie(DoseSerieSaisie req)
    {
        if (string.IsNullOrWhiteSpace(req.Medicament))
            return BadRequest(new { error = "Indiquez le nom du médicament." });

        var debut = req.Date == default ? DateTime.Today : req.Date.Date;
        int jours = Math.Clamp(req.Jours <= 0 ? 1 : req.Jours, 1, 365);
        var heures = (req.Heures ?? new List<string>())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (heures.Count == 0)
            heures.Add("08:00");

        var creees = new List<Dose>();
        for (int i = 0; i < jours; i++)
        {
            foreach (var h in heures)
            {
                creees.Add(new Dose
                {
                    UtilisateurId = UserId,
                    Medicament = req.Medicament.Trim(),
                    Dosage = req.Dosage?.Trim() ?? "",
                    Posologie = req.Posologie?.Trim() ?? "",
                    Heure = h,
                    Detail = string.IsNullOrWhiteSpace(req.Detail) ? "À prendre" : req.Detail,
                    Date = debut.AddDays(i),
                    Etat = EtatDose.Avenir,
                });
            }
        }

        db.Doses.AddRange(creees);
        await db.SaveChangesAsync();
        return Ok(creees.OrderBy(d => d.Date).ThenBy(d => d.Heure).Select(VersDto));
    }

    /// <summary>Marque l'état d'une prise (faite / partielle / manquée / à venir).</summary>
    [HttpPut("{id:int}/etat")]
    public async Task<ActionResult<DoseDto>> ChangerEtat(int id, DoseEtatSaisie req)
    {
        var dose = await db.Doses.FirstOrDefaultAsync(d => d.Id == id && d.UtilisateurId == UserId);
        if (dose is null) return NotFound();

        var etat = (EtatDose)req.Etat;
        dose.Etat = etat;

        if (etat == EtatDose.Faite || etat == EtatDose.Partielle)
        {
            dose.DatePrise = DateTime.Now;
            dose.Validee = string.IsNullOrWhiteSpace(req.Validee)
                ? DateTime.Now.ToString("HH:mm")
                : req.Validee!;
        }
        else
        {
            dose.DatePrise = null;
            dose.Validee = "";
        }

        await db.SaveChangesAsync();
        return Ok(VersDto(dose));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var dose = await db.Doses.FirstOrDefaultAsync(d => d.Id == id && d.UtilisateurId == UserId);
        if (dose is null) return NotFound();
        db.Doses.Remove(dose);
        await db.SaveChangesAsync();
        return NoContent();
    }

    static DoseDto VersDto(Dose d) => new(
        d.Id, d.Medicament, d.Dosage, d.Posologie, d.Heure, d.Detail,
        (int)d.Etat, d.Validee, d.DatePrise, d.Date);
}
