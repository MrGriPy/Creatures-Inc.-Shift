using System.Collections;
using UnityEngine;

public class MonstreLumiere : MonoBehaviour
{
    [System.Serializable]
    public struct PointApparition
    {
        public int cameraIndex; 
        public Transform positionVisuelle; 
        public Light lumiereCamera; 
    }

    [Header("--- Identité ---")]
    public int monstreIndex = 2; // The corrected variable!

    [Header("--- Évolution de la Difficulté ---")]
    [Tooltip("Durée totale de ta nuit en secondes (ex: 360s pour 6 minutes)")]
    public float dureeTotaleNuit = 360f;

    [Space(5)]
    public int aiLevelDebut = 2;   
    public int aiLevelFin = 15;    

    [Space(5)]
    public float intervalleDebut = 12f; 
    public float intervalleFin = 5f;    

    [Space(5)]
    public float tempsPourEclairerDebut = 6f; 
    public float tempsPourEclairerFin = 1.5f; 

    [HideInInspector] public int aiLevel;
    [HideInInspector] public float intervalleTentative;
    [HideInInspector] public float tempsPourEclairer;

    [Header("--- Visuel Déplacement (Caméras) ---")]
    [Tooltip("Le modèle 3D qui s'affiche sur les caméras")]
    public GameObject animatronicModel; 
    public PointApparition[] pointsApparition;

    [Header("--- Mécanique Porte Droite ---")]
    public float tempsAttentePorte = 5f; 

    [Header("--- Audio ---")]
    public AudioSource sonRire;
    public AudioSource sonJumpscare;

    [Header("--- Jumpscare (Attaque par la droite) ---")]
    public GameObject jumpscareModel;
    public float dureeJumpscare = 1.5f;
    public float jumpDuration = 0.2f;
    public Vector3 positionDepartJumpscare = new Vector3(1.5f, -0.2f, 1.0f); 
    public Vector3 positionFinJumpscare = new Vector3(0f, -0.2f, 0.5f);      

    [Header("--- Références ---")]
    [SerializeField] private PlayerActionManager playerManager;

    private enum EtatMonstre { Cache, SurCamera, ALaPorte, EnAttaque }
    private EtatMonstre etatActuel = EtatMonstre.Cache;

    private float timerMouvement;
    private float timerAction;
    private int indexPointActuel = -1;
    
    // Our internal timer to track where we are in the night
    private float tempsPasseDansLaNuit = 0f; 

    void Start()
    {
        if (animatronicModel != null) animatronicModel.SetActive(false);
        if (jumpscareModel != null) jumpscareModel.SetActive(false);
        
        // We initialize with the starting values
        aiLevel = aiLevelDebut;
        intervalleTentative = intervalleDebut;
        tempsPourEclairer = tempsPourEclairerDebut;
        
        timerMouvement = intervalleTentative;
        
        Debug.Log("<b>[Monstre 3] Initialisé et caché. Début du cycle de difficulté.</b>");
    }

    void Update()
    {
        if (playerManager == null || etatActuel == EtatMonstre.EnAttaque) return;

        // --- INCREASING DIFFICULTY MANAGEMENT ---
        // We add the elapsed time since the last frame
        tempsPasseDansLaNuit += Time.deltaTime;
        
        // We calculate a progression percentage between 0 (start) and 1 (end of night)
        float progression = Mathf.Clamp01(tempsPasseDansLaNuit / dureeTotaleNuit);

        // We update our variables in real time
        aiLevel = (int)Mathf.Lerp(aiLevelDebut, aiLevelFin, progression);
        intervalleTentative = Mathf.Lerp(intervalleDebut, intervalleFin, progression);
        tempsPourEclairer = Mathf.Lerp(tempsPourEclairerDebut, tempsPourEclairerFin, progression);
        // ------------------------------------------

        switch (etatActuel)
        {
            case EtatMonstre.Cache:
                GererTentativeApparition();
                break;

            case EtatMonstre.SurCamera:
                GererPresenceCamera();
                break;

            case EtatMonstre.ALaPorte:
                GererPresencePorte();
                break;
        }
    }

