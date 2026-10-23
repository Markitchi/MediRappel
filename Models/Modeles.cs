using System.ComponentModel;

namespace MediRappel.Models;

/// États d'une prise : double signalisation (couleur + texte + icône).
/// Les valeurs correspondent à celles exposées par l'API.
public enum EtatDose { Faite = 0, Avenir = 1, Manquee = 2, Partielle = 3 }

/// <summary>
/// Une heure de prise éditable (formulaire « Ajouter une prise » et planning
/// d'ordonnance). Le créneau est pré-rempli par les boutons rapides
/// (matin/midi/soir/coucher) mais reste totalement libre.
/// </summary>
public sealed class PriseHoraire : INotifyPropertyChanged
{
    /// <summary>Nombre maximal de prises par jour saisissables dans les formulaires.</summary>
    public const int MaxParJour = 6;

    /// <summary>
    /// Créneaux conventionnels, strictement identiques aux boutons rapides
    /// (matin 08:00 · midi 12:00 · soir 20:00 · coucher 22:00) : ils servent
    /// de valeurs par défaut à la saisie.
    /// </summary>
    public static readonly TimeSpan[] Creneaux =
        [new(8, 0, 0), new(12, 0, 0), new(20, 0, 0), new(22, 0, 0)];

    /// <summary>
    /// Construit la prochaine ligne horaire en choisissant une heure encore
    /// libre : d'abord un créneau conventionnel, sinon la première demi-heure
    /// non retenue. Le compteur « + » reste ainsi toujours cohérent avec le
    /// nombre d'heures réellement planifiées — pas de doublon à corriger à la
    /// main avant d'enregistrer.
    /// </summary>
    public static PriseHoraire Creer(IEnumerable<TimeSpan> occupes)
    {
        var prises = occupes as IReadOnlyCollection<TimeSpan> ?? occupes.ToArray();

        foreach (var c in Creneaux)
            if (!prises.Contains(c)) return new PriseHoraire { Heure = c };

        for (int h = 0; h < 24; h++)
            for (int m = 0; m < 60; m += 30)
            {
                var t = new TimeSpan(h, m, 0);
                if (!prises.Contains(t)) return new PriseHoraire { Heure = t };
            }
        return new PriseHoraire();          // cas limite : quota déjà atteint
    }

    TimeSpan _heure = new(8, 0, 0);
    bool _supprimable;

    public TimeSpan Heure
    {
        get => _heure;
        set
        {
            if (_heure == value) return;
            _heure = value;
            Maj(nameof(Heure));
            Maj(nameof(Libelle));
            Maj(nameof(EstMatin));
            Maj(nameof(EstMidi));
            Maj(nameof(EstSoir));
            Maj(nameof(EstCoucher));
        }
    }

    public string Libelle => Heure.ToString(@"hh\:mm");

    public bool EstMatin => Heure.Hours is >= 5 and < 11;
    public bool EstMidi => Heure.Hours is >= 11 and < 16;
    public bool EstSoir => Heure.Hours is >= 16 and < 22;
    public bool EstCoucher => Heure.Hours >= 22 || Heure.Hours < 5;

    /// <summary>L'icône de suppression n'apparaît que s'il reste au moins une prise.</summary>
    public bool Supprimable
    {
        get => _supprimable;
        set
        {
            if (_supprimable == value) return;
            _supprimable = value;
            Maj(nameof(Supprimable));
        }
    }

    void Maj(string nom) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class Profil
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreeLe { get; set; }

    public string Depuis => $"Compte créé le {CreeLe:dd/MM/yyyy}";
    public string Initiales
    {
        get
        {
            var parties = Nom.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parties.Length >= 2
                ? $"{parties[0][0]}{parties[1][0]}"
                : (Nom.Length > 0 ? Nom[..1].ToUpperInvariant() : "?");
        }
    }
}

public sealed class Dose
{
    public int Id { get; set; }
    public string Medicament { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Posologie { get; set; } = "";
    public string Heure { get; set; } = "";
    public string Detail { get; set; } = "";
    public EtatDose Etat { get; set; } = EtatDose.Avenir;
    public string Validee { get; set; } = "";
    public DateTime? DatePrise { get; set; }
    public DateTime Date { get; set; }

    public string Titre => string.IsNullOrWhiteSpace(Dosage) ? Medicament : $"{Medicament} {Dosage}";

    public string EtatTexte => Etat switch
    {
        EtatDose.Faite => "Faite",
        EtatDose.Manquee => "Manquée",
        EtatDose.Partielle => "Partielle",
        _ => "À venir",
    };

    public string BadgeTexte => Etat switch
    {
        EtatDose.Faite => string.IsNullOrWhiteSpace(Validee) ? "Faite" : $"OK · {Validee}",
        EtatDose.Manquee => "Manquée",
        EtatDose.Partielle => "Partielle",
        _ => "À venir",
    };
}

public sealed class Medicament
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Posologie { get; set; } = "";
    public string Depuis { get; set; } = "";

    public bool AfficheDepuis => !string.IsNullOrWhiteSpace(Depuis);
}

