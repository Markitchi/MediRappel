using MediRappel.Api.Data;
using MediRappel.Api.Dtos;
using MediRappel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediRappel.Api.Controllers;

[ApiController]
[Route("api/ordonnances")]
[Authorize]
public class OrdonnancesController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<OrdonnanceDto>> Get()
    {
        var liste = await db.Ordonnances
            .Include(o => o.Medicaments)
            .Where(o => o.UtilisateurId == UserId)
            .OrderByDescending(o => o.Date)
            .ToListAsync();
        return liste.Select(VersDto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrdonnanceDto>> Get(int id)
    {
        var o = await db.Ordonnances.Include(x => x.Medicaments)
            .FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UserId);
        return o is null ? NotFound() : Ok(VersDto(o));
    }

    [HttpPost]
    public async Task<ActionResult<OrdonnanceDto>> Post(OrdonnanceSaisie req)
    {
        var o = new Ordonnance
        {
            UtilisateurId = UserId,
            Medecin = req.Medecin,
            Specialite = req.Specialite,
            Cabinet = req.Cabinet,
            Date = req.Date.Date,
            Statut = string.IsNullOrWhiteSpace(req.Statut) ? "Active" : req.Statut,
            Resume = req.Resume,
            Periode = req.Periode,
            Favori = req.Favori,
            Medicaments = req.Medicaments.Select(m => new Medicament
            {
                Nom = m.Nom, Dosage = m.Dosage, Posologie = m.Posologie, Depuis = m.Depuis
            }).ToList(),
        };
        db.Ordonnances.Add(o);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = o.Id }, VersDto(o));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrdonnanceDto>> Put(int id, OrdonnanceSaisie req)
    {
        var o = await db.Ordonnances.Include(x => x.Medicaments)
            .FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UserId);
        if (o is null) return NotFound();

        o.Medecin = req.Medecin;
        o.Specialite = req.Specialite;
        o.Cabinet = req.Cabinet;
        o.Date = req.Date.Date;
        o.Statut = req.Statut;
        o.Resume = req.Resume;
        o.Periode = req.Periode;
        o.Favori = req.Favori;

        db.Medicaments.RemoveRange(o.Medicaments);
        o.Medicaments = req.Medicaments.Select(m => new Medicament
        {
            Nom = m.Nom, Dosage = m.Dosage, Posologie = m.Posologie, Depuis = m.Depuis
        }).ToList();

        await db.SaveChangesAsync();
        return Ok(VersDto(o));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var o = await db.Ordonnances.FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UserId);
        if (o is null) return NotFound();
        db.Ordonnances.Remove(o);
        await db.SaveChangesAsync();
        return NoContent();
    }

    static OrdonnanceDto VersDto(Ordonnance o) => new(
        o.Id, o.Medecin, o.Specialite, o.Cabinet, o.Date, o.Statut, o.Resume, o.Periode, o.Favori,
        o.Medicaments.Select(m => new MedicamentDto(m.Id, m.Nom, m.Dosage, m.Posologie, m.Depuis)).ToList());
}
