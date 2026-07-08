using UnityEngine;
using UnityEngine.UI;

public class ToggleHandler : MonoBehaviour
{
    public Sprite onSprite;
    public Sprite offSprite;
    public Image image;
    public Button button;
    public PlayerController sunController;
    public PlayerController moonController;
    public PlayerController ecclipseController;

    [Header("Sounds")]
    public AudioClip click1;
    public bool state
    {
        get => PlayerPrefs.GetInt("JoystickEnabled", 1) == 1;
        set => PlayerPrefs.SetInt("JoystickEnabled", value ? 1 : 0);
    }

    void Awake()
    {
        if (state) image.sprite = onSprite;
        else image.sprite = offSprite;

        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        state = !state;
        sunController.useGyro = !state;
        moonController.useGyro = !state;
        ecclipseController.useGyro = !state;
        AudioManager.Instance.PlaySFX(click1);
        if (state)
        {
            image.sprite = onSprite;
        }
        else image.sprite = offSprite;
    }
}