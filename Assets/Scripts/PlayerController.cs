using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Planet")]
    public Transform planet;
    public float radius = 10f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    [Header("Controls")]
    public bool useArrowKeys = false;

    [Header("Collision")]
    public float collisionRadius = 0.5f;
    public LayerMask playerLayer;

    Vector3 forward;
    Vector3 right;

    void Start()
    {
        InitializePosition();
    }

    void FixedUpdate()
    {
        HandleMovement();
        AlignToSurface();
    }

    void InitializePosition()
    {
        Vector3 normal = GetNormal();
        transform.position = planet.position + normal * radius;

        forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;

        if (forward == Vector3.zero)
            forward = Vector3.Cross(normal, Vector3.right).normalized;
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
            if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
            if (Input.GetKey(KeyCode.W)) v += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;
        }

        if (Mathf.Abs(h) < 0.001f && Mathf.Abs(v) < 0.001f)
            return; // no input

        Vector3 normal = GetNormal();

        // Keep movement aligned to planet surface
        Vector3 forwardDir = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        Vector3 rightDir = Vector3.ProjectOnPlane(transform.right, normal).normalized;

        Vector3 desiredMove = (forwardDir * v + rightDir * h).normalized * moveSpeed * Time.fixedDeltaTime;

        Vector3 newPos = transform.position + desiredMove;

        newPos = planet.position + (newPos - planet.position).normalized * radius;

        if (!Physics.CheckSphere(newPos, collisionRadius, playerLayer))
        {
            transform.position = newPos;
        }
    }

    void AlignToSurface()
    {
        Vector3 normal = GetNormal();
        Quaternion target = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 12f * Time.fixedDeltaTime);
    }
}
