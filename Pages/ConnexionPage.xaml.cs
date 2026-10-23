using MediRappel.Services;

namespace MediRappel.Pages;

public partial class ConnexionPage : ContentPage
{
    public ConnexionPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    // Compte d'évaluation : l'adresse est pré-remplie pour aller plus vite,
    // le mot de passe reste à saisir.
    public string Email { get; set; } = "test@medirappel.fr";
    public string Mdp { get; set; } = "";
    public bool Souvenir { get; set; } = true;

    public bool EmailErreur { get; private set; }
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
    public string BoutonTexte => _occupe ? "Connexion…" : "Se connecter";

    protected override void OnAppearing()
    {
        base.OnAppearing();
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
    }

    async void Connexion(object? s, EventArgs e)
    {
        ErreurVisible = false;
        EmailErreur = false;
        OnPropertyChanged(nameof(ErreurVisible));
        OnPropertyChanged(nameof(EmailErreur));

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
        {
            EmailErreur = true;
            OnPropertyChanged(nameof(EmailErreur));
            return;
        }
        if (string.IsNullOrWhiteSpace(Mdp))
        {
            ErreurMessage = "Saisissez votre mot de passe.";
            ErreurVisible = true;
            OnPropertyChanged(nameof(ErreurVisible));
            OnPropertyChanged(nameof(ErreurMessage));
            return;
        }

        Occupe = true;
        try
        {
            var auth = await Medi.Api.Login(new LoginRequest(Email.Trim(), Mdp));
            Session.Definir(auth, Souvenir);
            await Medi.Data.RejouerAttentes();
            await AppShell.Aller("main");
            await AppShell.ActiverRappels();
        }
        catch (ApiException ex)
        {
            ErreurMessage = ex.Message;
            ErreurVisible = true;
            OnPropertyChanged(nameof(ErreurVisible));
            OnPropertyChanged(nameof(ErreurMessage));
        }
        catch (Exception)
        {
            ErreurMessage = "Serveur injoignable. Vérifiez votre connexion, puis réessayez.";
            ErreurVisible = true;
            OnPropertyChanged(nameof(ErreurVisible));
            OnPropertyChanged(nameof(ErreurMessage));
        }
        finally
        {
            Occupe = false;
        }
    }

    async void VersInscription(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("inscription");

    async void OubliMdp(object? s, TappedEventArgs e)
    {
        var email = await Ui.Saisir("Mot de passe oublié",
            "Indiquez l'adresse e-mail de votre compte.", Email, "Envoyer", Keyboard.Email);
        if (string.IsNullOrWhiteSpace(email)) return;

        try
        {
            await Medi.Api.MotDePasseOublie(email.Trim());
            await Ui.Avertir("Demande envoyée",
                "Si un compte existe pour cette adresse, les instructions de réinitialisation vous seront transmises.");
        }
        catch (Exception)
        {
            await Ui.Avertir("Envoi impossible", "Vérifiez votre connexion, puis réessayez.");
        }
    }
}
