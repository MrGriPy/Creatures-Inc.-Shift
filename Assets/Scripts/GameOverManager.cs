using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; 
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [System.Serializable]
    public struct InfoMonstre
    {
        public string nom;
        public GameObject modele3D; 
        [TextArea] public string conseil;
    }

    [Header("--- Caméras à Couper/Allumer ---")]
    public Camera mainCamera;
    [Tooltip("Glisse ici le dossier parent qui contient Left, Center et Right")]
    public GameObject groupeCamerasBureau; 
    public Camera gameOverCamera;

    [Header("--- UI à Couper/Allumer ---")]
    [Tooltip("Le HUD du joueur (Horloge, batterie, boutons...)")]
    public GameObject hudDeJeu;
    [Tooltip("L'écran noir avec le texte de mort et le bouton")]
    public GameObject gameOverPanel;
    public Text texteConseil;

    [Header("--- Manette / Joystick ---")]
    [Tooltip("Glisse le bouton RECOMMENCER ici")]
    public GameObject boutonAFocus;

    [Header("--- Liste des Monstres ---")]
    public InfoMonstre[] listeMonstres; 

    void Awake() 
    { 
        if (instance == null) instance = this; 
        else Destroy(gameObject); 
    }

    void Start()
    {
        // We make sure the Game Over screen is properly hidden when playing
        if(gameOverCamera != null) gameOverCamera.gameObject.SetActive(false);
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void DeclencherGameOver(int indexMonstre)
    {
        // 1. We freeze the game
        Time.timeScale = 0;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. WE TURN OFF EVERYTHING THAT BELONGS TO THE GAME
        if(mainCamera != null) mainCamera.gameObject.SetActive(false);
        if(groupeCamerasBureau != null) groupeCamerasBureau.SetActive(false);
        if(hudDeJeu != null) hudDeJeu.SetActive(false);

        // 3. WE TURN ON EVERYTHING THAT BELONGS TO GAME OVER
        if(gameOverCamera != null) gameOverCamera.gameObject.SetActive(true);
        if(gameOverPanel != null) gameOverPanel.SetActive(true);

        // 4. Monster display
        if (listeMonstres != null && indexMonstre < listeMonstres.Length)
        {
            if(listeMonstres[indexMonstre].modele3D != null)
                listeMonstres[indexMonstre].modele3D.SetActive(true);
            if(texteConseil != null)
                texteConseil.text = listeMonstres[indexMonstre].conseil;
        }

        // 5. Coroutine call to properly select the button
        if (boutonAFocus != null)
        {
            StartCoroutine(ForcerFocusBouton());
        }
        else
        {
            Debug.LogWarning("Attention : Le bouton à focus n'est pas assigné dans le GameOverManager !");
        }
    }

    private IEnumerator ForcerFocusBouton()
    {
        // WaitForEndOfFrame works even if Time.timeScale = 0
        yield return new WaitForEndOfFrame();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null); // We clear the current selection just in case
            EventSystem.current.SetSelectedGameObject(boutonAFocus); // We target the button
            Debug.Log("<color=cyan>Focus forcé sur le bouton : " + boutonAFocus.name + "</color>");
        }
    }

    public void BoutonRecommencer() 
    { 
        Time.timeScale = 1; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
    
    public void BoutonMenu() 
    { 
        Time.timeScale = 1; 
        SceneManager.LoadScene(0); 
    }
}