using System.Text;
using MediRappel.Api.Data;
using MediRappel.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Base SQLite locale, toujours au même endroit quelle que soit la console.
var cheminDb = Path.Combine(builder.Environment.ContentRootPath, "medirappel.db");
var cle = builder.Configuration["Jwt:Key"] ?? "MediRappelSuperSecretKeyDevOnlyChangeMe";

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={cheminDb}"));
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "MediRappel",
            ValidAudience = "MediRappel",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cle)),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(p => p.AddPolicy("cors",
    c => c.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors("cors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Text("MediRappel API", "text/plain"));

using (var sc = app.Services.CreateScope())
{
    var db = sc.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Seed.Initialiser(db);
}

app.Run();
