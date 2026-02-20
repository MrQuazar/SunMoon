using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class LobbyManager : MonoBehaviour
{
    public GameObject planet;
    public GameObject player1;
    public GameObject player2;
    public GameObject eclipse;
    public GameObject gameManager;

    public bool roomCreated = false; // Set to true once the room is created
    private bool isPlanetRotating = true;
    public bool isPlayerSun = true;

    // Cinemachine components
    public CinemachineCamera cinemachineCamera;
    public Transform player1CameraTransform;
    public Transform player2CameraTransform;
    public Transform eclipseCameraTransform;
    public float cameraTransitionSpeed = 2f; // Adjust speed of camera pan
    public AudioClip bgMusic;

    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        UnityEngine.Application.targetFrameRate = 120;
        AudioManager.Instance.PlayMusic(bgMusic);
    }

    void Update()
    {
        // Rotate planet if the room is not created
        if (isPlanetRotating)
        {
            planet.transform.Rotate(Vector3.up * Time.deltaTime * 10f); // Adjust speed if needed
        }

        // When the room is created, stop rotating planet and activate necessary objects
        if (roomCreated && isPlanetRotating)
        {
            StopPlanetRotation();
            EnableGameObjects();
            SetPlayerSunStatus();
            HandleCameraTransition();
        }
    }

    void StopPlanetRotation()
    {
        isPlanetRotating = false;
    }

    void EnableGameObjects()
    {
        // Enable player objects and game manager
        player1.SetActive(true);
        player2.SetActive(true);
        gameManager.SetActive(true);
    }

    void SetPlayerSunStatus()
    {
        // Check if myId is at the start or end of receivedIds
        if (isPlayerSun)
        {
            player1.GetComponent<PlayerController>().isPlayerController = true;
            player2.GetComponent<PlayerController>().isPlayerController = false;
        }
        else
        {
            player1.GetComponent<PlayerController>().isPlayerController = false;
            player2.GetComponent<PlayerController>().isPlayerController = true;
        }
    }

    internal void HandleCameraTransition()
    {
        if (isPlayerSun)
        {
            // Disable Cinemachine camera and transition to player1's camera
            cinemachineCamera.gameObject.SetActive(false); // Disable the current Cinemachine camera
            StartCoroutine(PanCameraToTarget(player1CameraTransform)); // Pan to player1's camera
        }
        else
        {
            // Disable Cinemachine camera and transition to player2's camera
            cinemachineCamera.gameObject.SetActive(false); // Disable the current Cinemachine camera
            StartCoroutine(PanCameraToTarget(player2CameraTransform)); // Pan to player2's camera
        }
    }

    internal void HandleEclipseCameraTransition()
    {
        // Disable current camera and transition to eclipse camera
        cinemachineCamera.gameObject.SetActive(true); // Disable the current Cinemachine camera
        if (eclipseCameraTransform == null)
        {
            Debug.LogError("Eclipse camera transform is not assigned!");
            return;
        }
        StartCoroutine(PanCameraToTarget(eclipseCameraTransform)); // Pan to eclipse camera
    }

    IEnumerator PanCameraToTarget(Transform target)
    {
        float timeElapsed = 0f;

        Transform cam = Camera.main.transform;

        Vector3 initialPosition = cam.position;
        Quaternion initialRotation = cam.rotation;

        Vector3 targetPosition = target.position;
        Quaternion targetRotation = target.rotation;

        while (timeElapsed < 1f)
        {
            cam.position = Vector3.Lerp(initialPosition, targetPosition, timeElapsed);
            cam.rotation = Quaternion.Slerp(initialRotation, targetRotation, timeElapsed);

            timeElapsed += Time.deltaTime * cameraTransitionSpeed;
            yield return null;
        }

        // Snap exactly
        cam.position = targetPosition;
        cam.rotation = targetRotation;

        // 🔥 Now make it a child of the TARGET (not the parent)
        cam.SetParent(target);

        // Reset local transform so it follows perfectly
        cam.localPosition = Vector3.zero;
        cam.localRotation = Quaternion.identity;
    }
}