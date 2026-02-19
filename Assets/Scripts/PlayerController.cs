using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Planet")]
    public Transform planet;
    public float radius = 10f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float alignSpeed = 12f;

    [Header("Controls")]
    public bool useArrowKeys = false;

    [Header("Blocking")]
    public float playerCollisionRadius = 0.5f;
    public GameObject otherPlayer;

    void Start()
    {
        InitializePosition();
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

    void HandleMovement()
    {
        float h = 0f;
        float v = 0f;

        if (useArrowKeys)
        {
            if (Input.GetKey(KeyCode.UpArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.DownArrow)) h += 1f;
            if (Input.GetKey(KeyCode.RightArrow)) v += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) v -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.W)) h -= 1f;
            if (Input.GetKey(KeyCode.S)) h += 1f;
            if (Input.GetKey(KeyCode.D)) v += 1f;
            if (Input.GetKey(KeyCode.A)) v -= 1f;
        }

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

        // Block movement if it overlaps another player
        if (!WouldCollideWithOtherPlayer(newPos))
        {
            transform.position = newPos;
        }
    }

    bool WouldCollideWithOtherPlayer(Vector3 targetPos)
    {
        PlayerController other = otherPlayer.GetComponent<PlayerController>();
        if (other == null)
            return false;

        float combinedRadius = playerCollisionRadius + other.playerCollisionRadius;

        float dist = Vector3.Distance(targetPos, otherPlayer.transform.position);
        if (dist < combinedRadius)
            return true;
        return false;
    }

    void AlignToSurface()
    {
        Vector3 normal = GetNormal();
        Quaternion targetRot = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignSpeed * Time.deltaTime);
    }
}
