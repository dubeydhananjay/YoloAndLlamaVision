using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using System.Text;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit;
using System.Diagnostics;

public class YoloDetection1 : MonoBehaviour, IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessMeshObject>
{
    [Header("Server and Detection Settings")]
    private string serverUrl = "https://9782-128-235-248-150.ngrok-free.app/process?type=image";
    public Camera mainCamera;
    public CameraDataCollector2 cameraDataCollector;

    [Header("UI and Feedback")]
    public TextMeshProUGUI detectionResultsText;

    [Header("Bounding Box Management")]
    public BoundingBoxHolder boundingBoxHolderPrefab;
    public Transform boxParent;
    private List<BoundingBoxHolder> boundingBoxHolders = new List<BoundingBoxHolder>();
    private const int MaxBoundingBoxes = 5;

    [Header("Spatial Mapping")]
    public LayerMask spatialMappingLayer;
    private IMixedRealitySpatialAwarenessMeshObserver spatialMeshObserver;

    private Texture2D inputImage;
    private Stopwatch stopwatch;
    public TextMeshProUGUI yoloTime;

    void Start()
    {
        stopwatch = new Stopwatch();
        InitializeBoundingBoxPool();
        InitializeCameraDataCollector();
        InitializeSpatialMapping();
        StartCoroutine(StartDetection());
    }

    private void InitializeBoundingBoxPool()
    {
        for (int i = 0; i < MaxBoundingBoxes; i++)
        {
            BoundingBoxHolder boundingBoxHolder = Instantiate(boundingBoxHolderPrefab, boxParent);
            boundingBoxHolder.gameObject.SetActive(false);
            boundingBoxHolders.Add(boundingBoxHolder);
        }
    }

    private void InitializeCameraDataCollector()
    {
        if (cameraDataCollector != null)
        {
            cameraDataCollector.OnImageCaptured += OnImageCaptured;
        }
    }

    private void InitializeSpatialMapping()
    {
        var spatialAwarenessSystem = CoreServices.SpatialAwarenessSystem as IMixedRealityDataProviderAccess;
        if (spatialAwarenessSystem != null)
        {
            spatialMeshObserver = spatialAwarenessSystem.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();
            if (spatialMeshObserver != null)
            {
                spatialMeshObserver.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;
                spatialMeshObserver.LevelOfDetail = SpatialAwarenessMeshLevelOfDetail.Coarse;

                var eventSystem = CoreServices.InputSystem as IMixedRealityEventSystem;
                eventSystem?.RegisterHandler<IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessMeshObject>>(this);
            }
        }
    }

    private IEnumerator StartDetection()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Trigger image capture at regular intervals
            if (cameraDataCollector != null)
            {
                cameraDataCollector.StartCapturing();
            }
        }
    }

    private void OnImageCaptured(Texture2D capturedImage)
    {
        inputImage = capturedImage;
        StartCoroutine(UploadImageAsync());
    }

    private IEnumerator UploadImageAsync()
    {
        if (inputImage == null) yield break;

        byte[] imageData = inputImage.EncodeToJPG(50); // Compress image with quality 50
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageData, "image.jpg", "image/jpeg");
        stopwatch.Restart();
        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            www.certificateHandler = new CustomCertificateHandler();
            www.disposeCertificateHandlerOnDispose = false;
            yield return www.SendWebRequest();
            stopwatch.Stop();
            yoloTime.text = $"YOLO Detection - Request and Response Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds";
            if (www.result == UnityWebRequest.Result.Success)
            {
                ProcessDetectionResults(www.downloadHandler.text);
            }
            else
            {
                UnityEngine.Debug.LogError($"Error communicating with the server. {www.error}");
            }
        }
    }

    private void ProcessDetectionResults(string jsonResponse)
    {
        try
        {
            DetectionResults detections = JsonConvert.DeserializeObject<DetectionResults>(jsonResponse);
            StringBuilder textBuilder = new StringBuilder("Detected Objects:\n");

            int n = Mathf.Min(detections.objects.Count, MaxBoundingBoxes);

            for (int i = 0; i < n; i++)
            {
                textBuilder.AppendLine(detections.objects[i].@class);
                boundingBoxHolders[i].gameObject.SetActive(true);
                UpdateBoundingBox(boundingBoxHolders[i], detections.objects[i]);
            }

            for (int i = n; i < MaxBoundingBoxes; i++)
            {
                boundingBoxHolders[i].gameObject.SetActive(false);
            }

            detectionResultsText.text = textBuilder.ToString();
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error parsing JSON response: {ex.Message}");
            detectionResultsText.text = "Error parsing server response.";
        }
    }

    private void UpdateBoundingBox(BoundingBoxHolder boundingBoxHolder, Detection detection)
    {
        float xCenter = (detection.x1 + detection.x2) / 2;
        float yCenter = (detection.y1 + detection.y2) / 2;

        // Perform raycasting to spatial mesh to get accurate depth
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(xCenter, yCenter, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, spatialMappingLayer))
        {
            Vector3 worldCenter = hit.point;
            worldCenter.y /= 2;

            Vector3 screenTopLeft = new Vector3(detection.x1, detection.y1, hit.distance);
            Vector3 screenBottomRight = new Vector3(detection.x2, detection.y2, hit.distance);

            Vector3 worldTopLeft = mainCamera.ScreenToWorldPoint(screenTopLeft);
            Vector3 worldBottomRight = mainCamera.ScreenToWorldPoint(screenBottomRight);

            Vector3 worldSize = worldBottomRight - worldTopLeft;

            boundingBoxHolder.transform.position = worldCenter;
            boundingBoxHolder.SetBoundingBoxSize(new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), 0.2f));
            boundingBoxHolder.SetClassText(detection.@class);
        }
    }

    public void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData) { }
    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData) { }
    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData) { }

    private class CustomCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
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