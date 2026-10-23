using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MediRappel.Models;
using MediRappel.Services;
using MediRappel.Vues;

namespace MediRappel.Pages;

/// <summary>
/// Ajout de prises. Le médicament peut être choisi parmi les médicaments des
/// ordonnances actives (dosage et posologie pré-remplis) ou saisi librement.
/// Le nombre de prises par jour se règle avec le compteur −/+ ; chaque heure
/// est éditable librement, avec des créneaux rapides (matin/midi/soir/coucher).
/// </summary>
public partial class NouvellePrisePage : ContentPage
{
    public const string AutreChoix = "Autre médicament…";

    static readonly Dictionary<string, int> JoursParLibelle = new()
    {
        ["Une seule fois"] = 1,
        ["Chaque jour · 1 semaine"] = 7,
        ["Chaque jour · 2 semaines"] = 14,
        ["Chaque jour · 1 mois"] = 30,
        ["Chaque jour · 3 mois"] = 90,
    };

    readonly List<Medicament> _catalogue = new();
    bool _pret;

    public NouvellePrisePage()
    {
        InitializeComponent();
        BindingContext = this;
        Prises.CollectionChanged += PrisesModifiees;
        AjouterPriseHoraire();
    }

    public ObservableCollection<string> ChoixMedicaments { get; } = new();
    public List<string> Repetitions { get; } = JoursParLibelle.Keys.ToList();

    /// <summary>Heures saisies : une par prise quotidienne (1 à <see cref="PriseHoraire.MaxParJour"/>).</summary>
    public ObservableCollection<PriseHoraire> Prises { get; } = new();

    public int NbPrises => Prises.Count;
    public bool Plusieurs => Prises.Count > 1;
    public bool PeutAjouter => Prises.Count < PriseHoraire.MaxParJour;
    public bool PeutRetirer => Prises.Count > 1;
    public string NbPrisesLibelle => Texte.Pluriel(Prises.Count, "prise par jour", "prises par jour");