public sealed class Ordonnance
{
    public int Id { get; set; }
    public string Medecin { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string Cabinet { get; set; } = "";
    public DateTime Date { get; set; }
    public string Statut { get; set; } = "Active";
    public string Resume { get; set; } = "";
    public string Periode { get; set; } = "";
    public bool Favori { get; set; }
    public List<Medicament> Medicaments { get; set; } = new();

    public bool IsActive => Statut == "Active";
    public string SousTitre => string.IsNullOrWhiteSpace(Specialite) ? Cabinet : $"{Specialite} · {Cabinet}";
    public string DateTexte => Date.ToString("dd MMMM yyyy");
    public string DateCourte => Date.ToString("dd MMM. yyyy");
    public string PraticienLigne => $"{Specialite} · {Cabinet}";
    public string NbMedicamentsTexte => Medicaments.Count > 1
        ? $"{Medicaments.Count} médicaments"
        : $"{Medicaments.Count} médicament";
    public string Reference => $"MR-{Date:yyyy-MMdd}-{Id:D4}";
}

public sealed class RendezVous
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Medecin { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string Lieu { get; set; } = "";
    public string Motif { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Telephone { get; set; } = "";
    public bool Rappel { get; set; } = true;

    public bool Passe => Date < DateTime.Now;
    public string JourNum => Date.Day.ToString("00");
    public string MoisAbrev => Date.ToString("MMM").Replace(".", "").ToUpperInvariant();
    public string Heure => Date.ToString("HH:mm");
    public string DateLongue => $"{Capitalise(Date.ToString("dddd d MMMM yyyy"))} · {Heure}";
    public string MedecinSpecialite => $"{Medecin} — {Specialite}";

    public string JoursRestants
    {
        get
        {
            int j = (int)Math.Ceiling((Date.Date - DateTime.Today).TotalDays);
            if (j < 0) return "Passé";
            if (j == 0) return "Aujourd'hui";
            if (j == 1) return "Demain";
            return $"J-{j}";
        }
    }

    public string Groupe
    {
        get
        {
            int j = (int)Math.Ceiling((Date.Date - DateTime.Today).TotalDays);
            if (j < 0) return "Passés";
            if (j <= 7) return "Cette semaine";
            if (j <= 30) return "Prochainement";
            return "Plus tard";
        }
    }

    static string Capitalise(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}

/// <summary>Tendance d'une semaine, pour la couleur de l'histogramme (jamais la couleur seule).</summary>
public enum TendanceSemaine { Bonne = 0, Partielle = 1, Manquee = 2 }

public sealed class SemaineStat
{
    public string Libelle { get; set; } = "";
    public int Validees { get; set; }
    public int Prevues { get; set; }
    public int Manquees { get; set; }
    public int Partielles { get; set; }
    public int Pourcent { get; set; }

    public string Compteur => $"{Validees} sur {Prevues} prises";
    public string PourcentTexte => $"{Pourcent} %";
    public int HauteurBarre => Math.Max(6, (int)Math.Round(Pourcent * 0.72));

    /// <summary>Libellé compact pour l'axe de l'histogramme (« 05/10 », « Cette sem. »).</summary>
    public string LibelleCourt
    {
        get
        {
            if (string.IsNullOrEmpty(Libelle)) return "";
            if (Libelle.StartsWith("Cette", StringComparison.OrdinalIgnoreCase)) return "Cette sem.";
            int i = Libelle.IndexOf("du ", StringComparison.OrdinalIgnoreCase);
            return i >= 0 ? Libelle[(i + 3)..].Trim() : Libelle;
        }
    }

    public TendanceSemaine Tendance =>
        Pourcent >= 90 ? TendanceSemaine.Bonne
        : Pourcent >= 75 ? TendanceSemaine.Partielle
        : TendanceSemaine.Manquee;
}

public sealed class Historique
{
    public int Jours { get; set; }
    public int Observance { get; set; }
    public int Validees { get; set; }
    public int Prevues { get; set; }
    public int Manquees { get; set; }
    public int Partielles { get; set; }
    public List<SemaineStat> Semaines { get; set; } = new();

    public string ObservanceTexte => $"{Observance} %";
    public string Resume => $"{Validees} sur {Prevues} prises";
    public bool Vide => Prevues == 0;
    public string NonPrises => Manquees == 0
        ? "Aucune dose non prise sur la période"
        : $"{Manquees} dose{(Manquees > 1 ? "s" : "")} non prise{(Manquees > 1 ? "s" : "")} sur la période";
}

public sealed class Reglage
{
    public bool SyncWifi { get; set; } = true;
    public bool RappelsActifs { get; set; } = true;
    public string PlageDebut { get; set; } = "22:00";
    public string PlageFin { get; set; } = "07:00";
    public bool Verrouillage { get; set; } = true;

    public string Plage => $"{PlageDebut} – {PlageFin}";
}

/// <summary>Jour du sélecteur de semaine (écran Mes prises).</summary>
public sealed class JourItem
{
    public DateTime Date { get; init; }
    public bool Selectionne { get; init; }
    public bool EstAujourdhui => Date.Date == DateTime.Today;
    public string Numero => Date.Day.ToString("00");
    public string Libelle => Date.ToString("ddd").Replace(".", "").ToUpperInvariant();
}
