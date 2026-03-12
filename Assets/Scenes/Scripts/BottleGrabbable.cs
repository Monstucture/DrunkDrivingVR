using UnityEngine;

/// <summary>
/// Attach to the alcohol bottle GameObject to enable right-hand grab/drop interaction.
/// Uses OVR/Meta Quest APIs consistent with the rest of the project.
/// Requires a Rigidbody and a Collider on the same GameObject.
/// </summary>
public class BottleGrabbable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How close the right hand must be to grab the bottle")]
    public float grabRange = 0.15f;

    [Header("Visual Feedback")]
    [Tooltip("Show highlight when hand is in range")]
    public bool showHighlight = true;
    [Tooltip("Material applied when the bottle is in grab range")]
    public Material highlightMaterial;

    private Transform rightHand;
    private Rigidbody rb;
    private Renderer bottleRenderer;
    private Material originalMaterial;

    private bool isHeld = false;
    private bool isInRange = false;

    // Used to calculate throw velocity when dropping
    private Vector3 prevHandPos;
    private Vector3 handVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("BottleGrabbable requires a Rigidbody component on " + gameObject.name);

        bottleRenderer = GetComponentInChildren<Renderer>();
        if (bottleRenderer != null)
            originalMaterial = bottleRenderer.material;

        // Find OVR right hand anchor
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            rightHand = rig.rightHandAnchor;
        }
        else
        {
            GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
            if (ovrCameraRig != null)
            {
                Transform trackingSpace = ovrCameraRig.transform.Find("TrackingSpace");
                if (trackingSpace != null)
                    rightHand = trackingSpace.Find("RightHandAnchor");
            }
        }

        if (rightHand == null)
            Debug.LogWarning("BottleGrabbable: Could not find RightHandAnchor. Make sure OVRCameraRig is in the scene.");
    }

    void Update()
    {
        if (rightHand == null) return;

        TrackHandVelocity();

        if (isHeld)
        {
            HandleDrop();
        }
        else
        {
            CheckGrabRange();
            if (isInRange)
                HandleGrab();
        }
    }

    void TrackHandVelocity()
    {
        handVelocity = (rightHand.position - prevHandPos) / Time.deltaTime;
        prevHandPos = rightHand.position;
    }

    void CheckGrabRange()
    {
        bool wasInRange = isInRange;
        isInRange = Vector3.Distance(transform.position, rightHand.position) <= grabRange;

        if (showHighlight && bottleRenderer != null && highlightMaterial != null)
        {
            if (isInRange && !wasInRange)
                bottleRenderer.material = highlightMaterial;
            else if (!isInRange && wasInRange)
                bottleRenderer.material = originalMaterial;
        }
    }

    void HandleGrab()
    {
        // Right index trigger pressed
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            PickUp();
        }
    }

    void HandleDrop()
    {
        // Right index trigger released
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Drop();
        }
    }

    void PickUp()
    {
        if (rb == null) return;

        isHeld = true;
        isInRange = false;

        // Restore material in case it was highlighted
        if (bottleRenderer != null && originalMaterial != null)
            bottleRenderer.material = originalMaterial;

        // Disable physics while held so the bottle follows the hand
        rb.isKinematic = true;

        // Parent to hand so it moves with it
        transform.SetParent(rightHand, worldPositionStays: true);

        prevHandPos = rightHand.position;
    }

    void Drop()
    {
        if (rb == null) return;

        isHeld = false;

        // Unparent before re-enabling physics
        transform.SetParent(null, worldPositionStays: true);

        rb.isKinematic = false;

        // Apply the hand's velocity so the bottle can be thrown
        rb.linearVelocity = handVelocity;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isHeld ? Color.cyan : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRange);
    }
}
