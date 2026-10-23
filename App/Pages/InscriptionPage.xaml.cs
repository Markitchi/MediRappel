using MediRappel.Services;

namespace MediRappel.Pages;

public partial class InscriptionPage : ContentPage
{
    public InscriptionPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string Nom { get; set; } = "";
    public string Email { get; set; } = "";
    public string Mdp { get; set; } = "";
    public string Confirmation { get; set; } = "";
    public bool Accorde { get; set; }

    public bool NomErreur { get; private set; }
    public bool EmailErreur { get; private set; }
    public bool MdpErreur { get; private set; }
    public bool ConfErreur { get; private set; }
    public bool ErreurVisible { get; private set; }
    public string ErreurMessage { get; private set; } = "";

    bool _occupe;
    public bool Occupe
    {
        get => _occupe;
        private set
        {
            _occupe = value;
            OnPropertyChanged(nameof(Occupe));
            OnPropertyChanged(nameof(NonOccupe));
            OnPropertyChanged(nameof(BoutonTexte));
        }
    }
    public bool NonOccupe => !_occupe;
    public string BoutonTexte => _occupe ? "Création…" : "Créer mon compte";

    protected override void OnAppearing()
    {
        base.OnAppearing();
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
    }

    async void CreerCompte(object? s, EventArgs e)
    {
        NomErreur = EmailErreur = MdpErreur = ConfErreur = false;

        if (string.IsNullOrWhiteSpace(Nom) || Nom.Trim().Length < 2) NomErreur = true;
        if (!EmailValide(Email)) EmailErreur = true;
        if (Mdp.Length < 8) MdpErreur = true;
        if (Confirmation != Mdp) ConfErreur = true;

        Notifier();
        if (NomErreur || EmailErreur || MdpErreur || ConfErreur) return;

        if (!Accorde)
        {
            Signaler("Veuillez accepter les conditions d'utilisation pour continuer.");
            return;
        }

        Occupe = true;
        try
        {
            var auth = await Medi.Api.Register(new RegisterRequest(Email.Trim(), Nom.Trim(), Mdp));
            Session.Definir(auth, true);
            await Medi.Data.RejouerAttentes();
            await AppShell.Aller("main");
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
            Occupe = false;
        }
    }

    static bool EmailValide(string email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.Contains('@')
        && email.IndexOf('.', email.IndexOf('@')) > email.IndexOf('@') + 1;

    void Signaler(string message)
    {
        ErreurMessage = message;
        ErreurVisible = true;
        Notifier();
    }

    void Notifier()
    {
        OnPropertyChanged(nameof(NomErreur));
        OnPropertyChanged(nameof(EmailErreur));
        OnPropertyChanged(nameof(MdpErreur));
        OnPropertyChanged(nameof(ConfErreur));
        OnPropertyChanged(nameof(ErreurVisible));
        OnPropertyChanged(nameof(ErreurMessage));
    }

    async void VersConnexion(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");
}
