namespace MediRappel.Controls;

public partial class BarreSysteme : ContentView
{
    public static readonly BindableProperty HorsLigneProperty =
        BindableProperty.Create(nameof(HorsLigne), typeof(bool), typeof(BarreSysteme), false);

    // Heure désactivée : gérée par la barre système du téléphone

    public static readonly BindableProperty AccentProperty =
        BindableProperty.Create(nameof(Accent), typeof(string), typeof(BarreSysteme), "HORS LIGNE");

    public bool HorsLigne
    {
        get => (bool)GetValue(HorsLigneProperty);
        set => SetValue(HorsLigneProperty, value);
    }

    public string Accent
    {
        get => (string)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public BarreSysteme() => InitializeComponent();
}
