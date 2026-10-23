using MediRappel.Models;
using MediRappel.Services;

namespace MediRappel.Pages;

public partial class ReglagesPage : ContentPage
{
    Reglage _reglage = new();

    public ReglagesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string Initiales { get; private set; } = "?";
    public string Nom { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Depuis { get; private set; } = "";
    public string IconeReseau { get; private set; } = "wifi";
    public string EtatReseau { get; private set; } = "";

    public bool SyncWifi { get; private set; } = true;
    public bool RappelsActifs { get; private set; } = true;
    public bool Verrouillage { get; private set; } = true;
    public string ResumeRappels { get; private set; } = "";
    public string PlageSilence { get; private set; } = "";
    public string TailleCache { get; private set; } = "";

    bool _chargementUi;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Charger();
    }

    async Task Charger()
    {
        _chargementUi = true;

        MajReseau();

        try
        {
            var p = await Medi.Data.Profil();
            Nom = p.Nom;
            Email = p.Email;
            Initiales = p.Initiales;
            Depuis = p.Depuis;
        }
        catch (ApiException)
        {
            Nom = Session.Nom;
            Email = Session.Email;
            Initiales = Nom.Length > 0 ? Nom[..1].ToUpperInvariant() : "?";
            Depuis = "";
        }

        try { _reglage = await Medi.Data.Reglages(); }
        catch (ApiException) { _reglage = new Reglage(); }

        SyncWifi = _reglage.SyncWifi;
        RappelsActifs = _reglage.RappelsActifs;
        Verrouillage = _reglage.Verrouillage;
        MajTextes();
        RafraichirProprietes();

        _chargementUi = false;
    }

    void MajReseau()
    {
        bool enLigne = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        barre.HorsLigne = !enLigne;
        IconeReseau = enLigne ? "cloud_done" : "cloud_off";
        EtatReseau = enLigne
            ? "Connecté — les données sont synchronisées"
            : "Hors ligne — les dernières données connues sont affichées";
    }

    void MajTextes()
    {
        ResumeRappels = _reglage.RappelsActifs
            ? "Une notification à chaque prise"
            : "Rappels désactivés";
        PlageSilence = _reglage.PlageDebut == "00:00"
            ? "Désactivée"
            : $"{_reglage.PlageDebut} – {_reglage.PlageFin}";
        var octets = CacheLocal.Taille();
        TailleCache = octets < 1024
            ? $"{octets} o utilisés hors ligne"
            : $"{octets / 1024.0:0.0} Ko utilisés hors ligne";
    }

    void RafraichirProprietes()
    {
        OnPropertyChanged(nameof(Nom));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Initiales));
        OnPropertyChanged(nameof(Depuis));
        OnPropertyChanged(nameof(IconeReseau));
        OnPropertyChanged(nameof(EtatReseau));
        OnPropertyChanged(nameof(SyncWifi));
        OnPropertyChanged(nameof(RappelsActifs));
        OnPropertyChanged(nameof(Verrouillage));
        OnPropertyChanged(nameof(ResumeRappels));
        OnPropertyChanged(nameof(PlageSilence));
        OnPropertyChanged(nameof(TailleCache));
    }

    async Task Enregistrer()
    {
        _reglage.SyncWifi = SyncWifi;
        _reglage.RappelsActifs = RappelsActifs;
        _reglage.Verrouillage = Verrouillage;
        try { await Medi.Data.EnregistrerReglages(_reglage); }
        catch (ApiException ex) { await Ui.Avertir("Réglage non enregistré", ex.Message); }
    }

    async void SyncWifiToggled(object? s, ToggledEventArgs e)
    {
        if (_chargementUi) return;
        SyncWifi = e.Value;
        await Enregistrer();
    }

    async void RappelsToggled(object? s, ToggledEventArgs e)
    {
        if (_chargementUi) return;
        RappelsActifs = e.Value;
        MajTextes();
        OnPropertyChanged(nameof(ResumeRappels));
        await Enregistrer();
        await AppShell.ActiverRappels();
    }

    async void VerrouillageToggled(object? s, ToggledEventArgs e)
    {
        if (_chargementUi) return;
        Verrouillage = e.Value;
        await Enregistrer();
    }

    async void PlageTapee(object? s, TappedEventArgs e)
    {
        var choix = await Ui.Choix("Plage de silence",
            "21:00 – 06:00", "22:00 – 07:00", "23:00 – 07:00", "Aucune");
        if (choix is null) return;

        var parties = choix.Split('–');
        if (parties.Length == 2)
        {
            _reglage.PlageDebut = parties[0].Trim();
            _reglage.PlageFin = parties[1].Trim();
        }
        else
        {
            _reglage.PlageDebut = "00:00";
            _reglage.PlageFin = "00:00";
        }

        MajTextes();
        OnPropertyChanged(nameof(PlageSilence));
        await Enregistrer();
    }

    async void CacheTape(object? s, TappedEventArgs e)
    {
        var choix = await Ui.Choix("Données hors ligne",
            "Actualiser la taille", "Vider le cache");
        switch (choix)
        {
            case "Actualiser la taille":
                MajTextes();
                OnPropertyChanged(nameof(TailleCache));
                break;
            case "Vider le cache":
                if (!await Ui.Confirmer("Vider le cache",
                        "Les données hors ligne seront supprimées. Elles seront rechargées depuis le serveur.",
                        "Vider"))
                    return;
                CacheLocal.Vider();
                MajTextes();
                OnPropertyChanged(nameof(TailleCache));
                await Ui.Avertir("Cache vidé", "Les données hors ligne ont été supprimées.");
                break;
        }
    }

    async void Deconnecter(object? s, EventArgs e)
    {
        if (!await Ui.Confirmer("Se déconnecter",
                "Vous devrez vous reconnecter pour accéder à votre dossier.", "Se déconnecter"))
            return;

        Session.Deconnecter();
        CacheLocal.Vider();
        await AppShell.Aller("connexion");
    }

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");
}
