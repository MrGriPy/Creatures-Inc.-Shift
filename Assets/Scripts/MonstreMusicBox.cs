using System.Collections;
using UnityEngine;

public class MonstreMusicBox : MonoBehaviour
{
    [Header("--- Paramètres de l'Attaque ---")]
    public int monstreIndex = 1; 
    [Tooltip("Temps avant que le jumpscare ne se déclenche une fois la boîte vide")]
    public float delaiAvantAttaque = 5f; 
    [Tooltip("Temps total de l'animation avant l'écran noir")]
    public float dureeJumpscare = 1.5f;

    [Header("--- Évolution de la Difficulté ---")]
    [Tooltip("Durée totale de ta nuit en secondes (ex: 360s pour 6 minutes)")]
    public float dureeTotaleNuit = 360f;

    [Space(5)]
    [Tooltip("Vitesse de vidage au début (ex: 0.05)")]
    public float vitesseVidageDebut = 0.05f;
    [Tooltip("Vitesse de vidage à la fin (ex: 0.25)")]
    public float vitesseVidageFin = 0.25f;

    [Header("--- Réglages de la Chute (Attaque par le haut) ---")]
    public float distanceZ = 0.6f; 
    public float startY = 2.5f; 
    public float endY = -0.2f;

    [Header("--- Références ---")]
    public PlayerActionManager playerManager;
    public GameObject jumpscareModel;
    public AudioSource jumpscareSound;

    private bool enChasse = false;
    private bool attaqueDeclenchee = false;
    private float tempsPasseDansLaNuit = 0f;

    void Start()
    {
        if (jumpscareModel != null) jumpscareModel.SetActive(false);
        
        // Initialization of the depletion speed
        if (playerManager != null)
            playerManager.musicBoxDepletionSpeed = vitesseVidageDebut;

        Debug.Log("<b>[Monstre 2] Initialisé. La boîte commencera à se vider doucement.</b>");
    }

    void Update()
    {
        if (playerManager == null || attaqueDeclenchee) return;

        // --- INCREASING DIFFICULTY MANAGEMENT ---
        tempsPasseDansLaNuit += Time.deltaTime;
        float progression = Mathf.Clamp01(tempsPasseDansLaNuit / dureeTotaleNuit);

        // We adjust the depletion speed in PlayerActionManager
        playerManager.musicBoxDepletionSpeed = Mathf.Lerp(vitesseVidageDebut, vitesseVidageFin, progression);
        // ------------------------------------------

        // Checking the defeat condition (Music box empty)
        if (playerManager.GetIsMusicBoxEmpty() && !enChasse)
        {
            enChasse = true;
            Debug.Log("<color=orange><b>[Monstre 2] BOÎTE VIDE !</b> Le monstre sort du plafond dans " + delaiAvantAttaque + "s.</color>");
            StartCoroutine(SequenceAttaque());
        }
    }

    private IEnumerator SequenceAttaque()
    {
        // 1. Suspense phase
        yield return new WaitForSeconds(delaiAvantAttaque);

        // 2. Trigger
        attaqueDeclenchee = true;
        
        if (playerManager != null) playerManager.ForceCloseTablet();

        if (jumpscareModel != null)
        {
            jumpscareModel.transform.localPosition = new Vector3(0, startY, distanceZ);
            jumpscareModel.SetActive(true);
        }

        if (jumpscareSound != null) jumpscareSound.Play();

        // 3. ANIMATION: The monster falls violently
        float elapsed = 0;
        float moveDuration = 0.3f; 

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / moveDuration;
            
            float currentY = Mathf.Lerp(startY, endY, percent);
            
            if (jumpscareModel != null)
            {
                jumpscareModel.transform.localPosition = new Vector3(0, currentY, distanceZ);
            }
            
            yield return null; 
        }

        // 4. We wait for the end
        yield return new WaitForSeconds(dureeJumpscare - moveDuration);

        // 5. Game Over
        GameOverManager gameOverManager = Object.FindFirstObjectByType<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.DeclencherGameOver(monstreIndex);
        }
    }
}