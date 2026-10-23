namespace MediRappel.Api.Dtos;

// ---- Authentification ----
public record RegisterRequest(string Email, string Nom, string MotDePasse);
public record LoginRequest(string Email, string MotDePasse);
public record AuthResponse(string Token, string Email, string Nom, int Id);
public record ProfilDto(int Id, string Nom, string Email, DateTime CreeLe);

// ---- Ordonnances ----
public record MedicamentDto(int Id, string Nom, string Dosage, string Posologie, string Depuis);
public record OrdonnanceDto(
    int Id, string Medecin, string Specialite, string Cabinet, DateTime Date,
    string Statut, string Resume, string Periode, bool Favori, List<MedicamentDto> Medicaments);
public record MedicamentSaisie(string Nom, string Dosage, string Posologie, string Depuis);
public record OrdonnanceSaisie(
    string Medecin, string Specialite, string Cabinet, DateTime Date,
    string Statut, string Resume, string Periode, bool Favori, List<MedicamentSaisie> Medicaments);

// ---- Prises / doses ----
public record DoseDto(
    int Id, string Medicament, string Dosage, string Posologie, string Heure, string Detail,
    int Etat, string Validee, DateTime? DatePrise, DateTime Date);
/// <summary>Saisie d'une prise. <paramref name="Jours"/> permet de répéter la prise
/// chaque jour à la même heure (planification d'un traitement) ; 1 = jour unique.</summary>
public record DoseSaisie(
    string Medicament, string Dosage, string Posologie, string Heure, string Detail, DateTime Date,
    int Jours = 1);
public record DoseSerieSaisie(
    string Medicament, string Dosage, string Posologie, List<string> Heures, string Detail, DateTime Date,
    int Jours = 1);
public record DoseEtatSaisie(int Etat, string? Validee);

// ---- Rendez-vous ----
public record RendezVousDto(
    int Id, DateTime Date, string Medecin, string Specialite, string Lieu,
    string Motif, string Notes, string Telephone, bool Rappel);
public record RendezVousSaisie(
    DateTime Date, string Medecin, string Specialite, string Lieu,
    string Motif, string Notes, string Telephone, bool Rappel);

// ---- Réglages ----
public record ReglageDto(
    bool SyncWifi, bool RappelsActifs,
    string PlageDebut, string PlageFin, bool Verrouillage);

// ---- Historique ----
public record SemaineStat(string Libelle, int Validees, int Prevues, int Manquees, int Partielles, int Pourcent);
public record HistoriqueDto(
    int Jours, int Observance, int Validees, int Prevues, int Manquees, int Partielles, List<SemaineStat> Semaines);
