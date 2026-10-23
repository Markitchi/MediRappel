# MediRappel — application .NET MAUI

Application Android (et iOS/macOS/Windows sur les postes compatibles) reconstruisant
les 10 écrans du dossier de conception Figma (`../maquettes`, `../export`).

- **Cible principale** : `net10.0-android` (API 36)
- **Design** : Material Design 3, palette teal `#00696D` (voir `Resources/Styles/Colors.xaml`)
- **Hors ligne d'abord** : aucune donnée ne quitte l'appareil dans cette maquette interactive

## Ouvrir le projet dans VS Code

1. Ouvrir le dossier **`MediRappel/App`** dans VS Code :

   ```bash
   code /home/markitchi/Downloads/Act5/MediRappel/App
   ```

2. Installer les extensions recommandées (VS Code les propose automatiquement) :

   | Extension | Rôle |
   |---|---|
   | `ms-dotnettools.csdevkit` | C# Dev Kit (IntelliSense, build) |
   | `ms-dotnettools.dotnet-maui` | **Lancement de l'app + émulateur Android depuis VS Code** |
   | `ms-dotnettools.csharp` | Support C# |
   | `redhat.vscode-xml` | Validation des fichiers `.xaml` |

   La configuration est déjà présente dans `.vscode/` (extensions, tâches, lancement,
   variables d'environnement Android).

## Créer l'émulateur Android (une seule fois)

L'image système `system-images;android-36;google_apis;x86_64` doit être installée.
Un script fait tout :

```bash
bash /home/markitchi/Downloads/Act5/tools/emulateur.sh --creer
```

Ou depuis VS Code : `Ctrl+Maj+P` → **Tasks: Run Task** →
**MediRappel · Créer l'émulateur (AVD)**.

## Lancer l'application

### Depuis VS Code (recommandé)

1. Ouvrir le panneau **Run and Debug** (`Ctrl+Maj+D`).
2. Choisir **MediRappel (Android)**.
3. Appuyer sur **F5**.

   L'extension .NET MAUI compile, démarre un émulateur si nécessaire et déploie l'app.

### En ligne de commande

```bash
# 1. démarrer l'émulateur
bash /home/markitchi/Downloads/Act5/tools/emulateur.sh --demarrer

# 2. compiler + installer + lancer l'app
export ANDROID_HOME=/home/markitchi/android-sdk
export JAVA_HOME=/home/markitchi/.jdk
export PATH=$JAVA_HOME/bin:$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH

cd /home/markitchi/Downloads/Act5/MediRappel/App
dotnet build -t:Run -f net10.0-android -c Debug
```

## Arborescence du code

```
App/
├── App.xaml / AppShell.xaml        # application + routes de navigation
├── MauiProgram.cs                  # enregistrement des polices (dont Material Icons)
├── Models/Modeles.cs               # Dose, Ordonnance, RendezVous, Prise…
├── Services/Donnees.cs             # jeu de données de démonstration (hors ligne)
├── Converters/Converters.cs        # état de dose → couleur + icône (jamais la couleur seule)
├── Controls/                       # composants partagés
│   ├── BarreSysteme.xaml           # barre d'état 9:41 + indicateur hors ligne
│   ├── BarreNavigation.xaml        # navigation inférieure (4 onglets)
│   ├── BandeauReseau.xaml          # bandeau « HORS LIGNE »
│   ├── ChampTexte.xaml             # champ outlined M3 à label flottant
│   └── Fab.xaml                    # bouton d'action flottant étendu
├── Vues/                           # les 4 onglets racine
│   ├── VueAccueil.xaml             # écran 03
│   ├── VueOrdonnances.xaml         # écran 04
│   ├── VueRendezVous.xaml          # écran 07
│   └── VueHistorique.xaml          # écran 09
└── Pages/                          # écrans poussés
    ├── ConnexionPage.xaml          # écran 01
    ├── InscriptionPage.xaml        # écran 02
    ├── DetailOrdonnancePage.xaml   # écran 05
    ├── MesPrisesPage.xaml          # écran 06
    ├── DetailRendezVousPage.xaml   # écran 08
    └── ReglagesPage.xaml           # écran 10
```

## Correspondance écrans ↔ maquettes

| # | Maquette | Écran MAUI |
|---|---|---|
| 01 | `01-connexion.html` | `Pages/ConnexionPage.xaml` |
| 02 | `02-inscription.html` | `Pages/InscriptionPage.xaml` |
| 03 | `03-accueil.html` | `Vues/VueAccueil.xaml` |
| 04 | `04-ordonnances.html` | `Vues/VueOrdonnances.xaml` |
| 05 | `05-detail-ordonnance.html` | `Pages/DetailOrdonnancePage.xaml` |
| 06 | `06-mes-prises.html` | `Pages/MesPrisesPage.xaml` |
| 07 | `07-rendez-vous.html` | `Vues/VueRendezVous.xaml` |
| 08 | `08-detail-rendez-vous.html` | `Pages/DetailRendezVousPage.xaml` |
| 09 | `09-historique.html` | `Vues/VueHistorique.xaml` |
| 10 | `10-reglages.html` | `Pages/ReglagesPage.xaml` |

## Accessibilité (reprise de la charte)

- Aucune information portée par la **seule couleur** : chaque état associe
  couleur + icône + libellé (voir `Converters/Converters.cs`).
- Tailles de texte et espacements alignés sur l'échelle M3 de `Styles.xaml`.
- Contrastes issus de la charte vérifiée WCAG 2.1 AA (`tools/contraste.py`).
