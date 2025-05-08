using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine.SceneManagement;
using TMPro;

public class HandGesture : MonoBehaviour
{
    [SerializeField]
    private float gestureHoldTime = 1f; // Time to hold gesture before triggering (seconds)
    [SerializeField]
    private TextMeshProUGUI debugText; // Optional: For debugging gesture detection

    private float gestureTimer = 0f;
    private bool isGestureActive = false;
    private MixedRealityPose thumbTipPose;
    private MixedRealityPose indexTipPose;

    private void Update()
    {
        DetectHandGesture();
    }

    private void DetectHandGesture()
    {
        // Get the left hand controller
        IMixedRealityHand leftHand = HandJointUtils.FindHand(Handedness.Left) as IMixedRealityHand;
        if (leftHand == null || !leftHand.Enabled)
        {
            if (debugText != null) debugText.text = "Right hand not tracked.";
            ResetGesture();
            return;
        }

        // Get hand joint poses
        bool hasThumbTip = leftHand.TryGetJoint(TrackedHandJoint.ThumbTip, out thumbTipPose);
        bool hasIndexTip = leftHand.TryGetJoint(TrackedHandJoint.IndexTip, out indexTipPose);
        bool hasMiddleTip = leftHand.TryGetJoint(TrackedHandJoint.MiddleTip, out MixedRealityPose middleTipPose);
        bool hasRingTip = leftHand.TryGetJoint(TrackedHandJoint.RingTip, out MixedRealityPose ringTipPose);
        bool hasPinkyTip = leftHand.TryGetJoint(TrackedHandJoint.PinkyTip, out MixedRealityPose pinkyTipPose);
        bool hasPalm = leftHand.TryGetJoint(TrackedHandJoint.Palm, out MixedRealityPose palmPose);

        if (!hasThumbTip || !hasIndexTip || !hasMiddleTip || !hasRingTip || !hasPinkyTip || !hasPalm)
        {
            ResetGesture();
            return;
        }

        // Check if fingers are curled (fist)
        bool isFist = IsFingerCurled(indexTipPose, palmPose) &&
                      IsFingerCurled(middleTipPose, palmPose) &&
                      IsFingerCurled(ringTipPose, palmPose) &&
                      IsFingerCurled(pinkyTipPose, palmPose);

        // Check if thumb is extended and pointing left
        bool isThumbLeft = IsThumbPointingLeft(thumbTipPose, palmPose);

        if (isFist && isThumbLeft)
        {
            if (!isGestureActive)
            {
                isGestureActive = true;
                gestureTimer = 0f;
            }

            gestureTimer += Time.deltaTime;
            if (gestureTimer >= gestureHoldTime)
            {
                ResetGesture();
                SceneManager.LoadScene("YoloScene");
                
            }
        }
        else
        {
            if (debugText != null) debugText.text = $"Fist: {isFist}, Thumb Left: {isThumbLeft}";
            ResetGesture();
        }
    }

    private bool IsFingerCurled(MixedRealityPose fingerTipPose, MixedRealityPose palmPose)
    {
        // Check if finger tip is closer to palm than extended
        float distanceToPalm = Vector3.Distance(fingerTipPose.Position, palmPose.Position);
        float curledThreshold = 0.05f; // Adjust based on hand size (meters)
        return distanceToPalm < curledThreshold;
    }

    private bool IsThumbPointingLeft(MixedRealityPose thumbTipPose, MixedRealityPose palmPose)
    {
        // Check thumb orientation relative to palm
        Vector3 thumbDirection = thumbTipPose.Position - palmPose.Position;
        Vector3 palmRight = palmPose.Right; // Right direction of palm in world space

        // Project thumb direction onto palm's right axis
        float dotProduct = Vector3.Dot(thumbDirection.normalized, palmRight); // Negative for left
        float leftThreshold = 0.7f; // Thumb should align strongly with left direction

        // Ensure thumb is extended (not curled)
        float distanceToPalm = Vector3.Distance(thumbTipPose.Position, palmPose.Position);
        float extendedThreshold = 0.08f; // Adjust for thumb length (meters)

        return dotProduct > leftThreshold && distanceToPalm > extendedThreshold;
    }

    private bool IsLlamaScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        return (currentSceneIndex == 1);
    }

    private bool IsYoloScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        return (currentSceneIndex == 0);
    }

    private void NavigateToNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = (currentSceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(nextSceneIndex);
        if (debugText != null) debugText.text = $"Navigated to scene: {nextSceneIndex}";
        Debug.Log($"Navigated to scene: {nextSceneIndex}");
    }

    private void ResetGesture()
    {
        isGestureActive = false;
        gestureTimer = 0f;
    }

    private void OnDestroy()
    {
        // Clean up if needed
    }
}