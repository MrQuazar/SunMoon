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

    [Header("PC Controls")]
    public bool useArrowKeys = false;

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
        float h = 0f; // LEFT/RIGHT
        float v = 0f; // UP/DOWN

#if UNITY_EDITOR || UNITY_STANDALONE
        // ================================
        // PC CONTROLS (as is, no change)
        // ================================
        if (useArrowKeys)
        {
            if (Input.GetKey(KeyCode.UpArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.DownArrow)) h += 1f;

            if (Input.GetKey(KeyCode.LeftArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) v += 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.W)) h -= 1f; // up
            if (Input.GetKey(KeyCode.S)) h += 1f; // down

            if (Input.GetKey(KeyCode.A)) v -= 1f; // left
            if (Input.GetKey(KeyCode.D)) v += 1f; // right
        }

#else
    // ================================
    // ANDROID CONTROLS
    // ================================
    if (useArrowKeys)
    {
        // If using arrow keys on Android, don't allow player to move
        h = 0f;
        v = 0f;
    }
    else
    {
        // Joystick controls for Android
        if (useJoystick && joystick != null)
        {
            Vector2 input = joystick.InputDirection;

            h = -input.y;   // left/right
            v = input.x;  // flipped forward/back
        }

        // Gyro controls for Android
        if (useGyro && gyroAvailable)
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
    }
#endif

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
