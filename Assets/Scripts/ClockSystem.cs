using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ClockSystem : MonoBehaviour
{
    [Header("--- Réglages Temps ---")]
    public float nightDuration = 60f;
    public int startHour = 12;

    [Header("--- Interface HUD ---")]
    public Text hudClockText;
    
    [Header("--- Nettoyage (Objets à bloquer) ---")]
    public GameObject hudGroup;       // The complete HUD
    public GameObject tabletObject;   // The visual tablet object (Canvas or Panel)

    [Header("--- Séquence de Victoire ---")]
    public GameObject victoryPanel;   // The black background
    public Image victoryPanelImage;   // The black background image (for the fade)
    public Text centerClockText;      // Text "5 AM / 6 AM"
    public Text thanksText;           // Text "Thanks"
    public AudioSource victorySound;

    // Internal variables
    private float timer;
    private bool gameEnded = false;

    void Start()
    {
        // Initialization: We hide the victory screen and the texts
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (centerClockText != null) SetTextAlpha(centerClockText, 0); // Invisible
        if (thanksText != null) SetTextAlpha(thanksText, 0); // Invisible

        Time.timeScale = 1;
        UpdateClockDisplay(startHour);
    }

    void Update()
    {
        // If the game is over, we FORCE the tablet and HUD to close
        // This is the drastic solution to prevent the player from reopening it
        if (gameEnded)
        {
            if (tabletObject != null) tabletObject.SetActive(false);
            if (hudGroup != null) hudGroup.SetActive(false);
            return;
        }

        timer += Time.deltaTime;
        float progress = timer / nightDuration;
        int hourToDisplay = Mathf.FloorToInt(startHour + (progress * 6));

        if (hourToDisplay == 12) UpdateClockDisplay(12);
        else if (hourToDisplay > 12) UpdateClockDisplay(hourToDisplay - 12);

        if (timer >= nightDuration)
        {
            StartCoroutine(PlayWinSequence());
        }
    }

    void UpdateClockDisplay(int hour)
    {
        if (hudClockText != null) hudClockText.text = hour + " AM";
    }

    IEnumerator PlayWinSequence()
    {
        gameEnded = true; // This activates the tablet lock in Update
        Time.timeScale = 0; // We freeze the monsters

        // 1. FADE TO BLACK (BACKGROUND)
        if (victoryPanel != null && victoryPanelImage != null)
        {
            victoryPanel.SetActive(true);
            // We make sure the texts are active but invisible (Alpha 0)
            if (centerClockText) { centerClockText.gameObject.SetActive(true); SetTextAlpha(centerClockText, 0); }
            if (thanksText) { thanksText.gameObject.SetActive(true); SetTextAlpha(thanksText, 0); }

            // Fade of the black panel
            yield return StartCoroutine(FadeImage(victoryPanelImage, 0f, 1f, 1.5f));
        }

        // 2. FADE IN "5 AM"
        if (centerClockText != null)
        {
            centerClockText.text = "5 AM";
            centerClockText.color = new Color(1, 1, 1, 0); // Transparent white
            yield return StartCoroutine(FadeText(centerClockText, 0f, 1f, 1f));
            
            yield return new WaitForSecondsRealtime(1.5f); // Suspense

            // 3. HARD CUT TO 6 AM (More impactful at this moment)
            centerClockText.text = "6 AM";
            centerClockText.color = Color.green; // Opaque Green
            if (victorySound != null) victorySound.Play();
            
            yield return new WaitForSecondsRealtime(2f); // We let the player see the 6 AM
        }

        // 4. FADE IN "THANKS" (Below the 6 AM)
        if (thanksText != null)
        {
            // The 6 AM stays displayed, the thanks text fades in slowly
            yield return StartCoroutine(FadeText(thanksText, 0f, 1f, 1.5f));
            yield return new WaitForSecondsRealtime(3f); // Reading time
        }

        // 5. TOTAL FADE OUT (Both texts disappear together)
        if (centerClockText != null && thanksText != null)
        {
            // We start both coroutines at the same time without "yield return" so they play in parallel
            StartCoroutine(FadeText(centerClockText, 1f, 0f, 1.5f));
            yield return StartCoroutine(FadeText(thanksText, 1f, 0f, 1.5f)); // We only wait for the last one to finish
            
            yield return new WaitForSecondsRealtime(0.5f); // Short black screen pause
        }

        // 6. RETURN TO MENU
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    // --- SMALL UTILITIES FOR FADES ---

    // Instantly changes the alpha
    void SetTextAlpha(Text txt, float alpha)
    {
        Color c = txt.color;
        c.a = alpha;
        txt.color = c;
    }

    // Animates the alpha of a Text
    IEnumerator FadeText(Text txt, float startAlpha, float endAlpha, float duration)
    {
        float t = 0f;
        Color c = txt.color;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            txt.color = c;
            yield return null;
        }
    }

    // Animates the alpha of an Image (Background Panel)
    IEnumerator FadeImage(Image img, float startAlpha, float endAlpha, float duration)
    {
        float t = 0f;
        Color c = img.color;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            img.color = c;
            yield return null;
        }
    }
}