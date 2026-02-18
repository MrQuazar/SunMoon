using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Planet")]
    public Transform planet;
    public float radius = 10f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 12f;
    [Header("Controls")]
    public bool useArrowKeys = false;

    Vector3 forward;
    Vector3 right;
    Rigidbody rb;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
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
        rb.MovePosition(planet.position + normal * radius);

        forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;

        if (forward == Vector3.zero)
            forward = Vector3.Cross(normal, Vector3.right).normalized;
    }

    Vector3 GetNormal()
    {
        return (rb.position - planet.position).normalized;
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

        Vector3 normal = GetNormal();

        right = Vector3.Cross(forward, normal).normalized;
        forward = Vector3.Cross(normal, right).normalized;

        Vector3 move = forward * v + right * h;

        Vector3 newPos = rb.position;

        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();
            newPos += move * moveSpeed * Time.fixedDeltaTime;

            Quaternion targetRot = Quaternion.LookRotation(move, normal);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }

        newPos = planet.position + (newPos - planet.position).normalized * radius;

        rb.MovePosition(newPos);
    }

    void AlignToSurface()
    {
        Vector3 normal = GetNormal();
        Quaternion target = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }
}
