using UnityEngine;
using UnityEngine.XR;

public class TwoHandManipulator : MonoBehaviour
{
    public Transform targetRoot; // The "GraphGroup" object
    
    // Assign these in Inspector to your XR Rigs Left/Right controllers (Objects that move)
    public Transform leftController; 
    public Transform rightController;

    private bool leftGripped = false;
    private bool rightGripped = false;

    // Manipulation state
    private float initialDist;
    private Vector3 initialScale;
    private Vector3 initialRootPos;
    private Quaternion initialRootRot;
    private Vector3 initialMidpoint;
    private float initialAngle;

    void Update()
    {
        CheckInput();

        if (leftGripped && rightGripped)
        {
            HandleTwoHanded();
        }
        else if (leftGripped)
        {
            HandleOneHanded(leftController);
        }
        else if (rightGripped)
        {
            HandleOneHanded(rightController);
        }
    }

    void CheckInput()
    {
        // Simple Input check (works with standard XR plugin)
        // You might need to change "Grip" to "Trigger" depending on pref
        InputDevice leftDev = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice rightDev = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool lPrev = leftGripped;
        bool rPrev = rightGripped;

        leftDev.TryGetFeatureValue(CommonUsages.gripButton, out leftGripped);
        rightDev.TryGetFeatureValue(CommonUsages.gripButton, out rightGripped);

        // Detect state change to Initialize logic
        if ((leftGripped && rightGripped) && (!lPrev || !rPrev))
        {
            InitializeTwoHand();
        }
        else if (leftGripped && !lPrev && !rightGripped) InitializeOneHand(leftController);
        else if (rightGripped && !rPrev && !leftGripped) InitializeOneHand(rightController);
    }

    void InitializeOneHand(Transform activeHand)
    {
        initialRootPos = targetRoot.position;
        // Store offset relative to hand
        initialMidpoint = activeHand.position; 
    }

    void HandleOneHanded(Transform activeHand)
    {
        Vector3 delta = activeHand.position - initialMidpoint;
        targetRoot.position = initialRootPos + delta;
    }

    void InitializeTwoHand()
    {
        initialDist = Vector3.Distance(leftController.position, rightController.position);
        initialScale = targetRoot.localScale;
        initialRootPos = targetRoot.position;
        initialRootRot = targetRoot.rotation;
        initialMidpoint = (leftController.position + rightController.position) * 0.5f;

        Vector3 dir = rightController.position - leftController.position;
        initialAngle = Mathf.Atan2(dir.z, dir.x);
    }

    void HandleTwoHanded()
    {
        // 1. Scale
        float currentDist = Vector3.Distance(leftController.position, rightController.position);
        float scaleFactor = currentDist / initialDist;
        targetRoot.localScale = initialScale * scaleFactor;

        // 2. Position
        Vector3 currentMidpoint = (leftController.position + rightController.position) * 0.5f;
        Vector3 moveDelta = currentMidpoint - initialMidpoint;
        targetRoot.position = initialRootPos + moveDelta;

        // 3. Rotation (Yaw only to prevent nausea)
        Vector3 dir = rightController.position - leftController.position;
        float currentAngle = Mathf.Atan2(dir.z, dir.x);
        float deltaAngle = currentAngle - initialAngle;
        
        // Negative delta to match intuition
        Quaternion rot = Quaternion.AngleAxis(-deltaAngle * Mathf.Rad2Deg, Vector3.up);
        targetRoot.rotation = initialRootRot * rot;
    }
}