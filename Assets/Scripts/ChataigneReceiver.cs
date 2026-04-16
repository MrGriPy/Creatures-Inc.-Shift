using UnityEngine;

public class ChataigneReceiver : MonoBehaviour // <-- FIX HERE: The name matches the file
{
    public static ChataigneReceiver instance; // <-- FIX HERE

    [Header("--- Commandes reçues depuis Chataigne ---")]
    [Tooltip("Dans Chataigne, passe ces valeurs à True/False")]
    public bool inputHaut = false;
    public bool inputBas = false;
    public bool inputGauche = false;
    public bool inputDroite = false;
    public bool inputBouton = false;

    void Awake()
    {
        // We make sure there is only one in the scene
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // Utility functions to convert booleans to axes (-1 to 1) for Unity
    public float GetVerticalAxis()
    {
        if (inputHaut) return 1f;
        if (inputBas) return -1f;
        return 0f;
    }

    public float GetHorizontalAxis()
    {
        if (inputDroite) return 1f;
        if (inputGauche) return -1f;
        return 0f;
    }
}