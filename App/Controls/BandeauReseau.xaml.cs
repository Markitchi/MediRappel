using BindableObject = Microsoft.Maui.Controls.BindableObject;

namespace MediRappel.Controls;

public partial class BandeauReseau : ContentView
{
    public static readonly BindableProperty AfficherProperty =
        BindableProperty.Create(nameof(Afficher), typeof(bool), typeof(BandeauReseau), true);

    public static readonly BindableProperty TitreProperty =
        BindableProperty.Create(nameof(Titre), typeof(string), typeof(BandeauReseau), "HORS LIGNE");

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(BandeauReseau), "");

    public static readonly BindableProperty TexteActionProperty =
        BindableProperty.Create(nameof(TexteAction), typeof(string), typeof(BandeauReseau), "");

    public static readonly BindableProperty ActionVisibleProperty =
        BindableProperty.Create(nameof(ActionVisible), typeof(bool), typeof(BandeauReseau), false);

    public static readonly BindableProperty DepliableProperty =
        BindableProperty.Create(nameof(Depliable), typeof(bool), typeof(BandeauReseau), true);

    public event EventHandler? ActionCliquee;

    public bool Afficher { get => (bool)GetValue(AfficherProperty); set => SetValue(AfficherProperty, value); }
    public string Titre { get => (string)GetValue(TitreProperty); set => SetValue(TitreProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string TexteAction { get => (string)GetValue(TexteActionProperty); set => SetValue(TexteActionProperty, value); }
    public bool ActionVisible { get => (bool)GetValue(ActionVisibleProperty); set => SetValue(ActionVisibleProperty, value); }
    public bool Depliable { get => (bool)GetValue(DepliableProperty); set => SetValue(DepliableProperty, value); }

    public BandeauReseau()
    {
        InitializeComponent();
        var t = new TapGestureRecognizer();
        t.Tapped += (_, _) =>
        {
            if (ActionVisible) ActionCliquee?.Invoke(this, EventArgs.Empty);
        };
        ((View)Content).GestureRecognizers.Add(t);
    }
}
