namespace EchoduKarma.Scripts.UI;

/// <summary>Onglet affiché dans <see cref="GameMenuShell"/>.</summary>
public interface IGameMenuTabPage
{
    void OnTabShown();
    void OnTabHidden();
    void FocusDefault();
    /// <summary>Retour (Échap) : true si géré (ex. fermer un sous-écran détail).</summary>
    bool TryHandleCancel();
}
