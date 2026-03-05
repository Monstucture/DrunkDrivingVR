using UnityEngine;

/// <summary>
/// Enhanced VehicleManager for VR car enter/exit functionality
/// Attach this to your Car's root GameObject
/// </summary>
public class VehicleManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your OVRPlayerController or XR Rig")]
    public GameObject playerRig;

    [Tooltip("Your car controller script (e.g., CarController2)")]
    public CarController2_VR carController;

    [Tooltip("Empty GameObject at driver's head/seat position")]
    public Transform seatAnchor;

    [Header("Exit Settings")]
    [Tooltip("Distance to move player when exiting (to the side)")]
    public float exitDistance = 1.5f;

    [Tooltip("Exit on the right side of the car (uncheck for left)")]
    public bool exitOnRightSide = true;

    [Header("Optional - Disable Player Movement")]
    [Tooltip("Reference to OVRPlayerController if you want to disable movement")]
    public MonoBehaviour ovrPlayerController;

    private bool inCar = false;
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private Transform originalPlayerParent;

    void Start()
    {
        // Disable car controller at start
        if (carController != null)
        {
            carController.enabled = false;
        }

        // Validate setup
        if (playerRig == null)
        {
            Debug.LogError("PlayerRig not assigned to VehicleManager!");
        }
        if (seatAnchor == null)
        {
            Debug.LogWarning("SeatAnchor not assigned. Creating one at car's position.");
            seatAnchor = new GameObject("SeatAnchor").transform;
            seatAnchor.SetParent(transform);
            seatAnchor.localPosition = new Vector3(0, 1.5f, 0); // Default height
        }
    }

    /// <summary>
    /// Toggle between entering and exiting the car
    /// </summary>
    public void ToggleVehicle()
    {
        if (!inCar)
            EnterCar();
        else
            ExitCar();
    }

    /// <summary>
    /// Enter the car and enable driving
    /// </summary>
    public void EnterCar()
    {
        if (inCar || playerRig == null)
            return;

        Debug.Log("Entering car...");
        inCar = true;

        // Store original position/rotation/parent for exit
        originalPlayerPosition = playerRig.transform.position;
        originalPlayerRotation = playerRig.transform.rotation;
        originalPlayerParent = playerRig.transform.parent;

        // Parent player to the car (this makes player move with the car)
        playerRig.transform.SetParent(transform);

        // Position player at the seat anchor
        playerRig.transform.position = seatAnchor.position;
        playerRig.transform.rotation = seatAnchor.rotation;

        // Disable player walking/movement
        CharacterController characterController = playerRig.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Disable OVR player controller if present
        if (ovrPlayerController != null)
        {
            ovrPlayerController.enabled = false;
        }

        // Enable car driving
        if (carController != null)
        {
            carController.enabled = true;
        }
    }

    /// <summary>
    /// Exit the car and re-enable walking
    /// </summary>
    public void ExitCar()
    {
        if (!inCar || playerRig == null)
            return;

        Debug.Log("Exiting car...");
        inCar = false;

        // Unparent from car
        playerRig.transform.SetParent(originalPlayerParent);

        // Calculate exit position (to the side of the car)
        Vector3 exitDirection = exitOnRightSide ? transform.right : -transform.right;
        Vector3 exitPosition = transform.position + exitDirection * exitDistance;

        // Place player at exit position
        playerRig.transform.position = exitPosition;

        // Keep the rotation or reset it
        // playerRig.transform.rotation = originalPlayerRotation; // Uncomment to reset rotation

        // Re-enable walking
        CharacterController characterController = playerRig.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // Re-enable OVR player controller
        if (ovrPlayerController != null)
        {
            ovrPlayerController.enabled = true;
        }

        // Disable car driving
        if (carController != null)
        {
            carController.enabled = false;
        }
    }

    /// <summary>
    /// Check if player is currently in the car
    /// </summary>
    public bool IsInCar()
    {
        return inCar;
    }

    // Optional: Force exit on destruction
    void OnDestroy()
    {
        if (inCar)
        {
            ExitCar();
        }
    }
}