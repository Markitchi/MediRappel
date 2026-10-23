using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediRappel.Api.Data;
using MediRappel.Api.Dtos;
using MediRappel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MediRappel.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> Register(RegisterRequest req);
    Task<AuthResponse> Login(LoginRequest req);
    Task<ProfilDto?> GetProfil(int id);
    Task<bool> DemanderReinitialisation(string email);
}

public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
{
    public async Task<AuthResponse> Register(RegisterRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new InvalidOperationException("Adresse e-mail invalide.");
        if (string.IsNullOrWhiteSpace(req.Nom))
            throw new InvalidOperationException("Le nom est obligatoire.");
        if ((req.MotDePasse ?? "").Length < 8)
            throw new InvalidOperationException("Le mot de passe doit contenir au moins 8 caractères.");

        if (await db.Utilisateurs.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Cette adresse e-mail est déjà utilisée.");

        var user = new Utilisateur
        {
            Email = email,
            Nom = req.Nom.Trim(),
            MotDePasseHash = Hacher(req.MotDePasse!),
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();
        db.Reglages.Add(new Reglage { UtilisateurId = user.Id });
        await db.SaveChangesAsync();

        return new AuthResponse(GenererToken(user), user.Email, user.Nom, user.Id);
    }

    public async Task<AuthResponse> Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = await db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !Verifier(req.MotDePasse ?? "", user.MotDePasseHash))
            throw new InvalidOperationException("E-mail ou mot de passe incorrect.");
        return new AuthResponse(GenererToken(user), user.Email, user.Nom, user.Id);
    }

    public async Task<ProfilDto?> GetProfil(int id)
    {
        var u = await db.Utilisateurs.FindAsync(id);
        return u is null ? null : new ProfilDto(u.Id, u.Nom, u.Email, u.CreeLe);
    }

    public async Task<bool> DemanderReinitialisation(string email)
    {
        // Pas d'envoi d'e-mail en développement : on confirme simplement la
        // demande. Réponse identique que le compte existe ou non (anti-énumération).
        email = (email ?? "").Trim().ToLowerInvariant();
        await db.Utilisateurs.AnyAsync(u => u.Email == email);
        return true;
    }

    string GenererToken(Utilisateur user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] ?? "MediRappelSuperSecretKeyDevOnlyChangeMe"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
        };
        var token = new JwtSecurityToken("MediRappel", "MediRappel", claims,
            expires: DateTime.UtcNow.AddDays(30), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    static string Hacher(string motDePasse)
    {
        var sel = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(sel) + ":" + Convert.ToBase64String(hash);
    }

    static bool Verifier(string motDePasse, string stocke)
    {
        var parties = (stocke ?? "").Split(':');
        if (parties.Length != 2) return false;
        try
        {
            var sel = Convert.FromBase64String(parties[0]);
            var attendu = Convert.FromBase64String(parties[1]);
            var calcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(calcule, attendu);
        }
        catch (FormatException) { return false; }
    }
}