    private void GererTentativeApparition()
    {
        timerMouvement -= Time.deltaTime;
        if (timerMouvement <= 0)
        {
            timerMouvement = intervalleTentative;
            
            int tirage = Random.Range(1, 21);
            Debug.Log($"<i>[Monstre 3] Tentative... Tirage: {tirage} / Niveau IA actuel: {aiLevel} / Temps de réaction actuel: {tempsPourEclairer:F1}s</i>");

            if (tirage <= aiLevel && pointsApparition.Length > 0)
            {
                ApparaitreSurCamera();
            }
            else
            {
                Debug.Log("<i>[Monstre 3] Échec de la tentative. Il reste caché.</i>");
            }
        }
    }

    private void ApparaitreSurCamera()
    {
        etatActuel = EtatMonstre.SurCamera;
        
        // It uses the reaction time corresponding to its current aggressiveness
        timerAction = tempsPourEclairer;

        indexPointActuel = Random.Range(0, pointsApparition.Length);
        PointApparition point = pointsApparition[indexPointActuel];
        Transform pos = point.positionVisuelle;

        if (animatronicModel != null && pos != null)
        {
            animatronicModel.transform.position = pos.position;
            animatronicModel.transform.rotation = pos.rotation;
            animatronicModel.SetActive(true);
        }

        if (sonRire != null) sonRire.Play();
        
        Debug.Log($"<color=yellow><b>[Monstre 3] APPARAÎT !</b> Caméra ciblée : {point.cameraIndex} (Position : {pos.name}). Temps pour réagir : {tempsPourEclairer:F1}s.</color>");
    }

    private void GererPresenceCamera()
    {
        timerAction -= Time.deltaTime;
        PointApparition point = pointsApparition[indexPointActuel];

        if (point.lumiereCamera != null && point.lumiereCamera.enabled)
        {
            Debug.Log($"<color=green><b>[Monstre 3] REPOUSSÉ !</b> La lumière l'a fait fuir.</color>");
            RepartirSeCacher();
            DeclencherBrouillage();
            return;
        }

        if (timerAction <= 0)
        {
            Debug.Log($"<color=orange><b>[Monstre 3] TEMPS ÉCOULÉ !</b> Il fonce à la porte droite !</color>");
            AllerALaPorte();
        }
    }

    private void GererPresencePorte()
    {
        bool porteFermee = playerManager.IsDoorClosed(false); 

        if (!porteFermee)
        {
            Debug.Log("<color=red><b>[Monstre 3] ATTAQUE !</b> La porte droite était ouverte.</color>");
            StartCoroutine(SequenceJumpscare());
        }
        else
        {
            timerAction -= Time.deltaTime;
            if (timerAction <= 0)
            {
                Debug.Log("<color=green><b>[Monstre 3] ABANDON !</b> La porte droite était fermée.</color>");
                RepartirSeCacher();
            }
        }
    }

    private void AllerALaPorte()
    {
        etatActuel = EtatMonstre.ALaPorte;
        timerAction = tempsAttentePorte; 
        
        if (animatronicModel != null) animatronicModel.SetActive(false);
        DeclencherBrouillage();
    }

    private void RepartirSeCacher()
    {
        etatActuel = EtatMonstre.Cache;
        if (animatronicModel != null) animatronicModel.SetActive(false);
        
        // We restart the timer with its current speed
        timerMouvement = intervalleTentative; 
    }

    private IEnumerator SequenceJumpscare()
    {
        etatActuel = EtatMonstre.EnAttaque;
        
        if (playerManager != null) playerManager.ForceCloseTablet();

        if (jumpscareModel != null)
        {
            jumpscareModel.transform.localPosition = positionDepartJumpscare;
            jumpscareModel.SetActive(true);
        }

        if (sonJumpscare != null) sonJumpscare.Play();

        float elapsed = 0;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / jumpDuration;
            
            if (jumpscareModel != null)
            {
                jumpscareModel.transform.localPosition = Vector3.Lerp(positionDepartJumpscare, positionFinJumpscare, percent);
            }
            yield return null;
        }

        yield return new WaitForSeconds(dureeJumpscare - jumpDuration);

        if (jumpscareModel != null) jumpscareModel.SetActive(false);

        GameOverManager gameOverManager = Object.FindFirstObjectByType<GameOverManager>();
        if (gameOverManager != null) gameOverManager.DeclencherGameOver(monstreIndex);
    }

    private void DeclencherBrouillage()
    {
        if (CameraStaticEffect.instance != null) CameraStaticEffect.instance.TriggerStatic();
    }
}