using UnityEngine;

/// <summary>
/// Attach this to your DoorHandle GameObject to enable OVR/Meta Quest grab interaction
/// Works with OVR/Oculus Integration package
/// </summary>
public class DoorHandleInteraction_OVR : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your VehicleManager script reference here")]
    public VehicleManager vehicleManager;

    [Header("Interaction Settings")]
    [Tooltip("Distance from door handle to trigger interaction")]
    public float grabDistance = 0.3f;
    
    [Tooltip("Show visual feedback when player can interact")]
    public bool showInteractionFeedback = true;
    
    [Tooltip("Material to apply when handle is highlighted")]
    public Material highlightMaterial;

    private Transform leftHand;
    private Transform rightHand;
    private Material originalMaterial;
    private Renderer handleRenderer;
    private bool isNearHandle = false;
    private OVRInput.Controller lastActiveController;

    void Start()
    {
        // Find OVR hand anchors
        GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
        if (ovrCameraRig != null)
        {
            Transform trackingSpace = ovrCameraRig.transform.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                leftHand = trackingSpace.Find("LeftHandAnchor");
                rightHand = trackingSpace.Find("RightHandAnchor");
            }
        }

        // Alternative: Find by tag or component
        if (leftHand == null || rightHand == null)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
            {
                leftHand = rig.leftHandAnchor;
                rightHand = rig.rightHandAnchor;
            }
        }

        // Get renderer for visual feedback
        handleRenderer = GetComponent<Renderer>();
        if (handleRenderer != null)
        {
            originalMaterial = handleRenderer.material;
        }

        // Validate VehicleManager reference
        if (vehicleManager == null)
        {
            Debug.LogError("VehicleManager reference not set on DoorHandleInteraction_OVR!");
        }

        if (leftHand == null || rightHand == null)
        {
            Debug.LogWarning("Could not find OVR hand anchors. Make sure OVRCameraRig is in the scene.");
        }
    }

    void Update()
    {
        if (vehicleManager == null || (leftHand == null && rightHand == null))
            return;

        // Check distance to each hand
        bool wasNearHandle = isNearHandle;
        isNearHandle = false;

        if (leftHand != null && Vector3.Distance(transform.position, leftHand.position) < grabDistance)
        {
            isNearHandle = true;
            lastActiveController = OVRInput.Controller.LTouch;
        }
        else if (rightHand != null && Vector3.Distance(transform.position, rightHand.position) < grabDistance)
        {
            isNearHandle = true;
            lastActiveController = OVRInput.Controller.RTouch;
        }

        // Update visual feedback
        if (showInteractionFeedback && handleRenderer != null)
        {
            if (isNearHandle && !wasNearHandle && highlightMaterial != null)
            {
                handleRenderer.material = highlightMaterial;
            }
            else if (!isNearHandle && wasNearHandle && originalMaterial != null)
            {
                handleRenderer.material = originalMaterial;
            }
        }

        // Check for grab input
        if (isNearHandle)
        {
            // Check for grip button press (most common grab button)
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, lastActiveController) ||
                OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, lastActiveController))
            {
                vehicleManager.ToggleVehicle();
            }
        }
    }

    // Optional: Draw gizmo to visualize grab distance
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, grabDistance);
    }
}
