

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraDataCollector2 : MonoBehaviour
{
    public Action<Texture2D> OnImageCaptured;
    private WebCamTexture webCamTexture;
    private Texture2D capturedImage;
    private bool isCameraRunning;
    public TMPro.TextMeshProUGUI t;

    private void Awake()
    {
        webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 640, 480, 30);
    }

    public void StartCamera()
    {
        if (!isCameraRunning) 
        {
            if (webCamTexture == null)
            {
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 640, 480, 30);
            }
            webCamTexture.Play();
            isCameraRunning = true;
        }
        
    }

    public void StopCamera()
    {
        if (isCameraRunning)
        {
            webCamTexture.Stop();
            isCameraRunning = false;
        }
    }

    public IEnumerator CaptureImage()
    {
        if (t != null) t.text += "\nBefore CaptureImageCoroutine";
        Debug.Log("Before CaptureImageCoroutine");

        float timeout = 10f; // Increased timeout
        float elapsed = 0f;
        while (!webCamTexture.isPlaying && elapsed < timeout)
        {
            if (t != null) t.text += "\nWaiting for webcam to start...";
            Debug.Log("Waiting for webcam to start...");
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!webCamTexture.isPlaying)
        {
            if (t != null) t.text += "\nWebcam failed to start!";
            Debug.LogError("Webcam failed to start!");
            yield break;
        }

        // Wait for a few frames to ensure valid data
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        if (t != null) t.text += "\nCaptureImageCoroutine";
        Debug.Log("CaptureImageCoroutine - Webcam is playing");

        int width = webCamTexture.width;
        int height = webCamTexture.height;

        if (capturedImage == null || capturedImage.width != width || capturedImage.height != height)
        {
            capturedImage = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        capturedImage.SetPixels(webCamTexture.GetPixels());
        capturedImage.Apply();

        // Validate image (check if it's mostly black)
        if (IsImageBlack(capturedImage))
        {
            if (t != null) t.text += "\nCaptured image is black! Retrying...";
            Debug.LogWarning("Captured image is black! Retrying...");
            yield return new WaitForSeconds(0.5f);
            yield return CaptureImage(); // Retry
            yield break;
        }

        if (t != null) t.text += "\nImage captured and invoking OnImageCaptured";
        Debug.Log("Image captured and invoking OnImageCaptured");
        OnImageCaptured?.Invoke(capturedImage);
    }

    private bool IsImageBlack(Texture2D image)
    {
        Color[] pixels = image.GetPixels();
        float totalBrightness = 0f;
        for (int i = 0; i < pixels.Length; i += 10) // Sample every 10th pixel
        {
            totalBrightness += pixels[i].grayscale;
        }
        float averageBrightness = totalBrightness / (pixels.Length / 10f);
        return averageBrightness < 0.05f; // Threshold for "black" image
    }

    public void StartCapturing()
    {
        StartCamera();
        StartCoroutine(CaptureImage());
    }

    private void OnDestroy()
    {
        StopCamera();
    }
}