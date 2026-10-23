using BindableObject = Microsoft.Maui.Controls.BindableObject;

namespace MediRappel.Controls;

/// <summary>
/// Barre de navigation inférieure. L'état actif est signalé par la forme
/// (pastille) + le gras du label, pas par la couleur seule.
/// </summary>
public partial class BarreNavigation : ContentView
{
    public static readonly BindableProperty ActiveIndexProperty =
        BindableProperty.Create(nameof(ActiveIndex), typeof(int), typeof(BarreNavigation), 0,
            propertyChanged: OnActiveChanged);

    public event EventHandler<int>? DestinationChoisie;

    public int ActiveIndex
    {
        get => (int)GetValue(ActiveIndexProperty);
        set => SetValue(ActiveIndexProperty, value);
    }

    public BarreNavigation()
    {
        InitializeComponent();

        var tab = new (VerticalStackLayout v, Border pill, Label icon, Label lbl)[]
        {
            (i0, p0, ic0, l0),
            (i1, p1, ic1, l1),
            (i2, p2, ic2, l2),
            (i3, p3, ic3, l3),
        };

        for (int k = 0; k < tab.Length; k++)
        {
            int idx = k;
            var t = new TapGestureRecognizer();
            t.Tapped += (_, _) =>
            {
                ActiveIndex = idx;
                DestinationChoisie?.Invoke(this, idx);
            };
            tab[k].v.GestureRecognizers.Add(t);
        }

        // État initial : appliqué explicitement car la valeur par défaut (0)
        // n'entraîne aucun changement observable pour le moteur de liaison.
        Appliquer(ActiveIndex);
    }

    static void OnActiveChanged(BindableObject b, object oldValue, object newValue)
    {
        if (b is not BarreNavigation nav) return;
        nav.Appliquer((int)newValue);
    }

    void Appliquer(int actif)
    {
        // Pendant l'analyse du XAML, les champs peuvent ne pas encore exister.
        if (p0 is null || p1 is null || p2 is null || p3 is null) return;

        var tab = new (Border pill, Label icon, Label lbl)[]
        {
            (p0, ic0, l0), (p1, ic1, l1), (p2, ic2, l2), (p3, ic3, l3)
        };

        for (int k = 0; k < tab.Length; k++)
        {
            bool on = k == actif;
            tab[k].pill.BackgroundColor = on
                ? (Color)Application.Current!.Resources["SecondaryContainer"]
                : Colors.Transparent;
            tab[k].icon.TextColor = on
                ? (Color)Application.Current!.Resources["OnSecondaryContainer"]
                : (Color)Application.Current!.Resources["OnSurfaceVariant"];
            tab[k].lbl.TextColor = on
                ? (Color)Application.Current!.Resources["OnSecondaryContainer"]
                : (Color)Application.Current!.Resources["OnSurfaceVariant"];
            tab[k].lbl.FontAttributes = on ? FontAttributes.Bold : FontAttributes.None;
        }
    }
}
