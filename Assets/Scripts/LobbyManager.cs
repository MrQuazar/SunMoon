using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class LobbyManager : MonoBehaviour
{
    public GameObject planet;
    public GameObject player1;
    public GameObject player2;
    public GameObject playerManager;
    public Canvas mainMenuCanvas;
    public Canvas mainGameCanvas;

    public bool roomCreated = false; // Set to true once the room is created
    public string myId;
    public string[] receivedIds;

    private bool isPlanetRotating = true;

    // Cinemachine components
    public CinemachineCamera cinemachineCamera;
    public Transform player1CameraTransform;
    public Transform player2CameraTransform;
    public float cameraTransitionSpeed = 2f; // Adjust speed of camera pan

    void Update()
    {
        // Rotate planet if the room is not created
        if (isPlanetRotating)
        {
            planet.transform.Rotate(Vector3.up * Time.deltaTime * 10f); // Adjust speed if needed
        }

        // When the room is created, stop rotating planet and activate necessary objects
        if (roomCreated)
        {
            StopPlanetRotation();
            EnableGameObjects();
            SetPlayerSunStatus();
            SwitchCanvases();
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
        playerManager.SetActive(true);
    }

    void SetPlayerSunStatus()
    {
        // Check if myId is at the start or end of receivedIds
        if (receivedIds.Length > 0 && receivedIds[0] == myId)
        {
            playerManager.GetComponent<PlayerManager>().isPlayerSun = true;
        }
        else
        {
            playerManager.GetComponent<PlayerManager>().isPlayerSun = false;
        }
    }

    void SwitchCanvases()
    {
        // Disable the main menu canvas and enable the main game canvas
        mainMenuCanvas.enabled = false;
        mainGameCanvas.enabled = true;
    }

    void HandleCameraTransition()
    {
        if (playerManager.GetComponent<PlayerManager>().isPlayerSun)
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

    IEnumerator PanCameraToTarget(Transform target)
    {
        // Pan camera to the new target smoothly
        float timeElapsed = 0f;
        Vector3 initialPosition = Camera.main.transform.position;
        Quaternion initialRotation = Camera.main.transform.rotation;
        Vector3 targetPosition = target.position;
        Quaternion targetRotation = target.rotation;

        while (timeElapsed < 1f)
        {
            Camera.main.transform.position = Vector3.Lerp(initialPosition, targetPosition, timeElapsed);
            Camera.main.transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, timeElapsed);
            timeElapsed += Time.deltaTime * cameraTransitionSpeed; // Adjust speed of transition
            yield return null;
        }

        // Ensure the camera reaches the exact target position and rotation
        Camera.main.transform.position = targetPosition;
        Camera.main.transform.rotation = targetRotation;
    }
}
