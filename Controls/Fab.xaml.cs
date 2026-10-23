using BindableObject = Microsoft.Maui.Controls.BindableObject;

namespace MediRappel.Controls;

public partial class Fab : ContentView
{
    public static readonly BindableProperty LibelleProperty =
        BindableProperty.Create(nameof(Libelle), typeof(string), typeof(Fab), "Scanner");

    public static readonly BindableProperty IconeProperty =
        BindableProperty.Create(nameof(Icone), typeof(string), typeof(Fab), "qr_code_scanner");

    public string Libelle { get => (string)GetValue(LibelleProperty); set => SetValue(LibelleProperty, value); }
    public string Icone { get => (string)GetValue(IconeProperty); set => SetValue(IconeProperty, value); }

    public event EventHandler? ActionCliquee;

    public Fab() => InitializeComponent();

    void Cliquer(object? s, TappedEventArgs e) => ActionCliquee?.Invoke(this, EventArgs.Empty);
}
