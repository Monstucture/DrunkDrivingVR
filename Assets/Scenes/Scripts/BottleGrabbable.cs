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

    [Header("Drinking Settings")]
    [Tooltip("How close the bottle must be to the head to count as drinking")]
    public float drinkRange = 0.25f;
    [Tooltip("How many seconds of continuous drinking before registering a sip")]
    public float drinkDuration = 1.5f;
    [Tooltip("Reference to the DrunkEffect script in the scene")]
    // EDITOR SETUP: Drag the OVRCameraRig GameObject into this slot in the Inspector
    public DrunkEffect drunkEffect;

    [Header("Visual Feedback")]
    [Tooltip("Show highlight when hand is in range")]
    public bool showHighlight = true;
    [Tooltip("Material applied when the bottle is in grab range")]
    public Material highlightMaterial;

    private Transform rightHand;
    private Transform headAnchor;
    private Rigidbody rb;
    private Renderer bottleRenderer;
    private Material originalMaterial;

    private bool isHeld = false;
    private bool isInRange = false;
    private bool isDrinking = false;
    private float drinkTimer = 0f;

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

        // Find OVR hand and head anchors
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            rightHand = rig.rightHandAnchor;
            headAnchor = rig.centerEyeAnchor;
        }
        else
        {
            GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
            if (ovrCameraRig != null)
            {
                Transform trackingSpace = ovrCameraRig.transform.Find("TrackingSpace");
                if (trackingSpace != null)
                {
                    rightHand = trackingSpace.Find("RightHandAnchor");
                    headAnchor = trackingSpace.Find("CenterEyeAnchor");
                }
            }
        }

        if (rightHand == null)
            Debug.LogWarning("BottleGrabbable: Could not find RightHandAnchor. Make sure OVRCameraRig is in the scene.");
        if (headAnchor == null)
            Debug.LogWarning("BottleGrabbable: Could not find CenterEyeAnchor. Drinking detection will not work.");
    }

    void Update()
    {
        if (rightHand == null) return;

        TrackHandVelocity();

        if (isHeld)
        {
            HandleDrinking();
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

    void HandleDrinking()
    {
        if (headAnchor == null) return;

        bool bottleNearFace = Vector3.Distance(transform.position, headAnchor.position) <= drinkRange;

        if (bottleNearFace)
        {
            if (!isDrinking)
            {
                isDrinking = true;
                drinkTimer = 0f;
                Debug.Log("[Bottle] Raise bottle to drink...");
            }

            drinkTimer += Time.deltaTime;

            if (drinkTimer >= drinkDuration)
            {
                drinkTimer = 0f;
                OnSipTaken();
            }
        }
        else
        {
            if (isDrinking)
            {
                isDrinking = false;
                drinkTimer = 0f;
                Debug.Log("[Bottle] Stopped drinking.");
            }
        }
    }

    void OnSipTaken()
    {
        Debug.Log("[Bottle] Drinking! Impairment increasing.");
        if (drunkEffect != null)
            drunkEffect.AddSip();
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
        isDrinking = false;
        drinkTimer = 0f;

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
