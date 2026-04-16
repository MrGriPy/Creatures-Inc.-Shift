using UnityEngine;
using System.Collections;

public class Monstre : MonoBehaviour
{
    [Header("--- Identité ---")]
    public string animatronicName = "CapsuleTueur";
    public int monstreIndex = 0;

    [Header("--- Évolution de la Difficulté ---")]
    [Tooltip("Durée totale de ta nuit en secondes (ex: 360s pour 6 minutes)")]
    public float dureeTotaleNuit = 360f;

    [Space(5)]
    public int aiLevelDebut = 2;   
    public int aiLevelFin = 12;    

    [Space(5)]
    [Tooltip("Toutes les X secondes, il tente de bouger")]
    public float intervalleDebut = 8.0f; 
    public float intervalleFin = 4.0f;    

    // These variables are hidden, they are managed by the code in real time
    [HideInInspector] public int aiLevel;
    [HideInInspector] public float moveInterval;
    
    [Header("--- JUMPSCARE (ANIMÉ) ---")]
    public GameObject jumpscareModel; 
    public AudioSource jumpscareSound;
    public float durationJumpscare = 1.5f;
    [Tooltip("Vitesse à laquelle il nous saute dessus")]
    public float jumpSpeed = 5f;
    [Tooltip("Position Z de départ (un peu loin)")]
    public float startZ = 2.5f;
    [Tooltip("Position Z d'arrivée (dans le nez)")]
    public float endZ = 0.4f;

    [Header("--- Visuel Déplacement ---")]
    public GameObject animatronicModel; 
    public Transform[] visualPositions;
    public int[] cameraPathIndices; 

    [Header("--- Références ---")]
    [SerializeField] private PlayerActionManager playerManager;
    public bool attacksFromLeft = true; 

    private float moveTimer;
    private int currentPathIndex = 0; 
    private float tempsPasseDansLaNuit = 0f; 
    
    void Start()
    {
        if(jumpscareModel) jumpscareModel.SetActive(false);
        
        // Difficulty initialization at the starting level
        aiLevel = aiLevelDebut;
        moveInterval = intervalleDebut;

        moveTimer = moveInterval;
        currentPathIndex = 0;
        UpdateVisualPosition();

        Debug.Log($"<b>[{animatronicName}] Initialisé. Début du cycle de difficulté.</b>");
    }

    void Update()
    {
        // We stop everything if we don't have the manager (safety)
        if (playerManager == null) return;

        // --- INCREASING DIFFICULTY MANAGEMENT ---
        tempsPasseDansLaNuit += Time.deltaTime;
        float progression = Mathf.Clamp01(tempsPasseDansLaNuit / dureeTotaleNuit);

        aiLevel = (int)Mathf.Lerp(aiLevelDebut, aiLevelFin, progression);
        moveInterval = Mathf.Lerp(intervalleDebut, intervalleFin, progression);
        // ------------------------------------------

        HandleMovementLogic();
    }

    private void HandleMovementLogic()
    {
        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0)
        {
            moveTimer = moveInterval; 
            TryToMove();
        }
    }

    private void TryToMove()
    {
        int diceRoll = Random.Range(1, 21);
        Debug.Log($"<i>[{animatronicName}] Tentative... Tirage: {diceRoll} / Niveau IA actuel: {aiLevel} / Intervalle actuel: {moveInterval:F1}s</i>");
        
        if (diceRoll <= aiLevel) 
        {
            MoveForward();
        }
        else
        {
             Debug.Log($"<i>[{animatronicName}] Échec de la tentative. Il reste sur place.</i>");
        }
    }

    private void MoveForward()
    {
        DeclencherBrouillage();
        currentPathIndex++;
        
        Debug.Log($"<color=yellow><b>[{animatronicName}] AVANCE !</b> Étape {currentPathIndex}/{visualPositions.Length}</color>");
        
        UpdateVisualPosition();

        if (currentPathIndex >= visualPositions.Length)
        {
            AttemptAttack();
        }
    }

    private void UpdateVisualPosition()
    {
        if (animatronicModel == null || visualPositions == null) return;
        if (currentPathIndex < visualPositions.Length)
        {
            Transform targetPoint = visualPositions[currentPathIndex];
            if (targetPoint != null)
            {
                animatronicModel.transform.position = targetPoint.position;
                animatronicModel.transform.rotation = targetPoint.rotation;
                animatronicModel.SetActive(true);
            }
        }
    }

    private void AttemptAttack()
    {
        bool isDoorClosed = playerManager.IsDoorClosed(attacksFromLeft);
        if (isDoorClosed)
        {
            Debug.Log($"<color=green><b>[{animatronicName}] BLOQUÉ !</b> La porte est fermée. Il retourne à la case départ.</color>");
            DeclencherBrouillage();
            currentPathIndex = 0; 
            UpdateVisualPosition(); 
        }
        else
        {
            Debug.Log($"<color=red><b>[{animatronicName}] ATTAQUE !</b> La porte était ouverte.</color>");
            StartCoroutine(PlayJumpscareSequence());
        }
    }

    IEnumerator PlayJumpscareSequence()
    {
        if (playerManager != null) playerManager.ForceCloseTablet();

        if (jumpscareModel)
        {
            jumpscareModel.transform.localPosition = new Vector3(0, -0.2f, startZ);
            jumpscareModel.SetActive(true);
        }
        
        if (jumpscareSound) jumpscareSound.Play();

        float elapsed = 0;
        float moveDuration = 0.3f; 

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / moveDuration;
            
            float currentZ = Mathf.Lerp(startZ, endZ, percent);
            
            if (jumpscareModel)
                jumpscareModel.transform.localPosition = new Vector3(0, -0.2f, currentZ);
            
            yield return null; 
        }

        yield return new WaitForSeconds(durationJumpscare - moveDuration);

        if (jumpscareModel) jumpscareModel.SetActive(false);

        if (GameOverManager.instance != null)
            GameOverManager.instance.DeclencherGameOver(monstreIndex);
    }

    public void DeclencherBrouillage()
    {
        if (CameraStaticEffect.instance != null) CameraStaticEffect.instance.TriggerStatic();
    }
}