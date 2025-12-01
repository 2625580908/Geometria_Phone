using UnityEngine;
using UnityEngine.XR;

public class SketchPen : MonoBehaviour
{
    public Transform drawingHand; // The controller transform
    public GameObject linePrefab; // Prefab with LineRenderer component
    public Transform worldRoot;   // Parent to the graph root so drawings move with the world
    
    private LineRenderer currentLine;
    private bool isDrawing = false;
    private int positionCount = 0;

    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand); // Default to Right hand
        bool triggerVal;
        device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerVal);

        if (triggerVal && !isDrawing)
        {
            StartDrawing();
        }
        else if (!triggerVal && isDrawing)
        {
            isDrawing = false;
            currentLine = null;
        }
        else if (isDrawing && currentLine != null)
        {
            UpdateDrawing();
        }
    }

    void StartDrawing()
    {
        isDrawing = true;
        GameObject go = Instantiate(linePrefab, worldRoot);
        currentLine = go.GetComponent<LineRenderer>();
        currentLine.useWorldSpace = false; // Important so it scales with root
        positionCount = 0;
        currentLine.positionCount = 0;
    }

    void UpdateDrawing()
    {
        Vector3 newPos = drawingHand.position;
        
        // Optimization: Only add point if moved enough
        if (positionCount > 0 && Vector3.Distance(currentLine.GetPosition(positionCount - 1), worldRoot.InverseTransformPoint(newPos)) < 0.01f)
            return;

        positionCount++;
        currentLine.positionCount = positionCount;
        // Convert world controller pos to local space of the graph root
        currentLine.SetPosition(positionCount - 1, worldRoot.InverseTransformPoint(newPos));
    }
}