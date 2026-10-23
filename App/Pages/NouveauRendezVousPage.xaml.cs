using MediRappel.Services;

namespace MediRappel.Pages;

public partial class NouveauRendezVousPage : ContentPage
{
    public NouveauRendezVousPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public DateTime DateSelection { get; set; } = DateTime.Today.AddDays(1);
    public TimeSpan HeureSelection { get; set; } = new(9, 0, 0);

    public string Medecin { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string Lieu { get; set; } = "";
    public string Telephone { get; set; } = "";
    public string Motif { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool Rappel { get; set; } = true;

    public bool MedecinErreur { get; private set; }
    public bool ErreurVisible { get; private set; }
    public string ErreurMessage { get; private set; } = "";

    bool _occupe;
    public bool NonOccupe => !_occupe;
    public string BoutonTexte => _occupe ? "Enregistrement…" : "Enregistrer le rendez-vous";

    protected override void OnAppearing()
    {
        base.OnAppearing();
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
    }

    async void Enregistrer(object? s, EventArgs e)
    {
        MedecinErreur = string.IsNullOrWhiteSpace(Medecin);
        ErreurVisible = false;
        Notifier();
        if (MedecinErreur) return;

        if (!MediRappelData.EnLigne)
        {
            Signaler("La création d'un rendez-vous nécessite une connexion.");
            return;
        }

        _occupe = true;
        Notifier();

        try
        {
            var date = DateSelection.Date + HeureSelection;
            var saisie = new RendezVousSaisie(
                date, Medecin.Trim(), Specialite.Trim(), Lieu.Trim(),
                Motif.Trim(), Notes, Telephone.Trim(), Rappel);
            await Medi.Data.EnregistrerRendezVous(null, saisie);
            await Ui.Avertir("Rendez-vous ajouté", "Le rendez-vous a été enregistré.");
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
        OnPropertyChanged(nameof(MedecinErreur));
        OnPropertyChanged(nameof(ErreurVisible));
        OnPropertyChanged(nameof(ErreurMessage));
        OnPropertyChanged(nameof(NonOccupe));
        OnPropertyChanged(nameof(BoutonTexte));
    }

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");
}
