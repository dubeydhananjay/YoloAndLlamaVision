using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Microsoft.MixedReality.Toolkit;
using Newtonsoft.Json;

public class YoloObjectDeetctor : MonoBehaviour
{
    private string serverUrl = "https://9956-128-235-248-150.ngrok-free.app/process?type=image"; // Update as needed
    public TextMeshProUGUI detectionResultsText;
    public BoundingBoxHolder boundingBoxHolderPrefab;
    public Transform boxParent;
    public Camera mainCamera;

    private Texture2D inputImage;
    private float latestDepthEstimate; // Store depth from CameraDataCollector2
    public CameraCapture cameraDataCollector;
    private List<BoundingBoxHolder> boundingBoxHolders = new List<BoundingBoxHolder>();
    private const int MaxBoundingBoxes = 5;

    void Start()
    {
        if (cameraDataCollector != null)
        {
            cameraDataCollector.OnImageCaptured += OnImageCaptured;
        }

        // Pre-instantiate bounding boxes
        for (int i = 0; i < MaxBoundingBoxes; i++)
        {
            BoundingBoxHolder boundingBoxHolder = Instantiate(boundingBoxHolderPrefab, boxParent);
            boundingBoxHolder.gameObject.SetActive(false);
            boundingBoxHolders.Add(boundingBoxHolder);
        }

        StartDetection();
    }

    public void StartDetection()
    {
        if (cameraDataCollector != null)
        {
            cameraDataCollector.StartCapturing(2.0f); // Capture every 2 seconds
        }
    }

    private void OnImageCaptured(Texture2D capturedImage, float depthEstimate)
    {
        inputImage = capturedImage;
        latestDepthEstimate = depthEstimate;
        StartCoroutine(UploadImageAsync());
    }

    private IEnumerator UploadImageAsync()
    {
        if (inputImage == null)
        {
            detectionResultsText.text = "No image captured.";
            yield break;
        }

        byte[] imageData = inputImage.EncodeToJPG();
        if (imageData == null || imageData.Length == 0)
        {
            detectionResultsText.text = "Failed to encode image.";
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageData, "image.jpg", "image/jpeg");

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            www.certificateHandler = new CustomCertificateHandler();
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                detectionResultsText.text = "Error sending image to server: " + www.error;
            }
            else if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                ProcessDetectionResults(jsonResponse);
            }
            else
            {
                detectionResultsText.text = "Unexpected result: " + www.result;
            }
        }
    }

    private void ProcessDetectionResults(string jsonResponse)
    {
        try
        {
            DetectionResults detections = JsonConvert.DeserializeObject<DetectionResults>(jsonResponse);

            detectionResultsText.text = "Detected Objects: \n";
            foreach (var detection in detections.objects)
            {
                detectionResultsText.text += $"{detection.@class}\n";
            }

            int n = Mathf.Min(detections.objects.Count, MaxBoundingBoxes);
            for (int i = 0; i < n; i++)
            {
                boundingBoxHolders[i].gameObject.SetActive(true);
                UpdateBoundingBox(boundingBoxHolders[i], detections.objects[i]);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error parsing JSON response: " + ex.Message);
            detectionResultsText.text = "Error parsing JSON response: " + ex.Message;
        }
    }

    private void UpdateBoundingBox(BoundingBoxHolder boundingBoxHolder, Detection detection)
    {
        // Calculate screen-space center and bounds
        float xCenter = (detection.x1 + detection.x2) / 2f;
        float yCenter = (detection.y1 + detection.y2) / 2f;
        float width = Mathf.Abs(detection.x2 - detection.x1);
        float height = Mathf.Abs(detection.y2 - detection.y1);

        // Use depth estimate from CameraDataCollector2, refined with spatial mapping
        float estimatedDepth = RefineDepthEstimate(xCenter, yCenter, latestDepthEstimate);

        // Convert screen-space to world-space
        Vector3 screenCenter = new Vector3(xCenter, yCenter, estimatedDepth);
        Vector3 screenTopLeft = new Vector3(detection.x1, detection.y1, estimatedDepth);
        Vector3 screenBottomRight = new Vector3(detection.x2, detection.y2, estimatedDepth);

        Vector3 worldCenter = mainCamera.ScreenToWorldPoint(screenCenter);
        Vector3 worldTopLeft = mainCamera.ScreenToWorldPoint(screenTopLeft);
        Vector3 worldBottomRight = mainCamera.ScreenToWorldPoint(screenBottomRight);

        // Adjust bounding box size for real-world scale
        Vector3 worldSize = worldBottomRight - worldTopLeft;
        worldSize.z = estimatedDepth * 0.1f; // Depth proportional to distance

        // Snap to spatial surface if available
        RaycastHit hit;
        if (Physics.Raycast(worldCenter, mainCamera.transform.forward, out hit, 10f))
        {
            worldCenter = hit.point;
        }

        boundingBoxHolder.transform.position = worldCenter;
        boundingBoxHolder.SetBoundingBoxSize(new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z)));
        boundingBoxHolder.SetClassText(detection.@class);

        boundingBoxHolder.gameObject.SetActive(true);
        detectionResultsText.text += $"Bounding box updated for {detection.@class} at {worldCenter} with size {worldSize}\n";
    }

    private float RefineDepthEstimate(float xCenter, float yCenter, float initialDepth)
    {
        // Refine depth using spatial mapping at the object's center
        Vector3 screenPoint = new Vector3(xCenter, yCenter, 0);
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10f))
        {
            return hit.distance;
        }
        return initialDepth; // Fallback to initial estimate
    }

    private class CustomCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // Accept self-signed certificates (for testing)
        }
    }

    [System.Serializable]
    public class Detection
    {
        public string @class;
        public float confidence;
        public float x1;
        public float y1;
        public float x2;
        public float y2;
    }

    [System.Serializable]
    public class DetectionResults
    {
        public List<Detection> objects;
    }
}