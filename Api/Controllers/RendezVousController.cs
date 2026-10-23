using MediRappel.Api.Data;
using MediRappel.Api.Dtos;
using MediRappel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediRappel.Api.Controllers;

[ApiController]
[Route("api/rendezvous")]
[Authorize]
public class RendezVousController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<RendezVousDto>> Get()
    {
        var liste = await db.RendezVous
            .Where(r => r.UtilisateurId == UserId)
            .OrderBy(r => r.Date)
            .ToListAsync();
        return liste.Select(VersDto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RendezVousDto>> Get(int id)
    {
        var r = await db.RendezVous.FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UserId);
        return r is null ? NotFound() : Ok(VersDto(r));
    }

    [HttpPost]
    public async Task<ActionResult<RendezVousDto>> Post(RendezVousSaisie req)
    {
        var r = new RendezVous { UtilisateurId = UserId };
        Appliquer(r, req);
        db.RendezVous.Add(r);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = r.Id }, VersDto(r));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RendezVousDto>> Put(int id, RendezVousSaisie req)
    {
        var r = await db.RendezVous.FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UserId);
        if (r is null) return NotFound();
        Appliquer(r, req);
        await db.SaveChangesAsync();
        return Ok(VersDto(r));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await db.RendezVous.FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UserId);
        if (r is null) return NotFound();
        db.RendezVous.Remove(r);
        await db.SaveChangesAsync();
        return NoContent();
    }

    static void Appliquer(RendezVous r, RendezVousSaisie req)
    {
        r.Date = req.Date;
        r.Medecin = req.Medecin;
        r.Specialite = req.Specialite;
        r.Lieu = req.Lieu;
        r.Motif = req.Motif;
        r.Notes = req.Notes;
        r.Telephone = req.Telephone;
        r.Rappel = req.Rappel;
    }

    static RendezVousDto VersDto(RendezVous r) => new(
        r.Id, r.Date, r.Medecin, r.Specialite, r.Lieu, r.Motif, r.Notes, r.Telephone, r.Rappel);
}
