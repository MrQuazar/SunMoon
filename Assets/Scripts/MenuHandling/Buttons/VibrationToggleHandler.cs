using UnityEngine;
using UnityEngine.UI;

public class JoystickToggleHandler : MonoBehaviour
{
    public Sprite onSprite;
    public Sprite offSprite;
    public Image image;
    public Button button;

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

        if (state)
        {
            image.sprite = onSprite;
        }
        else image.sprite = offSprite;
    }
}