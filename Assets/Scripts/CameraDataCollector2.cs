

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

        float timeout = 5f;
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

        if (t != null) t.text += "\nCaptureImageCoroutine";
        Debug.Log("CaptureImageCoroutine - Webcam is playing");

        yield return new WaitForEndOfFrame();

        int width = webCamTexture.width;
        int height = webCamTexture.height;

        if (capturedImage == null || capturedImage.width != width || capturedImage.height != height)
        {
            capturedImage = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        capturedImage.SetPixels(webCamTexture.GetPixels());
        capturedImage.Apply();

        if (t != null) t.text += "\nImage captured and invoking OnImageCaptured";
        Debug.Log("Image captured and invoking OnImageCaptured");
        OnImageCaptured?.Invoke(capturedImage);
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