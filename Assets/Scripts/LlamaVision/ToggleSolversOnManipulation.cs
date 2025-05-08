using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using TMPro;
using UnityEngine;
using System.Diagnostics;

public class ToggleSolversOnObjectManipulation : MonoBehaviour, IMixedRealityPointerHandler
{
    private ObjectManipulator objectManipulator;
    private SolverHandler solverHandler;
    private RadialView radialView;
    private Follow follow;

    private bool isBeingManipulated = false;
    private bool isEyeGazeActive = true; // Panel moves with eye gaze by default

    public GameObject eyeGazeCollider; // Smaller collider for gaze detection
    private IMixedRealityEyeGazeProvider eyeGazeProvider;

    [Header("Layer Settings")]
    public string panelLayer = "Panel";        // Layer for panel collider
    public string eyeGazeLayer = "EyeGaze";   // Layer for eye gaze collider

    [Header("Latency Display")]
    public TextMeshProUGUI manipulationLatencyDisplay; // UI component for manipulation latency
    public TextMeshProUGUI eyeGazeLatencyDisplay;      // UI component for eye gaze latency


    void Start()
    {
        // Get components
        objectManipulator = GetComponent<ObjectManipulator>();
        solverHandler = GetComponent<SolverHandler>();
        radialView = GetComponent<RadialView>();
        follow = GetComponent<Follow>();

        // Register for pointer events
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);

        // Get the Eye Gaze Provider
        eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;

        // Ensure eye gaze collider and panel collider use different layers
        ConfigureColliders();

        // Enable solvers for eye gaze by default
        EnableSolvers();
    }

    void OnDestroy()
    {
        // Unregister for pointer events
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }

    public void OnManipulationStarted()
    {
        isBeingManipulated = true;
        isEyeGazeActive = false; // Disable eye gaze when manipulation starts


        DisableSolvers();
    }

    public void OnManipulationEnded()
    {
        isBeingManipulated = false;
        isEyeGazeActive = true;
    }

    private void DisableSolvers()
    {

        if (solverHandler != null) solverHandler.enabled = false;
        if (radialView != null) radialView.enabled = false;
        if (follow != null) follow.enabled = false;
    }

    private void EnableSolvers()
    {

        if (solverHandler != null) solverHandler.enabled = true;
        if (radialView != null) radialView.enabled = true;
        if (follow != null) follow.enabled = true;

    }

    void Update()
    {
        // Enable solvers only if gaze is on the smaller collider and not during manipulation
        if (isEyeGazeActive && eyeGazeProvider != null && !isBeingManipulated)
        {

            if (eyeGazeProvider.GazeTarget != null && eyeGazeProvider.GazeTarget == eyeGazeCollider)
            {
                EnableSolvers();
            }
        }
    }

    private void ConfigureColliders()
    {
        // Set appropriate layers to avoid interference
        if (gameObject.layer == LayerMask.NameToLayer(panelLayer) &&
            eyeGazeCollider.layer == LayerMask.NameToLayer(eyeGazeLayer))
        {
            // Ignore collisions between the layers
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer(panelLayer), LayerMask.NameToLayer(eyeGazeLayer));
        }
    }

    // IMixedRealityPointerHandler methods
    public void OnPointerClicked(MixedRealityPointerEventData eventData) { }
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        // Start manipulation when pointer is pressed
        //OnManipulationStarted();
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData) { }
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        // End manipulation when pointer is released
       // OnManipulationEnded();
    }
}
