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
    public bool state = true;

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
        if (state)
        {
            image.sprite = onSprite;
        }
        else image.sprite = offSprite;
    }
}