using MediRappel.Api.Dtos;
using MediRappel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediRappel.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        try { return Ok(await auth.Register(req)); }
        catch (InvalidOperationException e) { return BadRequest(new { error = e.Message }); }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        try { return Ok(await auth.Login(req)); }
        catch (InvalidOperationException e) { return BadRequest(new { error = e.Message }); }
    }

    [HttpPost("motdepasse-oublie")]
    public async Task<IActionResult> MotDePasseOublie([FromBody] ReinitialisationRequest req)
    {
        await auth.DemanderReinitialisation(req.Email);
        return Ok(new
        {
            message = "Si un compte existe pour cette adresse, les instructions de " +
                      "réinitialisation vous seront envoyées."
        });
    }

    [Authorize]
    [HttpGet("moi")]
    public async Task<ActionResult<ProfilDto>> Moi()
    {
        var id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var p = await auth.GetProfil(id);
        return p is null ? NotFound() : Ok(p);
    }
}

public record ReinitialisationRequest(string Email);
