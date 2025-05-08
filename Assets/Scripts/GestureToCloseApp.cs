using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using TMPro;
using UnityEngine.XR;

public class GestureToCloseApp : MonoBehaviour
{
    [SerializeField]
    private float gestureHoldTime = 1f;
    [SerializeField]
    private TextMeshProUGUI debugText;

    private float gestureTimer = 0f;
    private bool isGestureActive = false;
    private Handedness activeHand = Handedness.None;

    private void Update()
    {
        DetectHandGesture();
    }

    private void DetectHandGesture()
    {
        IMixedRealityHand leftHand = HandJointUtils.FindHand(Handedness.Left) as IMixedRealityHand;
        IMixedRealityHand rightHand = HandJointUtils.FindHand(Handedness.Right) as IMixedRealityHand;

        if (leftHand != null && leftHand.Enabled)
        {
            if (ProcessHandGesture(leftHand, Handedness.Left))
            {
                return;
            }
        }

        if (rightHand != null && rightHand.Enabled)
        {
            ProcessHandGesture(rightHand, Handedness.Right);
            return;
        }

        if (!isGestureActive && debugText != null)
        {
            debugText.text = "No fist gesture detected.";
        }
        ResetGesture();
    }

    private bool ProcessHandGesture(IMixedRealityHand hand, Handedness handedness)
    {
        bool hasIndexTip = hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose indexTipPose);
        bool hasMiddleTip = hand.TryGetJoint(TrackedHandJoint.MiddleTip, out MixedRealityPose middleTipPose);
        bool hasRingTip = hand.TryGetJoint(TrackedHandJoint.RingTip, out MixedRealityPose ringTipPose);
        bool hasPinkyTip = hand.TryGetJoint(TrackedHandJoint.PinkyTip, out MixedRealityPose pinkyTipPose);
        bool hasThumbTip = hand.TryGetJoint(TrackedHandJoint.ThumbTip, out MixedRealityPose thumbTipPose);
        bool hasPalm = hand.TryGetJoint(TrackedHandJoint.Palm, out MixedRealityPose palmPose);

        if (!hasIndexTip || !hasMiddleTip || !hasRingTip || !hasPinkyTip || !hasPalm || !hasThumbTip)
        {
            if (debugText != null) debugText.text = $"{handedness} hand: Missing joints.";
            return false;
        }

         bool isFist = IsFingerCurled(indexTipPose, palmPose) &&
                       IsFingerCurled(middleTipPose, palmPose) &&
                       IsFingerCurled(ringTipPose, palmPose) &&
                       IsFingerCurled(pinkyTipPose, palmPose) &&
                       IsThumbCurled(thumbTipPose, palmPose);

        if (isFist)
        {
            if (!isGestureActive)
            {
                isGestureActive = true;
                activeHand = handedness;
                gestureTimer = 0f;
                if (debugText != null)
                {
                    debugText.text = $"{handedness} hand fist detected. Holding...";
                }
                Debug.Log($"{handedness} hand fist detected.");
            }

            gestureTimer += Time.deltaTime;
            if (gestureTimer >= gestureHoldTime)
            {
                QuitApplication();
                ResetGesture();
            }
            return true;
        }

        return false;
    }

    private bool IsFingerCurled(MixedRealityPose fingerTipPose, MixedRealityPose palmPose)
    {
        float distanceToPalm = Vector3.Distance(fingerTipPose.Position, palmPose.Position);
        float curledThreshold = 0.05f;
        return distanceToPalm < curledThreshold;
    }

    private bool IsThumbCurled(MixedRealityPose thumbTipPose, MixedRealityPose palmPose)
    {
        float distanceToPalm = Vector3.Distance(thumbTipPose.Position, palmPose.Position);
        float curledThreshold = 0.07f;
        return distanceToPalm < curledThreshold;
    }

    private void QuitApplication()
    {
        if (debugText != null) debugText.text = "Quitting application...";
        Debug.Log("HandGestureSceneNavigator: Quitting application.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResetGesture()
    {
        isGestureActive = false;
        gestureTimer = 0f;
        activeHand = Handedness.None;
    }

    private void OnDestroy()
    {
        debugText = null;
        Debug.Log("HandGestureSceneNavigator destroyed and cleaned up.");
    }
}


