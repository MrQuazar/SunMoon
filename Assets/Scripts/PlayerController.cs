using UnityEngine;
using PinePie.SimpleJoystick;

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
    public bool useJoystick = true;
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
        }
        else
        {
            gyroAvailable = false;
            useGyro = false;
        }
    }

    void HandleMovement()
    {
        if (!isPlayerController)
        {
            recievedLocation = SocketHandler.instance.recievedLocation;
            Debug.Log("Received location: " + recievedLocation);
            if (!WouldCollideWithOtherPlayer(recievedLocation))
            {
                transform.position = recievedLocation;
            }
            return;
        }
        float h = 0f; // LEFT/RIGHT
        float v = 0f; // UP/DOWN

        // ================================
        // ANDROID CONTROLS
        // ================================
        // Joystick controls for Android
        if (useJoystick && joystick != null)
        {
            Vector2 input = joystick.InputDirection;

            h = -input.y;   // left/right
            v = input.x;  // flipped forward/back
        }

        // Gyro controls for Android
        else if (useGyro && gyroAvailable)
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
        }

        // Calculate movement direction and apply it to player
        Vector3 normal = GetNormal();
        Vector3 forwardDir = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        Vector3 rightDir = Vector3.ProjectOnPlane(transform.right, normal).normalized;

        Vector3 moveDir = (forwardDir * v + rightDir * h);

        if (moveDir.sqrMagnitude < 0.001f)
            return;

        moveDir.Normalize();

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
}
