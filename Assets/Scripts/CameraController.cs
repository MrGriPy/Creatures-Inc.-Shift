using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("--- Tes Caméras Standards ---")]
    [SerializeField] private List<GameObject> m_Cameras;

    private InputAction m_SwitchAction;
    private const string SWITCH_ACTION_NAME = "Switch";

    // We keep track of the active camera index (e.g. 0=Left, 1=Center, 2=Right)
    private int m_CurrentCameraIndex = 0; // We start at 0 (the center, according to your order)

    private void Awake()
    {
        // Safety: we make sure there are cameras in the list
        if (m_Cameras == null || m_Cameras.Count == 0) return;

        // Initialization: The starting camera is active, the others are not
        UpdateCameraStates();
    }

    private void Start()
    {
        m_SwitchAction = InputSystem.actions.FindAction(SWITCH_ACTION_NAME);
        if (m_SwitchAction != null)
        {
            m_SwitchAction.performed += OnSwitchActionPerformed;
        }
    }

    private void OnSwitchActionPerformed(InputAction.CallbackContext obj)
    {
        // SAFETY: We block the joystick if we are on the Game Over screen
        if (Time.timeScale == 0) return; 

        // 1. We read the value (-1 for left, +1 for right)
        float direction = obj.ReadValue<float>();

        // 2. If the value is 0 (when we release the key), we do nothing
        if (direction == 0) return;

        // 3. We change the index
        if (direction > 0) 
        {
            // To the right (Next)
            m_CurrentCameraIndex++;
            // If we go past the end of the list, we wrap back to the beginning (loop)
            if (m_CurrentCameraIndex >= m_Cameras.Count) 
                m_CurrentCameraIndex = 0;
        }
        else 
        {
            // To the left (Previous)
            m_CurrentCameraIndex--;
            // If we go below 0, we go to the end of the list
            if (m_CurrentCameraIndex < 0) 
                m_CurrentCameraIndex = m_Cameras.Count - 1;
        }

        // 4. We turn cameras on/off
        UpdateCameraStates();
    }

    private void UpdateCameraStates()
    {
        // We iterate through all cameras
        for (int i = 0; i < m_Cameras.Count; i++)
        {
            if (m_Cameras[i] != null)
            {
                // If the index matches, we activate the object, otherwise we deactivate it
                m_Cameras[i].SetActive(i == m_CurrentCameraIndex);
            }
        }
    }

    public int GetCurrentCameraIndex()
    {
        return m_CurrentCameraIndex;
    }

    public void ForcerCamera(int index)
    {
        m_CurrentCameraIndex = index;
        UpdateCameraStates();
    }

    public void SetHeadMovementActive(bool isActive)
    {
        if (m_SwitchAction == null) return;
        
        // We remove the listener to avoid duplicates, then add it back if active
        m_SwitchAction.performed -= OnSwitchActionPerformed;
        if (isActive) 
        {
            m_SwitchAction.performed += OnSwitchActionPerformed;
        }
    }

    private void OnDestroy()
    {
        // Safety: We clean up the action when the object is destroyed to avoid memory bugs
        if (m_SwitchAction != null)
        {
            m_SwitchAction.performed -= OnSwitchActionPerformed;
        }
    }
}