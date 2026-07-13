using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    [Header("References")]
    public Image countdownImage;
    public Image parentImage;

    [Header("Countdown Sprites")]
    public Sprite sprite3;
    public Sprite sprite2;
    public Sprite sprite1;
    public Sprite spriteGoSun;
    public Sprite spriteGoMoon;

    [Header("Animation Settings")]
    public float displayDuration = 0.6f;
    public float scaleAnimationDuration = 0.5f;
    public float maxScale = 1.2f;
    public float minScale = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Timing")]
    public float delayBeforeStart = 0.5f;
    public float timeBetweenNumbers = 1f;

    private Coroutine countdownCoroutine;
    internal bool countdownStarted = false;

    void Start()
    {
        if (!countdownStarted)
        {
            countdownStarted = true;
            StartCountdown();
        }
    }

    private void OnEnable()
    {
        if (countdownImage == null)
        {
            countdownImage = GetComponent<Image>();
        }

        if (parentImage == null)
        {
            parentImage = GetComponentInParent<Image>();
        }

        // Start invisible
        if (parentImage != null)
        {
            Color color = parentImage.color;
            color.a = 0f;
            parentImage.color = color;
        }
    }

    /// <summary>
    /// Starts the countdown timer animation
    /// </summary>
    public void StartCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownSequence());
    }

    /// <summary>
    /// Stops the countdown animation if it's running
    /// </summary>
    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        if (parentImage != null)
        {
            Color color = parentImage.color;
            color.a = 0f;
            parentImage.color = color;
        }
    }

    private IEnumerator CountdownSequence()
    {
        // Wait before starting
        yield return new WaitForSeconds(delayBeforeStart);

        // Show 3
        countdownImage.gameObject.SetActive(true);
        yield return StartCoroutine(ShowNumber(sprite3));
        yield return new WaitForSeconds(timeBetweenNumbers);

        // Show 2
        yield return StartCoroutine(ShowNumber(sprite2));
        yield return new WaitForSeconds(timeBetweenNumbers);

        // Show 1
        yield return StartCoroutine(ShowNumber(sprite1));
        yield return new WaitForSeconds(timeBetweenNumbers);

        // Show "Go" with player-specific sprite
        Sprite goSprite = GetGoSprite();
        yield return StartCoroutine(ShowNumber(goSprite));

        yield return new WaitForSeconds(timeBetweenNumbers);
        countdownImage.gameObject.SetActive(false);

        // Hide after "Go"
        if (parentImage != null)
        {
            Color color = parentImage.color;
            color.a = 0f;
            parentImage.color = color;
        }

        countdownCoroutine = null;
        countdownStarted = false; // Reset the countdown state after completion
    }

    private IEnumerator ShowNumber(Sprite sprite)
    {
        if (countdownImage == null)
            yield break;

        // Set the sprite
        countdownImage.sprite = sprite;

        // Fade in
        if (parentImage != null)
        {
            Color color = parentImage.color;
            color.a = 1f;
            parentImage.color = color;
        }

        // Scale animation: start at max, scale down to min
        float elapsed = 0f;
        float startScale = maxScale;
        float endScale = minScale;

        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleAnimationDuration);
            float curveValue = scaleCurve.Evaluate(t);
            float currentScale = Mathf.Lerp(startScale, endScale, curveValue);

            countdownImage.transform.localScale = new Vector3(currentScale, currentScale, 1f);
            yield return null;
        }

        // Ensure final scale is set
        countdownImage.transform.localScale = new Vector3(endScale, endScale, 1f);

        // Display for the remaining time
        float displayRemaining = displayDuration - scaleAnimationDuration;
        if (displayRemaining > 0f)
        {
            yield return new WaitForSeconds(displayRemaining);
        }
    }

    private Sprite GetGoSprite()
    {
        // Check if player is Sun
        bool isPlayerSun = true;

        if (SocketHandler.instance != null)
        {
            // Access the private field through reflection or check public method
            // For now, we'll try to access through the SocketHandler
            isPlayerSun = IsPlayerSun();
        }

        return isPlayerSun ? spriteGoSun : spriteGoMoon;
    }

    /// <summary>
    /// Determines if the current player is the Sun player
    /// </summary>
    private bool IsPlayerSun()
    {
        if (SocketHandler.instance == null)
            return true;

        // In single player mode, player is Sun
        if (SocketHandler.instance.isSinglePlayerMode)
            return true;

        // Check LobbyManager for player status
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            return lobbyManager.isPlayerSun;
        }

        return true; // Default to Sun
    }
}
