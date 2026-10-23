namespace MediRappel.Services;

/// <summary>Petits utilitaires d'interface (dialogues, état d'attente).</summary>
public static class Ui
{
    static Page Page => Shell.Current;

    public static Task Avertir(string titre, string message, string ok = "OK") =>
        Page.DisplayAlertAsync(titre, message, ok);

    public static Task<bool> Confirmer(string titre, string message,
        string ok = "Confirmer", string annuler = "Annuler") =>
        Page.DisplayAlertAsync(titre, message, ok, annuler);

    /// <summary>Feuille d'actions ; renvoie null si l'utilisateur annule.</summary>
    public static Task<string?> Choix(string titre, params string[] options) =>
        Page.DisplayActionSheetAsync(titre, "Annuler", null, options);

    public static Task<string?> Saisir(string titre, string message,
        string initial = "", string ok = "Valider", Keyboard? clavier = null) =>
        Page.DisplayPromptAsync(titre, message, ok, "Annuler", initialValue: initial,
            keyboard: clavier ?? Keyboard.Default);
}
