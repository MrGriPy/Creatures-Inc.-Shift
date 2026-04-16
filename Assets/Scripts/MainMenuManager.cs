using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // NEW: Essential for controlling the controller/keyboard

public class MainMenuManager : MonoBehaviour
{
    [Header("--- Scènes ---")]
    public string nomSceneJeu = "SampleScene";

    [Header("--- Les Panneaux (Groupes) ---")]
    public GameObject menuPrincipal;
    public GameObject ecranGuide;
    public GameObject ecranGalerie;
    public GameObject ecranCredits;

    [Header("--- Navigation Manette (Premier Bouton à sélectionner) ---")]
    // NEW: We specify which button to select when opening a screen
    public GameObject btnJouer;         // The default button of the main menu
    public GameObject btnRetourGuide;    // The back button of the Guide
    public GameObject btnRetourGalerie; // The back button of the Gallery
    public GameObject btnRetourCredits; // The back button of the Credits

    void Start()
    {
        AfficherMenuPrincipal();
    }

    public void AfficherMenuPrincipal()
    {
        menuPrincipal.SetActive(true);
        ecranGuide.SetActive(false);
        ecranGalerie.SetActive(false);
        ecranCredits.SetActive(false);

        // NEW: We reset the cursor to "PLAY" when returning to the menu
        EventSystem.current.SetSelectedGameObject(null); // We clear the selection
        EventSystem.current.SetSelectedGameObject(btnJouer);
    }

    public void OuvrirGuide()
    {
        menuPrincipal.SetActive(false);
        ecranGuide.SetActive(true);

        // NEW: We force the cursor to the BACK button of the Guide
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(btnRetourGuide);
    }

    public void OuvrirGalerie()
    {
        menuPrincipal.SetActive(false);
        ecranGalerie.SetActive(true);

        // NEW: We force the cursor to the BACK button of the Gallery
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(btnRetourGalerie);
    }

    public void OuvrirCredits()
    {
        menuPrincipal.SetActive(false);
        ecranCredits.SetActive(true);

        // NEW: We force the cursor to the BACK button of the Credits
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(btnRetourCredits);
    }

    public void LancerJeu()
    {
        SceneManager.LoadScene(nomSceneJeu);
    }

    public void QuitterJeu()
    {
        Application.Quit();
    }
}