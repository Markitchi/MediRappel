using MediRappel.Api.Data;
using MediRappel.Api.Dtos;
using MediRappel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediRappel.Api.Controllers;

[ApiController]
[Route("api/reglages")]
[Authorize]
public class ReglagesController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReglageDto>> Get()
    {
        var r = await Charger();
        return Ok(VersDto(r));
    }

    [HttpPut]
    public async Task<ActionResult<ReglageDto>> Put(ReglageDto req)
    {
        var r = await Charger();
        r.SyncWifi = req.SyncWifi;
        r.RappelsActifs = req.RappelsActifs;
        r.PlageDebut = req.PlageDebut;
        r.PlageFin = req.PlageFin;
        r.Verrouillage = req.Verrouillage;
        await db.SaveChangesAsync();
        return Ok(VersDto(r));
    }

    async Task<Reglage> Charger()
    {
        var r = await db.Reglages.FirstOrDefaultAsync(x => x.UtilisateurId == UserId);
        if (r is null)
        {
            r = new Reglage { UtilisateurId = UserId };
            db.Reglages.Add(r);
            await db.SaveChangesAsync();
        }
        return r;
    }

    static ReglageDto VersDto(Reglage r) => new(
        r.SyncWifi, r.RappelsActifs, r.PlageDebut, r.PlageFin, r.Verrouillage);
}
