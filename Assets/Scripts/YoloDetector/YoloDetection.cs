using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit;
using Newtonsoft.Json;
using System.Net;
using static System.Net.WebRequestMethods;

public class YoloDetection : MonoBehaviour, IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessMeshObject>
{
    public string serverUrl = "https://6f5a-128-235-248-168.ngrok-free.app/process?type=image";
    public TextMeshProUGUI detectionResultsText;
    public BoundingBoxHolder boundingBoxHolderPrefab;
    public Transform boxParent;
    public Camera mainCamera;

    private Texture2D inputImage;
    public CameraDataCollector2 cameraDataCollector;
    private List<BoundingBoxHolder> boundingBoxHolders = new List<BoundingBoxHolder>();
    private const int MaxBoundingBoxes = 10;
    public DetectionGridPanel detectionGridPanel;
    public GameObject AnalysingGroup;

    void Start()
    {
        //InitializeSpatialAwareness();
       

        // Instantiate 5 bounding boxes at the start
        /* for (int i = 0; i < MaxBoundingBoxes; i++)
         {
             BoundingBoxHolder boundingBoxHolder = Instantiate(boundingBoxHolderPrefab, boxParent);
             boundingBoxHolder.gameObject.SetActive(false);
             boundingBoxHolders.Add(boundingBoxHolder);
         }*/
        StartCoroutine(Wait());
    }

    private IEnumerator Wait()
    {
        yield return null;
        StartDetection();
    }

    void OnEnable()
    {
        if (cameraDataCollector != null)
        {
            // detectionResultsText.text = "cameraDataCollector.OnImageCaptured += OnImageCaptured";
            cameraDataCollector.OnImageCaptured += OnImageCaptured;
        }

    }

    void OnDisable()
    {
        cameraDataCollector.OnImageCaptured -= OnImageCaptured;
    }

    private void InitializeSpatialAwareness()
    {
        var spatialAwarenessSystem = CoreServices.SpatialAwarenessSystem as IMixedRealityDataProviderAccess;
        if (spatialAwarenessSystem != null)
        {
            var eventSystem = CoreServices.InputSystem as IMixedRealityEventSystem;
            if (eventSystem != null)
            {
                eventSystem.RegisterHandler<IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessMeshObject>>(this);
            }

            var meshObservers = spatialAwarenessSystem.GetDataProviders<IMixedRealitySpatialAwarenessMeshObserver>();
            foreach (var observer in meshObservers)
            {
                observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
                observer.LevelOfDetail = SpatialAwarenessMeshLevelOfDetail.Coarse;
            } 
        }
    }

    public void StartDetection()
    {
        if (cameraDataCollector != null)
        {
            //detectionResultsText.text += "\nStartDetection\n";
            cameraDataCollector.StartCapturing();
        }
    }

    private void OnImageCaptured(Texture2D capturedImage)
    {
        inputImage = capturedImage;
        StartCoroutine(UploadImageAsync());
    }

    private IEnumerator UploadImageAsync()
    {

        if (inputImage == null)
        {
            detectionResultsText.text += "No image captured.";
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
            www.disposeCertificateHandlerOnDispose = true;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                detectionResultsText.text = "Error sending image to server: " + www.error;
            }
            else if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                yield return new WaitForSeconds(2);
                //detectionResultsText.text = $"{jsonResponse}";
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
            AnalysingGroup.SetActive(false);
            DetectionResults detections = JsonConvert.DeserializeObject<DetectionResults>(jsonResponse);
            detectionResultsText.text += "Detected Objects: \n";
            foreach (var detection in detections.objects)
            {
                detectionResultsText.text += $"{detection.@class}\n";
            }
            //detectionResultsText.text = "detectionGridPanel.gameObject.SetActive(true)\n";
            detectionGridPanel.gameObject.SetActive(true);
            if (detectionGridPanel != null)
            {
                //detectionResultsText.text = "Display Detection called";
                detectionGridPanel.DisplayDetections(detections.objects.ToArray());
            }
            int n = Mathf.Min(detections.objects.Count, MaxBoundingBoxes);

            //for (int i = 0; i < n; i++)
            //{
                //boundingBoxHolders[i].gameObject.SetActive(true);
                //UpdateBoundingBox(boundingBoxHolders[i], detections.objects[i]);
            //}
            //cameraDataCollector.StartCapturing();  // Uncomment to capture image again after processing
            cameraDataCollector.StopCamera();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error parsing JSON response: " + ex.Message);
            detectionResultsText.text = "Error parsing JSON response: " + ex.Message;
        }
    }

    private void UpdateBoundingBox(BoundingBoxHolder boundingBoxHolder, Detection detection)
    {
        float xCenter = (detection.x1 + detection.x2) / 2;
        float yCenter = (detection.y1 + detection.y2) / 2;

        float estimatedDepth = GetEstimatedDepth();

        Vector3 screenCenter = new Vector3(xCenter, yCenter, estimatedDepth);
        Vector3 screenTopLeft = new Vector3(detection.x1, detection.y1, estimatedDepth);
        Vector3 screenBottomRight = new Vector3(detection.x2, detection.y2, estimatedDepth);

        Vector3 worldCenter = mainCamera.ScreenToWorldPoint(screenCenter);
        Vector3 worldTopLeft = mainCamera.ScreenToWorldPoint(screenTopLeft);
        Vector3 worldBottomRight = mainCamera.ScreenToWorldPoint(screenBottomRight);

        Vector3 worldSize = worldBottomRight - worldTopLeft;
        worldSize.z = 0.2f;

        boundingBoxHolder.transform.position = worldCenter;
        boundingBoxHolder.SetBoundingBoxSize(new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z)));
        boundingBoxHolder.SetClassText(detection.@class);

        boundingBoxHolder.gameObject.SetActive(true);
        detectionResultsText.text += $"Bounding box updated for {detection.@class} at {worldCenter} with size {worldSize}\n";
    }


    private float GetEstimatedDepth()
    {
        return 2.0f;
    }

    private class CustomCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData) { }
    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData) { }
    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData) { }

    [System.Serializable]
    public class Detection
    {
        public string @class;
        public float confidence;
        public float x1;
        public float y1;
        public float x2;
        public float y2;
        public string cropped_image;
    }

    [System.Serializable]
    public class DetectionResults
    {
        public List<Detection> objects;
    }
}