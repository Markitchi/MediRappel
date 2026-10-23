using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MediRappel.Api.Controllers;

/// <summary>Base commune : expose l'identifiant de l'utilisateur authentifié (JWT).</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
