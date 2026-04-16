using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerActionManager : MonoBehaviour
{
    [Header("--- Réglages des Vues ---")]
    public int centerCamIndex = 0;
    public int rightCamIndex = 1;
    public int leftCamIndex = 2;

    [Header("--- Réglages Batterie ---")]
    public float maxPower = 100f;
    public float passiveDrain = 0.1f;
    public float tabletDrain = 0.3f;
    public float doorDrain = 1.0f;
    public float lightDrain = 0.8f;
    public float musicBoxRechargeDrain = 0.5f;

    [Header("--- Réglages Boîte à Musique ---")]
    public int musicBoxCameraIndex = 5;
    public float musicBoxDepletionSpeed = 0.1f;
    public float musicBoxRechargeSpeed = 0.5f;

    [Header("--- Références UI ---")]
    [SerializeField] private GameObject tabletUI;
    [SerializeField] private Image mapDisplayImage;
    [SerializeField] private Sprite[] mapSprites;
    [SerializeField] private Slider musicBoxSlider;
    [SerializeField] private Text powerText;

    [Header("--- Références Système ---")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Camera[] securityCameras;
    [SerializeField] private Light[] securityCameraLights;

    [Header("--- Références Environnement ---")]
    [SerializeField] private Animator leftDoorAnimator;
    [SerializeField] private Animator leftLeverAnimator;
    [SerializeField] private Animator rightDoorAnimator;
    [SerializeField] private Animator rightLeverAnimator;
    [SerializeField] private Light leftLightObject;
    [SerializeField] private Light rightLightObject;

    private const string ACTION_SWITCH = "Switch";
    private const string ACTION_VERTICAL = "Vertical";
    private const string ACTION_INTERACT = "Interact";
    private const string ANIM_PARAM_CLOSED = "IsClosed";

    private InputAction switchAction;
    private InputAction verticalAction;
    private InputAction interactAction;

    private bool isTabletOpen = false;
    private int currentSecurityCameraIndex = 0;
    private bool canUseVerticalInput = true;

    private bool isRechargingMusicBoxFrame = false;
    private float currentPower;
    private bool isPowerOut = false;

    private float musicBoxLevel = 1.0f;
    private bool isMusicBoxEmpty = false;

    // Memory for the "Push Button" effect of the cameras
    private bool wasRightPressed = false;
    private bool wasLeftPressed = false;

    void Start()
    {
        var inputMap = InputSystem.actions;
        switchAction = inputMap.FindAction(ACTION_SWITCH);
        verticalAction = inputMap.FindAction(ACTION_VERTICAL);
        interactAction = inputMap.FindAction(ACTION_INTERACT);

        switchAction.performed += OnTabletSwitch;

        if (tabletUI) tabletUI.SetActive(false);
        if (leftLightObject) leftLightObject.enabled = false;
        if (rightLightObject) rightLightObject.enabled = false;

        if (musicBoxSlider)
        {
            musicBoxSlider.gameObject.SetActive(false);
            musicBoxSlider.minValue = 0f;
            musicBoxSlider.maxValue = 1f;
            musicBoxSlider.value = 1f;
        }

        currentPower = maxPower;
        isPowerOut = false;
        UpdateSecurityCameras();
    }

    void Update()
    {
        isRechargingMusicBoxFrame = false;

        if (isPowerOut)
        {
            HandlePowerOutage();
            return;
        }

        UpdateMusicBoxLogic();
        HandleGameplay();
        HandlePowerManagement(); 
    }

    private void HandlePowerManagement()
    {
        float totalDrain = passiveDrain;

        if (isTabletOpen) totalDrain += tabletDrain;

        if (leftDoorAnimator != null && leftDoorAnimator.GetBool(ANIM_PARAM_CLOSED)) totalDrain += doorDrain;
        if (rightDoorAnimator != null && rightDoorAnimator.GetBool(ANIM_PARAM_CLOSED)) totalDrain += doorDrain;

        bool isAnyLightOn = (leftLightObject != null && leftLightObject.enabled) ||
                            (rightLightObject != null && rightLightObject.enabled);

        if (!isAnyLightOn && securityCameraLights != null)
        {
            foreach (var l in securityCameraLights)
            {
                if (l != null && l.enabled) { isAnyLightOn = true; break; }
            }
        }

        if (isAnyLightOn) totalDrain += lightDrain;
        if (isRechargingMusicBoxFrame) totalDrain += musicBoxRechargeDrain;

        currentPower -= totalDrain * Time.deltaTime;

        if (powerText != null) powerText.text = Mathf.CeilToInt(currentPower) + "%";

        if (currentPower <= 0)
        {
            currentPower = 0;
            TriggerPowerOutage();
        }
    }

    private void TriggerPowerOutage()
    {
        if (isPowerOut) return;
        isPowerOut = true;

        if (isTabletOpen) ToggleTablet(false);
        if (leftDoorAnimator) leftDoorAnimator.SetBool(ANIM_PARAM_CLOSED, false);
        if (rightDoorAnimator) rightDoorAnimator.SetBool(ANIM_PARAM_CLOSED, false);

        TurnOffAllLights();
        if (tabletUI) tabletUI.SetActive(false);
    }

    private void HandlePowerOutage()
    {
        if (leftLightObject) leftLightObject.enabled = false;
        if (rightLightObject) rightLightObject.enabled = false;
    }

    private void HandleGameplay()
    {
        float verticalValue = verticalAction.ReadValue<float>();
        bool interactPressed = interactAction.IsPressed();

        // CLEAN READING FROM CHATAIGNE
        if (ChataigneReceiver.instance != null)
        {
            if (ChataigneReceiver.instance.inputHaut) verticalValue = 1f;
            if (ChataigneReceiver.instance.inputBas) verticalValue = -1f;
            if (ChataigneReceiver.instance.inputBouton) interactPressed = true;

            // Clean "Push Button" effect for camera switching on the tablet
            bool isRightPressed = ChataigneReceiver.instance.inputDroite;
            bool isLeftPressed = ChataigneReceiver.instance.inputGauche;

            if (isTabletOpen)
            {
                if (isRightPressed && !wasRightPressed) ChangeCameraIndex(1f);
                if (isLeftPressed && !wasLeftPressed) ChangeCameraIndex(-1f);
            }

            wasRightPressed = isRightPressed;
            wasLeftPressed = isLeftPressed;
        }

        if (Mathf.Abs(verticalValue) < 0.1f) canUseVerticalInput = true;

        if (isTabletOpen)
        {
            if (verticalValue < -0.5f && canUseVerticalInput)
            {
                ToggleTablet(false);
                canUseVerticalInput = false;
            }

            HandleTabletInteraction(interactPressed, verticalValue);

            if (leftLightObject) leftLightObject.enabled = false;
            if (rightLightObject) rightLightObject.enabled = false;
            return;
        }

        int currentView = cameraController.GetCurrentCameraIndex();

        if (currentView == centerCamIndex)
        {
            if (verticalValue < -0.5f && canUseVerticalInput) 
            {
                ToggleTablet(true);
                canUseVerticalInput = false;
            }
        }
        else if (currentView == leftCamIndex)
        {
            if (Mathf.Abs(verticalValue) > 0.5f && canUseVerticalInput)
            {
                ManageDoorOrLever(leftDoorAnimator, verticalValue);
                ManageDoorOrLever(leftLeverAnimator, verticalValue);
            }
            ManageLight(leftLightObject, interactPressed, leftDoorAnimator);
        }
        else if (currentView == rightCamIndex)
        {
            if (Mathf.Abs(verticalValue) > 0.5f && canUseVerticalInput)
            {
                ManageDoorOrLever(rightDoorAnimator, verticalValue);
                ManageDoorOrLever(rightLeverAnimator, verticalValue);
            }
            ManageLight(rightLightObject, interactPressed, rightDoorAnimator);
        }
    }

    private void HandleTabletInteraction(bool isInteractPressed, float verticalInput)
    {
        if (currentSecurityCameraIndex == musicBoxCameraIndex)
        {
            if (verticalInput > 0.1f)
            {
                RechargeMusicBox();
                isRechargingMusicBoxFrame = true;
            }
        }

        if (securityCameraLights != null && currentSecurityCameraIndex < securityCameraLights.Length)
        {
            Light currentLight = securityCameraLights[currentSecurityCameraIndex];
            if (currentLight != null) currentLight.enabled = isInteractPressed;
        }
    }

    private void UpdateMusicBoxLogic()
    {
        if (isMusicBoxEmpty) return;
        if (musicBoxLevel > 0) musicBoxLevel -= musicBoxDepletionSpeed * Time.deltaTime;

        if (musicBoxLevel <= 0)
        {
            musicBoxLevel = 0;
            isMusicBoxEmpty = true;
        }
        if (musicBoxSlider != null) musicBoxSlider.value = musicBoxLevel;
    }

    private void RechargeMusicBox()
    {
        if (isMusicBoxEmpty) return;
        musicBoxLevel += musicBoxRechargeSpeed * Time.deltaTime;
        if (musicBoxLevel > 1.0f) musicBoxLevel = 1.0f;
    }

    private void ManageDoorOrLever(Animator anim, float inputVal)
    {
        if (anim == null) return;
        if (inputVal < -0.5f) anim.SetBool(ANIM_PARAM_CLOSED, true);
        else if (inputVal > 0.5f) anim.SetBool(ANIM_PARAM_CLOSED, false);
    }

    private void ManageLight(Light lightObj, bool isPressed, Animator doorAnim)
    {
        if (lightObj == null) return;
        bool isDoorClosed = doorAnim != null && doorAnim.GetBool(ANIM_PARAM_CLOSED);
        lightObj.enabled = isPressed && !isDoorClosed;
    }

    private void ToggleTablet(bool open)
    {
        if (isTabletOpen == open) return;
        isTabletOpen = open;
        cameraController.SetHeadMovementActive(!open);

        if (tabletUI) tabletUI.SetActive(open);
        if (open)
        {
            UpdateSecurityCameras();
            if (CameraStaticEffect.instance != null) CameraStaticEffect.instance.TriggerStatic();
        }

        TurnOffAllLights();
    }

    private void OnTabletSwitch(InputAction.CallbackContext ctx)
    {
        float dir = ctx.ReadValue<float>();
        ChangeCameraIndex(dir);
    }

    private void ChangeCameraIndex(float dir)
    {
        if (!isTabletOpen || isPowerOut || dir == 0) return;

        TurnOffAllLights(); 

        if (dir > 0)
        {
            currentSecurityCameraIndex++;
            if (currentSecurityCameraIndex >= securityCameras.Length) currentSecurityCameraIndex = 0;
        }
        else
        {
            currentSecurityCameraIndex--;
            if (currentSecurityCameraIndex < 0) currentSecurityCameraIndex = securityCameras.Length - 1;
        }

        UpdateSecurityCameras();
        if (CameraStaticEffect.instance != null) CameraStaticEffect.instance.TriggerStatic();
    }

    private void UpdateSecurityCameras()
    {
        if (securityCameras != null)
        {
            for (int i = 0; i < securityCameras.Length; i++)
            {
                if (securityCameras[i] != null)
                    securityCameras[i].gameObject.SetActive(i == currentSecurityCameraIndex);
            }
        }

        if (musicBoxSlider != null)
            musicBoxSlider.gameObject.SetActive(currentSecurityCameraIndex == musicBoxCameraIndex);

        if (mapDisplayImage != null && mapSprites != null)
        {
            if (currentSecurityCameraIndex < mapSprites.Length)
                mapDisplayImage.sprite = mapSprites[currentSecurityCameraIndex];
        }
    }

    private void TurnOffAllLights()
    {
        if (securityCameraLights != null)
        {
            foreach (var l in securityCameraLights) if (l != null) l.enabled = false;
        }
        if (leftLightObject) leftLightObject.enabled = false;
        if (rightLightObject) rightLightObject.enabled = false;
    }

    public bool IsDoorClosed(bool isLeft)
    {
        Animator targetDoor = isLeft ? leftDoorAnimator : rightDoorAnimator;
        if (targetDoor == null) return false;
        return targetDoor.GetBool(ANIM_PARAM_CLOSED);
    }

    public void ForceCloseTablet()
    {
        if (isTabletOpen) ToggleTablet(false);
    }

    public bool GetIsMusicBoxEmpty()
    {
        return isMusicBoxEmpty;
    }
}