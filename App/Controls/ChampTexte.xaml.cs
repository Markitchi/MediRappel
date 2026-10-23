using BindableObject = Microsoft.Maui.Controls.BindableObject;

namespace MediRappel.Controls;

public partial class ChampTexte : ContentView
{
    public static readonly BindableProperty LibelleProperty =
        BindableProperty.Create(nameof(Libelle), typeof(string), typeof(ChampTexte), "");

    public static readonly BindableProperty ValeurProperty =
        BindableProperty.Create(nameof(Valeur), typeof(string), typeof(ChampTexte), "",
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty IndiceProperty =
        BindableProperty.Create(nameof(Indice), typeof(string), typeof(ChampTexte), "");

    public static readonly BindableProperty MotDePasseProperty =
        BindableProperty.Create(nameof(MotDePasse), typeof(bool), typeof(ChampTexte), false);

    public static readonly BindableProperty ErreurProperty =
        BindableProperty.Create(nameof(Erreur), typeof(string), typeof(ChampTexte), "");

    public static readonly BindableProperty ErreurVisibleProperty =
        BindableProperty.Create(nameof(ErreurVisible), typeof(bool), typeof(ChampTexte), false,
            propertyChanged: OnEtatChange);

    public static readonly BindableProperty LibelleVisibleProperty =
        BindableProperty.Create(nameof(LibelleVisible), typeof(bool), typeof(ChampTexte), true);

    public static readonly BindableProperty OeilVisibleProperty =
        BindableProperty.Create(nameof(OeilVisible), typeof(bool), typeof(ChampTexte), false);

    public static readonly BindableProperty IconeDroiteProperty =
        BindableProperty.Create(nameof(IconeDroite), typeof(string), typeof(ChampTexte), "visibility_off");

    public static readonly BindableProperty CleClavierProperty =
        BindableProperty.Create(nameof(CleClavier), typeof(string), typeof(ChampTexte), "Default",
            propertyChanged: OnCleChange);

    public static readonly BindableProperty AideProperty =
        BindableProperty.Create(nameof(Aide), typeof(string), typeof(ChampTexte), "");

    public static readonly BindableProperty AideVisibleProperty =
        BindableProperty.Create(nameof(AideVisible), typeof(bool), typeof(ChampTexte), false);

    public string Libelle { get => (string)GetValue(LibelleProperty); set => SetValue(LibelleProperty, value); }
    public string Valeur { get => (string)GetValue(ValeurProperty); set => SetValue(ValeurProperty, value); }
    public string Indice { get => (string)GetValue(IndiceProperty); set => SetValue(IndiceProperty, value); }
    public bool MotDePasse { get => (bool)GetValue(MotDePasseProperty); set => SetValue(MotDePasseProperty, value); }
    public string Erreur { get => (string)GetValue(ErreurProperty); set => SetValue(ErreurProperty, value); }
    public bool ErreurVisible { get => (bool)GetValue(ErreurVisibleProperty); set => SetValue(ErreurVisibleProperty, value); }
    public bool LibelleVisible { get => (bool)GetValue(LibelleVisibleProperty); set => SetValue(LibelleVisibleProperty, value); }
    public bool OeilVisible { get => (bool)GetValue(OeilVisibleProperty); set => SetValue(OeilVisibleProperty, value); }
    public string IconeDroite { get => (string)GetValue(IconeDroiteProperty); set => SetValue(IconeDroiteProperty, value); }
    public string CleClavier { get => (string)GetValue(CleClavierProperty); set => SetValue(CleClavierProperty, value); }
    public string Aide { get => (string)GetValue(AideProperty); set => SetValue(AideProperty, value); }
    public bool AideVisible { get => (bool)GetValue(AideVisibleProperty); set => SetValue(AideVisibleProperty, value); }

    public Entry Saisie => saisie;

    public ChampTexte()
    {
        InitializeComponent();
        AppliquerClavier(CleClavier);
    }

    static void OnCleChange(BindableObject b, object o, object n)
    {
        if (b is ChampTexte c) c.AppliquerClavier(n as string ?? "Default");
    }

    void AppliquerClavier(string cle)
    {
        saisie.Keyboard = cle switch
        {
            "Email" => Keyboard.Email,
            "Telephone" or "Phone" => Keyboard.Telephone,
            "Numerique" or "Numeric" => Keyboard.Numeric,
            "Url" => Keyboard.Url,
            _ => Keyboard.Default,
        };
    }

    static void OnEtatChange(BindableObject b, object o, object n)
    {
        var c = (ChampTexte)b;
        bool err = (bool)n;
        c.boite.Stroke = (Color)(err
            ? Application.Current!.Resources["Error"]
            : Application.Current!.Resources["Outline"]);
        c.boite.StrokeThickness = err ? 2 : 1.5;
    }

    void BasculerMdp(object? s, TappedEventArgs e)
    {
        MotDePasse = !MotDePasse;
        IconeDroite = MotDePasse ? "visibility" : "visibility_off";
    }
}
