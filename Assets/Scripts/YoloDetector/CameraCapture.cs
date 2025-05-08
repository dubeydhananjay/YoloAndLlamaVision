using UnityEngine;
using UnityEngine.Windows.WebCam;
using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit;
using TMPro;

public class CameraCapture : MonoBehaviour
{
    public Action<Texture2D, float> OnImageCaptured; // Updated to include depth
    private PhotoCapture photoCaptureObject = null;
    private const int captureWidth = 896;  // HoloLens 2 RGB camera resolution
    private const int captureHeight = 896; // Adjusted to match YOLO input after letterboxing
    private Texture2D capturedImage;
    private Camera mainCamera;
    public TextMeshProUGUI detectionResultsText;

    void Awake()
    {
        mainCamera = Camera.main;
        InitializeCamera();
    }

    private void InitializeCamera()
    {
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    private void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        detectionResultsText.text = "PhotoCapture created.\n";
        if (captureObject == null)
        {
            detectionResultsText.text = "Failed to create PhotoCapture object.\n";
            return;
        }

        photoCaptureObject = captureObject;
        CameraParameters cameraParameters = new CameraParameters
        {
            hologramOpacity = 0.0f,
            cameraResolutionWidth = captureWidth,
            cameraResolutionHeight = captureHeight,
            pixelFormat = CapturePixelFormat.BGRA32 // RGB + Alpha
        };

        photoCaptureObject.StartPhotoModeAsync(cameraParameters, OnPhotoModeStarted);
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        detectionResultsText.text = $"Photo mode started: {result.success}, hResult: {result.hResult}\n";
        if (!result.success)
        {
            detectionResultsText.text = "Failed to start photo mode: " + result.hResult + "\n";
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }
    }

    public void StartCapturing(float captureInterval = 2.0f)
    {
        if (photoCaptureObject != null)
        {
            StartCoroutine(CaptureImageCoroutine(captureInterval));
        }
        else
        {
            Debug.LogWarning("Camera not initialized. Retrying...");
            InitializeCamera();
        }
    }

    private IEnumerator CaptureImageCoroutine(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            if (photoCaptureObject != null)
            {
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            }
        }
    }

    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        detectionResultsText.text = $"Photo captured: {result.success}, hResult: {result.hResult}\n";

        if (result.success)
        {
            if (capturedImage == null || capturedImage.width != captureWidth || capturedImage.height != captureHeight)
            {
                capturedImage = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            }

            photoCaptureFrame.UploadImageDataToTexture(capturedImage);
            capturedImage.Apply();

            // Estimate depth at the center of the image using spatial awareness
            float estimatedDepth = GetEstimatedDepthFromSpatialMapping();

            OnImageCaptured?.Invoke(capturedImage, estimatedDepth);
        }
        else
        {
            Debug.LogError("Failed to capture photo: " + result.hResult);
        }
    }

    private float GetEstimatedDepthFromSpatialMapping()
    {
        // Use MRTK spatial awareness to estimate depth at the center of the image
        Vector3 centerScreenPoint = new Vector3(captureWidth / 2f, captureHeight / 2f, 0);
        Ray ray = mainCamera.ScreenPointToRay(centerScreenPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10f)) // Max distance of 10 meters
        {
            return hit.distance;
        }

        return 2.0f; // Fallback depth if no hit
    }

    private void OnDestroy()
    {
        if (photoCaptureObject != null)
        {
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
    }

    private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}