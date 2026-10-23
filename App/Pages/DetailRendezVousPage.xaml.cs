using System.Text;
using MediRappel.Models;
using MediRappel.Services;

namespace MediRappel.Pages;

// NB : ne pas nommer la propriété « Id » : Element expose déjà Id, la résolution
// du paramètre de requête par reflexion lèverait une AmbiguousMatchException.
[QueryProperty(nameof(RendezVousId), "id")]
public partial class DetailRendezVousPage : ContentPage
{
    RendezVous? _rdv;

    public int RendezVousId { get; set; }

    public DetailRendezVousPage()
    {
        InitializeComponent();
        BindingContext = this;
        nav.DestinationChoisie += async (_, idx) => await AppShell.AllerOnglet(idx);
    }

    public string Jour { get; private set; } = "";
    public string Mois { get; private set; } = "";
    public string DateLongue { get; private set; } = "";
    public string MedecinSpecialite { get; private set; } = "";
    public string Lieu { get; private set; } = "";
    public string Telephone { get; private set; } = "";
    public string Motif { get; private set; } = "";

    string _notes = "";
    public string Notes
    {
        get => _notes;
        set { _notes = value; OnPropertyChanged(); }
    }

    bool _rappel = true;
    public bool Rappel
    {
        get => _rappel;
        set
        {
            _rappel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RappelTexte));
        }
    }

    public string RappelTexte => _rappel
        ? "Un rappel la veille vous sera envoyé"
        : "Aucun rappel programmé";

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        barre.HorsLigne = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
        nav.ActiveIndex = MainPage.OngletDemande;

        try { _rdv = await Medi.Data.RendezVous(RendezVousId); }
        catch (ApiException ex) { await Ui.Avertir("Rendez-vous indisponible", ex.Message); }

        if (_rdv is null)
        {
            await Ui.Avertir("Introuvable", "Ce rendez-vous n'existe plus.");
            await Shell.Current!.GoToAsync("..");
            return;
        }

        Jour = _rdv.JourNum;
        Mois = _rdv.MoisAbrev;
        DateLongue = _rdv.DateLongue;
        MedecinSpecialite = _rdv.MedecinSpecialite;
        Lieu = _rdv.Lieu;
        Telephone = string.IsNullOrWhiteSpace(_rdv.Telephone) ? "Non renseigné" : _rdv.Telephone;
        Motif = _rdv.Motif;
        Notes = _rdv.Notes;
        Rappel = _rdv.Rappel;

        RafraichirProprietes();
    }

    void RafraichirProprietes()
    {
        OnPropertyChanged(nameof(Jour));
        OnPropertyChanged(nameof(Mois));
        OnPropertyChanged(nameof(DateLongue));
        OnPropertyChanged(nameof(MedecinSpecialite));
        OnPropertyChanged(nameof(Lieu));
        OnPropertyChanged(nameof(Telephone));
        OnPropertyChanged(nameof(Motif));
        OnPropertyChanged(nameof(RappelTexte));
    }

    async void Retour(object? s, TappedEventArgs e) =>
        await Shell.Current!.GoToAsync("..");

    async void Itineraire(object? s, TappedEventArgs e)
    {
        if (_rdv is null) return;
        var q = Uri.EscapeDataString(_rdv.Lieu);
        await Launcher.Default.OpenAsync($"https://www.google.com/maps/search/?api=1&query={q}");
    }

    async void Appeler(object? s, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Telephone) || Telephone == "—")
        {
            await Ui.Avertir("Numéro indisponible", "Aucun numéro n'est renseigné pour ce praticien.");
            return;
        }
        await Launcher.Default.OpenAsync($"tel:{Telephone.Replace(" ", "")}");
    }

    async void Partager(object? s, TappedEventArgs e)
    {
        if (_rdv is null) return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Rendez-vous MediRappel",
            Subject = $"RDV {_rdv.Medecin}",
            Text = $"Rendez-vous avec {_rdv.Medecin} ({_rdv.Specialite})\n" +
                   $"{_rdv.DateLongue}\n{_rdv.Lieu}\nMotif : {_rdv.Motif}",
        });
    }

    async void Agenda(object? s, TappedEventArgs e)
    {
        if (_rdv is null) return;

        var debut = _rdv.Date;
        var fin = debut.AddHours(1);
        var ics = new StringBuilder()
            .AppendLine("BEGIN:VCALENDAR")
            .AppendLine("VERSION:2.0")
            .AppendLine("PRODID:-//MediRappel//FR")
            .AppendLine("BEGIN:VEVENT")
            .AppendLine($"UID:rdv-{_rdv.Id}@medirappel")
            .AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}")
            .AppendLine($"DTSTART:{debut:yyyyMMddTHHmmss}")
            .AppendLine($"DTEND:{fin:yyyyMMddTHHmmss}")
            .AppendLine($"SUMMARY:{_rdv.Medecin} - {_rdv.Specialite}")
            .AppendLine($"LOCATION:{_rdv.Lieu}")
            .AppendLine($"DESCRIPTION:{_rdv.Motif}")
            .AppendLine("END:VEVENT")
            .AppendLine("END:VCALENDAR")
            .ToString();

        var chemin = Path.Combine(FileSystem.CacheDirectory, $"rdv-{_rdv.Id}.ics");
        await File.WriteAllTextAsync(chemin, ics);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Ajouter à l'agenda",
            File = new ShareFile(chemin),
        });
    }

    async void Enregistrer(object? s, EventArgs e)
    {
        if (_rdv is null) return;
        try
        {
            var saisie = new RendezVousSaisie(
                _rdv.Date, _rdv.Medecin, _rdv.Specialite, _rdv.Lieu,
                _rdv.Motif, Notes, _rdv.Telephone, Rappel);
            await Medi.Data.EnregistrerRendezVous(_rdv.Id, saisie);
            await Ui.Avertir("Enregistré", "Le rendez-vous a été mis à jour.");
            await Shell.Current!.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            await Ui.Avertir("Enregistrement impossible", ex.Message);
        }
    }

    async void Supprimer(object? s, EventArgs e)
    {
        if (_rdv is null) return;
        if (!await Ui.Confirmer("Supprimer le rendez-vous",
                $"Supprimer le rendez-vous avec {_rdv.Medecin} ?", "Supprimer")) return;

        try
        {
            await Medi.Data.SupprimerRendezVous(_rdv.Id);
            await Shell.Current!.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            await Ui.Avertir("Suppression impossible", ex.Message);
        }
    }
}
