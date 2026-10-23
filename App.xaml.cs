using Microsoft.Extensions.DependencyInjection;

namespace MediRappel;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// La charte MediRappel est claire uniquement : on verrouille le thème
		// pour éviter que le mode sombre du téléphone n'écrase les couleurs
		// (texte système clair sur fonds clairs = illisible).
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}