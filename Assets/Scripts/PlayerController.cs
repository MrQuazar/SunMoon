using UnityEngine;
using PinePie.SimpleJoystick;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Planet")]
    public Transform planet;
    public float radius = 10f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float alignSpeed = 12f;

    [Header("Player Controlled")]
    public bool isPlayerController = false;
    public Vector3 recievedLocation = Vector3.zero;

    [Header("Android Controls")]
    public bool useGyro = false;

    [Tooltip("Drag the PinePie JoystickController here (Right joystick).")]
    public JoystickController joystick;

    [Header("Gyro Settings")]
    public float gyroSensitivity = 1.5f;
    public bool invertGyroX = false;
    public bool invertGyroY = false;

    [Header("Blocking")]
    public float playerCollisionRadius = 0.5f;
    public GameObject otherPlayer;

    [Header("Overheat")]
    [SerializeField] private OverheatCollider overheat;
    [SerializeField] internal Image eclipseObject;

    [Header("Wiggle Fix Settings")]
    public float wiggleThreshold = 0.2f;     // minimum horizontal input
    public int requiredWiggles = 3;          // left-right switches required

    private int lastWiggleDirection = 0;
    private int wiggleCount = 0;

    private bool gyroAvailable = false;

    void Start()
    {
        InitializePosition();
        SetupGyro();
    }

    void Update()
    {
        HandleMovement();
        AlignToSurface();
    }

    void InitializePosition()
    {
        Vector3 normal = GetNormal();
        transform.position = planet.position + normal * radius;
        overheat = GetComponent<OverheatCollider>();
    }

    Vector3 GetNormal()
    {
        return (transform.position - planet.position).normalized;
    }

    void SetupGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroAvailable = true;
            Debug.Log("Gyroscope is available and enabled.");
        }
        else
        {
            gyroAvailable = false;
            useGyro = false;
            Debug.Log("Gyroscope is not available.");

        }
    }

    internal Vector3 moveDirGlobal;
    void CheckWiggle(float horizontalInput)
    {
        if (Mathf.Abs(horizontalInput) < wiggleThreshold)
            return;

        int direction = horizontalInput > 0 ? 1 : -1;

        // Debug.LogError("Direction: " + direction + ", Last Direction: " + lastWiggleDirection + ", horizontalInput: " + horizontalInput);

        if (lastWiggleDirection != 0 && direction != lastWiggleDirection)
        {
            // Debug.LogError(wiggleCount + " / " + requiredWiggles);
            wiggleCount++;

            if (wiggleCount >= requiredWiggles)
            {
                ForceStopOverheat();
                wiggleCount = 0;
                lastWiggleDirection = 0;
                return;
            }
        }

        lastWiggleDirection = direction;
    }

    void HandleMovement()
    {
        if (!isPlayerController && SocketHandler.instance.hasGameStarted)
        {
            // TEMP: in single-player test mode there's no peer sending real
            // positions, so recievedLocation just sits at Vector3.zero. Skip
            // the snap-to-received-location logic entirely and leave this
            // player (Moon) resting at its spawn point instead.
            if (SocketHandler.instance.isSinglePlayerMode)
            {
                return;
            }

            recievedLocation = SocketHandler.instance.recievedLocation;
            Debug.Log("Received location: " + recievedLocation);
            if (!WouldCollideWithOtherPlayer(recievedLocation))
            {
                transform.position = recievedLocation;
            }

            // if (!SocketHandler.instance.isEclipseActive)
            return;
        }
        float h = 0f; // LEFT/RIGHT
        float v = 0f; // UP/DOWN

        // ================================
        // ANDROID CONTROLS
        // ================================
        // Joystick controls for Android


        // Gyro controls for Android
        if ((overheat && overheat.isOverheating) || (useGyro && gyroAvailable))
        {
            Vector3 tilt = Input.gyro.gravity;

            // Apply gyro sensitivity adjustment to reduce sensitivity
            float gyroX = tilt.x * gyroSensitivity;
            float gyroY = tilt.y * gyroSensitivity;

            // Invert gyro based on settings
            if (invertGyroX) gyroX *= -1f;
            if (invertGyroY) gyroY *= -1f;

            // Apply a threshold for more significant tilts
            float tiltThreshold = 0.1f;  // Minimum tilt value to trigger movement (adjust if necessary)

            if (Mathf.Abs(gyroY) > tiltThreshold)
            {
                h = gyroY;   // left/right (x-axis)
            }

            if (Mathf.Abs(gyroX) > tiltThreshold)
            {
                v = gyroX;  // forward/back (y-axis)
            }
            CheckWiggle(v);
        }
        else if (!useGyro && joystick != null)
        {
            Vector2 input = joystick.InputDirection;

            h = -input.y;   // left/right
            v = input.x;  // flipped forward/back
            // CheckWiggle(h);
        }
        // Calculate movement direction and apply it to player
        Vector3 normal = GetNormal();
        Vector3 forwardDir = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        Vector3 rightDir = Vector3.ProjectOnPlane(transform.right, normal).normalized;

        Vector3 moveDir = (forwardDir * v + rightDir * h);
        if (moveDir.sqrMagnitude < 0.001f)
            return;

        if (overheat && overheat.isOverheating)
        {
            return; // Prevent movement during overheating
        }

        if (RoundManager.Instance.countdownTimer.countdownStarted)
        {
            return; // Prevent movement during countdown
        }

        moveDir.Normalize();
        moveDirGlobal = moveDir;
        // if (!isPlayerController/*  || (SocketHandler.instance.isEclipseActive && !SocketHandler.instance.isSameDirection) */)
        //     return;
        Vector3 newPos = transform.position + moveDir * moveSpeed * Time.deltaTime;

        // Clamp to planet surface
        newPos = planet.position + (newPos - planet.position).normalized * radius;

        if (!WouldCollideWithOtherPlayer(newPos))
        {
            transform.position = newPos;
        }
    }

    bool WouldCollideWithOtherPlayer(Vector3 targetPos)
    {
        if (otherPlayer == null)
            return false;

        PlayerController other = otherPlayer.GetComponent<PlayerController>();
        if (other == null)
            return false;

        float combinedRadius = playerCollisionRadius + other.playerCollisionRadius;
        float dist = Vector3.Distance(targetPos, otherPlayer.transform.position);

        return dist < combinedRadius;
    }

    void AlignToSurface()
    {
        Vector3 normal = GetNormal();
        Quaternion targetRot = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignSpeed * Time.deltaTime);
    }

    void ForceStopOverheat()
    {
        if (overheat == null) return;

        if (overheat.isOverheating)
        {
            overheat.ForceStopOverheat();
            Debug.Log("OVERHEAT FORCE STOPPED BY WIGGLE");
        }
    }

    internal void StartEclipse()
    {
        eclipseObject.gameObject.SetActive(true);
        if (gameObject.activeInHierarchy)
            StartCoroutine(EclipseCountdown());
    }

    //Reduce actionbutton fill
    private IEnumerator EclipseCountdown()
    {
        float elapsedTime = 0f;
        while (elapsedTime < SocketHandler.instance.timeout / 1000f) // Convert milliseconds to seconds
        {
            elapsedTime += Time.deltaTime;
            float fillAmount = Mathf.Clamp01(1f - (elapsedTime / (SocketHandler.instance.timeout / 1000f))); // Convert milliseconds to seconds
            eclipseObject.fillAmount = fillAmount;
            yield return null;
        }
    }

    internal void EndEclipse()
    {
        eclipseObject.gameObject.SetActive(false);
        StopCoroutine(EclipseCountdown());
        Debug.Log("ECLIPSE ENDED");
    }
}
