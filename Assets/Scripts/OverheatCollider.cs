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

    [Header("Audio")]
    public AudioClip dialogueAudio;
    public AudioClip laserAudio;

    public bool isOverheating = false;
    private bool hasShownDialogue = false;
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

        if (actionButton != null)
            actionButton.gameObject.SetActive(true);

        StartCoroutine(OverheatCountdown());
        Vibrate();

        Debug.Log("OVERHEAT STARTED");
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
        Debug.Log("OVERHEAT ENDED");
    }

    void HideUI()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);

        if (actionButton != null)
            actionButton.gameObject.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isOverheating) return;

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
        Vibrate();
        StartCoroutine(OverheatRoutine());
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
}