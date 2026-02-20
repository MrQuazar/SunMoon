using UnityEngine;
using UnityEngine.UI;

public class JoystickToggleHandler : MonoBehaviour
{
    public Sprite onSprite;
    public Sprite offSprite;
    public Image image;
    public Button button;

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

        if (state) 
        {
            image.sprite = onSprite;
        }
        else image.sprite = offSprite;
    }
}