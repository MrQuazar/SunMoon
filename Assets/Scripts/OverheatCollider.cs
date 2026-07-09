using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OverheatCollider : MonoBehaviour
{
    [Header("Timing")]
    public float interval = 30f;
    public float overheatDuration = 5f;
    public float warningTime = 2f;

    [Header("Player Role")]
    [SerializeField] private bool isPlayerSun = true;

    [Header("UI References")]
    public GameObject warningPanel;   // Parent object for all UI
    public Text warningText;
    public Button actionButton;
    public Image roleImage;

    [Header("Role Sprites")]
    public Sprite sprite;

    [Header("Shake Phone Icon")]
    public RectTransform shakePhoneIcon; // Drag the icon's RectTransform here (already in scene, hidden)
    public float tiltAngle = 20f;        // Max rotation angle in each direction
    public float tiltSpeed = 4f;         // How fast it tilts back and forth

    [Header("Audio")]
    public AudioClip dialogueAudio;
    public AudioClip laserAudio;

    public bool isOverheating = false;
    private bool hasShownDialogue = false;
    private Coroutine shakeIconRoutine;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private JoystickToggleHandler vibrationToggleHandler;

    void Start()
    {
        HideUI();
        StartCoroutine(OverheatRoutine());
    }

    IEnumerator OverheatRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval - warningTime);

            ShowWarning();

            yield return new WaitForSeconds(warningTime);

            StartOverheat();

            yield return new WaitForSeconds(overheatDuration);

            EndOverheat();
        }
    }

    void ShowWarning()
    {
        if (playerController.isPlayerController)
        {
            // Set correct sprite
            if (roleImage != null)
                roleImage.sprite = sprite;

            if (!hasShownDialogue)
            {
                if (warningPanel != null)
                    warningPanel.SetActive(true);

                if (warningText != null)
                {
                    warningText.text = isPlayerSun
                        ? "Uh-oh! Overheating"
                        : "Umm... Am I losing parts again?";
                }

                if (dialogueAudio != null)
                    AudioManager.Instance.PlaySFX(dialogueAudio);

                hasShownDialogue = true;
            }
            else
            {
                // Later overheats → no text, only laser audio
                if (laserAudio != null)
                    AudioManager.Instance.PlaySFX(laserAudio);
            }
        }
    }

    void StartOverheat()
    {
        isOverheating = true;

        if (playerController.isPlayerController)
        {
            if (actionButton != null)
                actionButton.gameObject.SetActive(true);

            StartCoroutine(OverheatCountdown());
            Vibrate();
            ShowShakeIcon();

            Debug.Log("OVERHEAT STARTED");
        }
    }

    //Reduce actionbutton fill
    private IEnumerator OverheatCountdown()
    {
        float elapsedTime = 0f;
        while (elapsedTime < overheatDuration)
        {
            elapsedTime += Time.deltaTime;
            float fillAmount = Mathf.Clamp01(1f - (elapsedTime / overheatDuration));
            actionButton.image.fillAmount = fillAmount;
            yield return null;
        }
    }

    void EndOverheat()
    {
        isOverheating = false;
        HideUI();
        AudioManager.Instance.StopSFX();
        HideShakeIcon();
        Debug.Log("OVERHEAT ENDED");
    }

    void HideUI()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);

        if (actionButton != null)
            actionButton.gameObject.SetActive(false);

        hasShownDialogue = false; // Reset for next overheat cycle 
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isOverheating) return;

        // Same reasoning as LightCollider: every client has both an overheat
        // collider for the Sun and one for the Moon in its scene, but only
        // the one attached to this client's own controlled player is
        // allowed to force a plant's state locally. The other player's
        // overheat conversions only ever arrive via the server.
        if (playerController == null || !playerController.isPlayerController) return;

        PlantHandler plant = other.GetComponentInParent<PlantHandler>();
        if (plant == null) return;

        plant.SetFinalState(isPlayerSun);
        plant.currentAmount = plant.maxAmount;
    }

    public void ForceStopOverheat()
    {
        StopAllCoroutines();
        isOverheating = false;
        HideUI();
        HideShakeIcon();
        Vibrate();
        StartCoroutine(OverheatRoutine());
    }

    // Called when a replay starts (or any time the overheat cycle needs a
    // clean restart). Unlike ForceStopOverheat, this also clears
    // hasShownDialogue so the intro warning line plays again next match,
    // and doesn't vibrate/count as a player-triggered stop.
    public void ResetForReplay()
    {
        StopAllCoroutines();
        isOverheating = false;
        hasShownDialogue = false;
        HideUI();
        HideShakeIcon();
        if(gameObject.activeInHierarchy) StartCoroutine(OverheatRoutine());
    }

    void Vibrate()
    {
        if (vibrationToggleHandler.state)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }

    // ---------- Shake Phone Icon ----------

    void ShowShakeIcon()
    {
        if (shakePhoneIcon == null) return;

        shakePhoneIcon.gameObject.SetActive(true);

        if (shakeIconRoutine != null)
            StopCoroutine(shakeIconRoutine);

        shakeIconRoutine = StartCoroutine(TiltShakeIconRoutine());
    }

    void HideShakeIcon()
    {
        if (shakePhoneIcon == null) return;

        if (shakeIconRoutine != null)
        {
            StopCoroutine(shakeIconRoutine);
            shakeIconRoutine = null;
        }

        shakePhoneIcon.localRotation = Quaternion.identity;
        shakePhoneIcon.gameObject.SetActive(false);
    }

    IEnumerator TiltShakeIconRoutine()
    {
        // Continuously oscillate rotation between -tiltAngle and +tiltAngle using a sine wave
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * tiltSpeed;
            float angle = Mathf.Sin(t) * tiltAngle;
            shakePhoneIcon.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
    }
}