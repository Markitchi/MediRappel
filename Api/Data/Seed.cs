using MediRappel.Api.Models;

namespace MediRappel.Api.Data;

/// <summary>
/// Jeu de données du compte de démonstration/évaluation.
/// Compte : test@medirappel.fr — mot de passe : test1234
/// Les données sont générées relativement à la date du jour, de façon
/// déterministe, pour que l'historique et l'observance soient cohérents.
/// </summary>
public static class Seed
{
    public const string EmailTest = "test@medirappel.fr";
    public const string MotDePasseTest = "test1234";

    public static void Initialiser(AppDbContext db)
    {
        if (db.Utilisateurs.Any(u => u.Email == EmailTest)) return;

        var aujourdhui = DateTime.Today;

        var utilisateur = new Utilisateur
        {
            Email = EmailTest,
            Nom = "Marie Dupont",
            MotDePasseHash = HashMotDePasse(MotDePasseTest),
            CreeLe = DateTime.UtcNow.AddDays(-120),
        };
        db.Utilisateurs.Add(utilisateur);
        db.SaveChanges();

        // ---- Ordonnances ----
        var ordo1 = new Ordonnance
        {
            UtilisateurId = utilisateur.Id,
            Medecin = "Dr Ngo Bellemma",
            Specialite = "Cardiologue",
            Cabinet = "Clinique Farah, Cotonou",
            Date = aujourdhui.AddDays(-18),
            Statut = "Active",
            Resume = "3 médicaments · valide jusqu'au 18 janv. 2027",
            Periode = "3 mois de traitement",
            Favori = true,
            Medicaments =
            {
                new Medicament { Nom = "Metformine", Dosage = "850 mg", Posologie = "1 comprimé · 2 fois par jour · matin et soir", Depuis = "J-18" },
                new Medicament { Nom = "Atorvastatine", Dosage = "20 mg", Posologie = "1 comprimé · 1 fois par jour · au dîner", Depuis = "J-18" },
                new Medicament { Nom = "Amlodipine", Dosage = "5 mg", Posologie = "1 comprimé · 1 fois par jour · le matin", Depuis = "J-17" },
            },
        };
        var ordo2 = new Ordonnance
        {
            UtilisateurId = utilisateur.Id,
            Medecin = "Dr Sow Mbaye",
            Specialite = "Médecin généraliste",
            Cabinet = "Cabinet Sow, Fidjrossè",
            Date = aujourdhui.AddDays(-34),
            Statut = "Active",
            Resume = "1 médicament · complément quotidien",
            Periode = "3 mois de traitement",
            Medicaments =
            {
                new Medicament { Nom = "Vitamine D", Dosage = "100 000 UI", Posologie = "1 dose · au déjeuner", Depuis = "J-34" },
            },
        };
        var ordo3 = new Ordonnance
        {
            UtilisateurId = utilisateur.Id,
            Medecin = "Dr Ahouandjinou",
            Specialite = "Dermatologue",
            Cabinet = "CHU Abomey-Calavi",
            Date = aujourdhui.AddDays(-73),
            Statut = "Terminée",
            Resume = "1 médicament · traitement terminé",
            Periode = "1 mois de traitement",
            Medicaments =
            {
                new Medicament { Nom = "Hydrocortisone", Dosage = "1 %", Posologie = "Application · 2 fois par jour", Depuis = "J-73" },
            },
        };
        var ordo4 = new Ordonnance
        {
            UtilisateurId = utilisateur.Id,
            Medecin = "Dr Kponou",
            Specialite = "Ophtalmologue",
            Cabinet = "Cabinet Kponou, Cotonou",
            Date = aujourdhui.AddDays(-87),
            Statut = "Terminée",
            Resume = "1 médicament · traitement terminé",
            Periode = "1 mois de traitement",
            Medicaments =
            {
                new Medicament { Nom = "Larmes artificielles", Dosage = "15 ml", Posologie = "1 goutte · 3 fois par jour", Depuis = "J-87" },
            },
        };
        db.Ordonnances.AddRange(ordo1, ordo2, ordo3, ordo4);
        db.SaveChanges();

        // ---- Prises (doses) : aujourd'hui + 29 jours d'historique ----
        var doses = new List<Dose>();

        // Aujourd'hui : état « vivant » (2 faites, 1 à venir, 1 manquée).
        doses.Add(Faire(utilisateur.Id, aujourdhui, "08:00", "Metformine", "850 mg", "1 comprimé · avec le petit-déjeuner", EtatDose.Faite, "08:12"));
        doses.Add(Faire(utilisateur.Id, aujourdhui, "12:00", "Vitamine D", "100 000 UI", "1 dose · au déjeuner", EtatDose.Faite, "12:04"));
        doses.Add(Faire(utilisateur.Id, aujourdhui, "20:00", "Atorvastatine", "20 mg", "1 comprimé · au dîner", EtatDose.Avenir, ""));
        doses.Add(Faire(utilisateur.Id, aujourdhui, "21:00", "Amlodipine", "5 mg", "1 comprimé · le soir", EtatDose.Manquee, ""));

        // Historique : une variation déterministe pour ~94 % d'observance.
        for (int i = 1; i <= 29; i++)
        {
            var jour = aujourdhui.AddDays(-i);
            bool manquee = i % 7 == 0;
            bool partielle = !manquee && i % 5 == 0;
            string deux = (i % 60).ToString("00");

            doses.Add(Faire(utilisateur.Id, jour, "08:00", "Metformine", "850 mg", "1 comprimé · avec le petit-déjeuner", EtatDose.Faite, "08:0" + (i % 10)));
            doses.Add(Faire(utilisateur.Id, jour, "12:00", "Vitamine D", "100 000 UI", "1 dose · au déjeuner",
                partielle ? EtatDose.Partielle : EtatDose.Faite, "12:" + i.ToString("00")));
            doses.Add(Faire(utilisateur.Id, jour, "20:00", "Atorvastatine", "20 mg", "1 comprimé · au dîner", EtatDose.Faite, "20:0" + (i % 10)));
            doses.Add(Faire(utilisateur.Id, jour, "21:00", "Amlodipine", "5 mg", "1 comprimé · le soir",
                manquee ? EtatDose.Manquee : EtatDose.Faite, "21:0" + (i % 10)));
        }

        db.Doses.AddRange(doses);
        db.SaveChanges();

        // ---- Rendez-vous ----
        db.RendezVous.AddRange(
            new RendezVous
            {
                UtilisateurId = utilisateur.Id,
                Date = aujourdhui.AddDays(3).AddHours(10).AddMinutes(30),
                Medecin = "Dr Ngo Bellemma", Specialite = "Cardiologue",
                Lieu = "Clinique Farah · salle 204", Motif = "Contrôle de la tension",
                Notes = "Apporter l'ordonnance du 28/09 et la prise de sang",
                Telephone = "+229 21 30 45 12", Rappel = true,
            },
            new RendezVous
            {
                UtilisateurId = utilisateur.Id,
                Date = aujourdhui.AddDays(5).AddHours(9).AddMinutes(15),
                Medecin = "Dr Sow Mbaye", Specialite = "Médecin généraliste",
                Lieu = "Cabinet Sow, Fidjrossè", Motif = "Consultation de suivi",
                Notes = "", Telephone = "+229 21 35 88 40", Rappel = true,
            },
            new RendezVous
            {
                UtilisateurId = utilisateur.Id,
                Date = aujourdhui.AddDays(20).AddHours(14),
                Medecin = "Dr Ahouandjinou", Specialite = "Dermatologue",
                Lieu = "CHU Abomey-Calavi", Motif = "Contrôle de la peau",
                Notes = "", Telephone = "+229 21 36 00 22", Rappel = false,
            },
            new RendezVous
            {
                UtilisateurId = utilisateur.Id,
                Date = aujourdhui.AddDays(-6).AddHours(11),
                Medecin = "Dr Kponou", Specialite = "Ophtalmologue",
                Lieu = "Cabinet Kponou, Cotonou", Motif = "Contrôle de la vue",
                Notes = "", Telephone = "+229 21 31 77 09", Rappel = false,
            });
        db.SaveChanges();

        // ---- Réglages ----
        db.Reglages.Add(new Reglage { UtilisateurId = utilisateur.Id });
        db.SaveChanges();
    }

    static Dose Faire(int userId, DateTime jour, string heure, string med, string dosage,
                      string posologie, EtatDose etat, string validee) => new()
    {
        UtilisateurId = userId,
        Date = jour.Date,
        Heure = heure,
        Medicament = med,
        Dosage = dosage,
        Posologie = posologie,
        Detail = etat switch
        {
            EtatDose.Faite => "Prise",
            EtatDose.Manquee => "Manquée",
            EtatDose.Partielle => "Partielle",
            _ => "À prendre",
        },
        Etat = etat,
        Validee = validee,
        DatePrise = etat == EtatDose.Faite ? jour.Date.Add(TimeSpan.Parse(heure)) : null,
    };

    /// <summary>PBKDF2-SHA256, 100 000 itérations — format « sel:hash » (Base64).</summary>
    static string HashMotDePasse(string motDePasse)
    {
        var sel = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            motDePasse, sel, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(sel) + ":" + Convert.ToBase64String(hash);
    }
}
