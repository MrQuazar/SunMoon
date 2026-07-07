using UnityEngine;
using UnityEngine.UI;

public class TutorialMenu : Screens
{
    [Header("Slideshow")]
    [SerializeField] private Image slideImage;      // Displays the current tutorial slide. Set to Preserve Aspect in the Inspector.
    [SerializeField] private Sprite[] slides;        // Portrait 9:16 tutorial images, in the order they should be shown.

    [Header("Navigation")]
    [SerializeField] private Button next;
    [SerializeField] private Button previous;
    [SerializeField] private Button back;

    // Optional: dot/step indicator, e.g. "2 / 6". Leave unassigned if not used.
    [SerializeField] private Text pageLabel;

    private int currentIndex;

    internal override void AddListeners()
    {
        next.onClick.AddListener(OnNextPress);
        previous.onClick.AddListener(OnPreviousPress);
        back.onClick.AddListener(OnBackPress);
    }

    internal override void RemoveListeners()
    {
        next.onClick.RemoveListener(OnNextPress);
        previous.onClick.RemoveListener(OnPreviousPress);
        back.onClick.RemoveListener(OnBackPress);
    }

    internal override void Enable()
    {
        // Always start the slideshow from the first slide when the panel is opened.
        currentIndex = 0;
        ShowSlide(currentIndex);
        base.Enable();
    }

    private void ShowSlide(int index)
    {
        if (slideImage == null || slides == null || slides.Length == 0)
        {
            Debug.LogWarning("TutorialMenu: no slides assigned.");
            return;
        }

        slideImage.sprite = slides[index];

        if (pageLabel != null)
            pageLabel.text = $"{index + 1} / {slides.Length}";
    }

    private void OnNextPress()
    {
        if (slides == null || slides.Length == 0) return;

        // Wraps back to the first image after the last one, so it cycles continuously.
        currentIndex = (currentIndex + 1) % slides.Length;
        ShowSlide(currentIndex);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }

    private void OnPreviousPress()
    {
        if (slides == null || slides.Length == 0) return;

        currentIndex = (currentIndex - 1 + slides.Length) % slides.Length;
        ShowSlide(currentIndex);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }

    private void OnBackPress()
    {
        menuHandler.ChangeScreen(menuHandler.mainMenu);
        AudioManager.Instance.PlaySFX(menuHandler.click2);
    }
}
