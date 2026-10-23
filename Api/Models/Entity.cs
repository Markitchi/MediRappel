namespace MediRappel.Api.Models;

/// <summary>État d'une prise. Les valeurs entières sont exposées telles quelles
/// à l'app cliente : Faite=0, Avenir=1, Manquee=2, Partielle=3.</summary>
public enum EtatDose { Faite = 0, Avenir = 1, Manquee = 2, Partielle = 3 }

public class Utilisateur
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string MotDePasseHash { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public List<Dose> Doses { get; set; } = new();
    public List<Ordonnance> Ordonnances { get; set; } = new();
    public List<RendezVous> RendezVous { get; set; } = new();
    public Reglage? Reglage { get; set; }
}

/// <summary>Une prise planifiée (ligne du calendrier d'observance).</summary>
public class Dose
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }

    public string Medicament { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Posologie { get; set; } = string.Empty;
    public string Heure { get; set; } = "08:00";
    public string Detail { get; set; } = string.Empty;

    public EtatDose Etat { get; set; } = EtatDose.Avenir;
    /// <summary>Heure de validation saisie par l'utilisateur (« 08:12 »).</summary>
    public string Validee { get; set; } = string.Empty;
    public DateTime? DatePrise { get; set; }
    /// <summary>Jour calendaire de la prise (heure remise à minuit).</summary>
    public DateTime Date { get; set; } = DateTime.Today;
}

public class Ordonnance
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }

    public string Medecin { get; set; } = string.Empty;
    public string Specialite { get; set; } = string.Empty;
    public string Cabinet { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    /// <summary>« Active » ou « Terminée ».</summary>
    public string Statut { get; set; } = "Active";
    public string Resume { get; set; } = string.Empty;
    public string Periode { get; set; } = string.Empty;
    public bool Favori { get; set; }

    public List<Medicament> Medicaments { get; set; } = new();
}

public class Medicament
{
    public int Id { get; set; }
    public int OrdonnanceId { get; set; }
    public Ordonnance? Ordonnance { get; set; }

    public string Nom { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Posologie { get; set; } = string.Empty;
    public string Depuis { get; set; } = string.Empty;
}

public class RendezVous
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }

    public DateTime Date { get; set; }
    public string Medecin { get; set; } = string.Empty;
    public string Specialite { get; set; } = string.Empty;
    public string Lieu { get; set; } = string.Empty;
    public string Motif { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public bool Rappel { get; set; } = true;
}

public class Reglage
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }

    public bool SyncWifi { get; set; } = true;
    public bool RappelsActifs { get; set; } = true;
    public string PlageDebut { get; set; } = "22:00";
    public string PlageFin { get; set; } = "07:00";
    public bool Verrouillage { get; set; } = true;
}