    // ---- Champs liés ----
    string _medicamentChoisi = AutreChoix;
    public string MedicamentChoisi
    {
        get => _medicamentChoisi;
        set
        {
            if (_medicamentChoisi == value) return;
            _medicamentChoisi = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AfficheLibre));
            if (AfficheLibre)
            {
                // Saisie libre : on repart d'une fiche vierge.
                Dosage = "";
                Posologie = "";
                OnPropertyChanged(nameof(Dosage));
                OnPropertyChanged(nameof(Posologie));
            }
            else Prefill(value);
        }
    }

    public string MedicamentLibre { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Posologie { get; set; } = "";

    DateTime _dateDebut = DateTime.Today;
    public DateTime DateDebut
    {
        get => _dateDebut;
        set { _dateDebut = value; OnPropertyChanged(); OnPropertyChanged(nameof(Apercu)); }
    }

    string _repetition = "Une seule fois";
    public string Repetition
    {
        get => _repetition;
        set { _repetition = value; OnPropertyChanged(); OnPropertyChanged(nameof(Apercu)); }
    }

    public bool AfficheLibre => MedicamentChoisi == AutreChoix;
    public int Jours => JoursParLibelle.TryGetValue(Repetition, out var j) ? j : 1;

    public string Apercu
    {
        get
        {
            var heures = string.Join(" · ", Prises.Select(p => p.Libelle).OrderBy(h => h, StringComparer.Ordinal));
            int nb = Prises.Count;
            if (Jours <= 1)
                return $"{Texte.Pluriel(nb, "prise", "prises")} le {DateDebut:dd/MM/yyyy} à {heures}.";
            var fin = DateDebut.Date.AddDays(Jours - 1);
            return $"{nb} prises/jour × {Jours} jours = {nb * Jours} prises, " +
                   $"du {DateDebut:dd/MM/yyyy} au {fin:dd/MM/yyyy}, à {heures}.";
        }
    }

    public bool MedErreur { get; private set; }
    public bool ErreurVisible { get; private set; }
    public string ErreurMessage { get; private set; } = "";

    bool _occupe;
    public bool NonOccupe => !_occupe;
    public string BoutonTexte => _occupe ? "Enregistrement…" : "Enregistrer les prises";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
        if (_pret) return;
        _pret = true;

        try
        {
            var ordos = await Medi.Data.Ordonnances();
            _catalogue.Clear();
            foreach (var m in ordos.Where(o => o.IsActive).SelectMany(o => o.Medicaments))
                if (_catalogue.All(x => !string.Equals(x.Nom, m.Nom, StringComparison.OrdinalIgnoreCase)))
                    _catalogue.Add(m);
        }
        catch (ApiException) { /* pas de catalogue : saisie libre */ }

        ChoixMedicaments.Clear();
        ChoixMedicaments.Add(AutreChoix);
        foreach (var m in _catalogue) ChoixMedicaments.Add(m.Nom);

        MedicamentChoisi = _catalogue.Count > 0 ? _catalogue[0].Nom : AutreChoix;
    }

    void Prefill(string nom)
    {
        var m = _catalogue.FirstOrDefault(x => string.Equals(x.Nom, nom, StringComparison.OrdinalIgnoreCase));
        if (m is null) return;

        Dosage = m.Dosage;
        Posologie = m.Posologie;
        OnPropertyChanged(nameof(Dosage));
        OnPropertyChanged(nameof(Posologie));
    }

    // ---- Compteur de prises ----

    void AjouterPrise(object? s, EventArgs e)
    {
        if (Prises.Count >= PriseHoraire.MaxParJour) return;
        AjouterPriseHoraire();
    }

    void RetirerPrise(object? s, EventArgs e)
    {
        if (Prises.Count <= 1) return;
        Prises.RemoveAt(Prises.Count - 1);
    }

    void AjouterPriseHoraire() => Prises.Add(PriseHoraire.Creer(Prises.Select(p => p.Heure)));

    /// <summary>Supprime une heure précise (icône de la carte).</summary>
    void SupprimerPrise(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<PriseHoraire>(s) is not { } p) return;
        if (Prises.Count <= 1) return;
        Prises.Remove(p);
    }

    /// <summary>Applique un créneau rapide (matin/midi/soir/coucher) à l'heure de la ligne.</summary>
    void Presets(object? s, TappedEventArgs e)
    {
        if (VueAccueil.Trouver<PriseHoraire>(s) is not { } p) return;
        if (TimeSpan.TryParse(e.Parameter as string, out var t)) p.Heure = t;
    }

    void PrisesModifiees(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (PriseHoraire p in e.OldItems)
                p.PropertyChanged -= SurveillePrise;
        if (e.NewItems != null)
            foreach (PriseHoraire p in e.NewItems)
            {
                p.PropertyChanged -= SurveillePrise; // évite un double abonnement
                p.PropertyChanged += SurveillePrise;
            }

        bool sup = Prises.Count > 1;
        foreach (var p in Prises) p.Supprimable = sup;

        OnPropertyChanged(nameof(NbPrises));
        OnPropertyChanged(nameof(Plusieurs));
        OnPropertyChanged(nameof(PeutAjouter));
        OnPropertyChanged(nameof(PeutRetirer));
        OnPropertyChanged(nameof(NbPrisesLibelle));
        OnPropertyChanged(nameof(Apercu));
    }

    void SurveillePrise(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PriseHoraire.Heure))
            OnPropertyChanged(nameof(Apercu));
    }

    async void Enregistrer(object? s, EventArgs e)
    {
        var nom = AfficheLibre ? MedicamentLibre.Trim() : MedicamentChoisi;
        MedErreur = string.IsNullOrWhiteSpace(nom);
        ErreurVisible = false;
        OnPropertyChanged(nameof(MedErreur));
        OnPropertyChanged(nameof(ErreurVisible));
        if (MedErreur) return;

        var heures = Prises.Select(p => p.Libelle).ToList();
        if (heures.Distinct(StringComparer.Ordinal).Count() != heures.Count)
        {
            Signaler("Deux prises ont la même heure : modifiez l'une d'elles.");
            return;
        }

        if (!MediRappelData.EnLigne)
        {
            Signaler("L'ajout de prises nécessite une connexion.");
            return;
        }

        _occupe = true;
        Notifier();

        try
        {
            var saisie = new DoseSerieSaisie(
                nom, Dosage.Trim(), Posologie.Trim(),
                heures, "À prendre", DateDebut.Date, Jours);

            var creees = await Medi.Data.AjouterPrisesSerie(saisie);
            await Ui.Avertir("Prises ajoutées",
                creees.Count > 1
                    ? $"{creees.Count} prises ont été planifiées."
                    : "La prise a été ajoutée à votre planning.");
            await Shell.Current!.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            Signaler(ex.Message);
        }
        catch (Exception)
        {
            Signaler("Serveur injoignable. Vérifiez votre connexion, puis réessayez.");
        }
        finally
        {
            _occupe = false;
            Notifier();
        }
    }

    void Signaler(string message)
    {
        ErreurMessage = message;
        ErreurVisible = true;
        Notifier();
    }

    void Notifier()
    {
        OnPropertyChanged(nameof(MedErreur));
        OnPropertyChanged(nameof(ErreurVisible));
        OnPropertyChanged(nameof(ErreurMessage));
        OnPropertyChanged(nameof(NonOccupe));
        OnPropertyChanged(nameof(BoutonTexte));
    }

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");
}
