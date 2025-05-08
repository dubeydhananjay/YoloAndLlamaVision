using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Microsoft.MixedReality.Toolkit.Audio;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Windows.Speech;
using Microsoft.MixedReality.Toolkit;
using System.Diagnostics;
using System.Net;

public class LlamaVisionSpeechRecogniserNew : MonoBehaviour, IMixedRealitySpeechHandler
{
    public TextToSpeech textToSpeech;
    public CameraDataCollector2 cameraDataCollector;
    public LoadingImage loadingImage;
    public TextMeshProUGUI llamaTime;

    public string serverUrl = "https://6f5a-128-235-248-168.ngrok-free.app/generate_description";
    private DictationRecognizer dictationRecognizer;
    private string recognizedText;
    private bool isDictationActive = false;
    private Stopwatch stopwatch;
    private bool isSpeaking = false; // Prevent multiple TTS processes
    private string currentObjectName; // Track the current object for context

    private void Start()
    {
        stopwatch = new Stopwatch();
        llamaTime.text = PlayerPrefs.GetString(Constants.DETECTED_OBJECT_STRING);

        // Check for object name in PlayerPrefs
        if (PlayerPrefs.HasKey(Constants.DETECTED_OBJECT_STRING))
        {
            currentObjectName = PlayerPrefs.GetString(Constants.DETECTED_OBJECT_STRING);
            recognizedText = $"Describe {currentObjectName}";
            ChatUIManager.Instance.currentChatUI.qText?.SetText($"Describe: {currentObjectName}");
            cameraDataCollector.OnImageCaptured += SendPromptToServer;
            cameraDataCollector.StartCamera();
            cameraDataCollector.StartCapturing();
        }
        else
        {
            ChatUIManager.Instance.currentChatUI.qText?.SetText("No object selected. Please select an object.");
        }
    }

    private void InitializeDictationRecognizer()
    {
        if (dictationRecognizer == null)
            dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += OnDictationResult;
        dictationRecognizer.DictationComplete += OnDictationComplete;
        dictationRecognizer.DictationError += OnDictationError;
    }

   /* private void Update()
    {
        if (!isSpeaking) return;
        if (textToSpeech.IsSpeaking()) return;
        OnTextToSpeechComplete();
    }*/

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        switch (eventData.Command.Keyword.ToLower())
        {
            case "start":
                llamaTime.text = "Start command detected!";
                InitializeDictationRecognizer();
                StartDictation();
                break;

            case "next":
                llamaTime.text = "Next command detected!";
                StopTextToSpeech();
                break;

            default:
                break;
        }
    }

    private void StartDictation()
    {
        if (isDictationActive || string.IsNullOrEmpty(currentObjectName)) return;
        isSpeaking = false;
        if (PhraseRecognitionSystem.isSupported && PhraseRecognitionSystem.Status == SpeechSystemStatus.Running)
        {
            PhraseRecognitionSystem.Shutdown();
        }

        isDictationActive = true;
        cameraDataCollector.StartCamera();
        ChatUIManager.Instance.currentChatUI.qText?.SetText($"Ask a question about {currentObjectName}.");
        dictationRecognizer.Start();
    }

    private void StopDictation()
    {
        if (!isDictationActive) return;

        dictationRecognizer.DictationResult -= OnDictationResult;
        dictationRecognizer.DictationComplete -= OnDictationComplete;
        dictationRecognizer.DictationError -= OnDictationError;

        isDictationActive = false;
        cameraDataCollector.StopCamera();
        cameraDataCollector.OnImageCaptured -= SendPromptToServer;

        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
            llamaTime.text += "\nDictation stopped.";
        }

        StartCoroutine(RestartPhraseRecognitionSystem());
    }

    private void OnDictationComplete(DictationCompletionCause completionCause)
    {
        //StopDictation();
    }

    private void OnDictationError(string error, int hResult)
    {
        ChatUIManager.Instance.currentChatUI.rText?.SetText($"Dictation Error: {error}");
        StopDictation();
    }

    private void OnDictationResult(string text, ConfidenceLevel confidenceLevel)
    {
        recognizedText = text;
        ChatUIManager.Instance.currentChatUI.qText?.SetText($"Query about {currentObjectName}: {recognizedText}\nSay 'Next' to start new chat!!");
        recognizedText = $"Tell me more about {currentObjectName}: {recognizedText}";
        cameraDataCollector.OnImageCaptured += SendPromptToServer;
        cameraDataCollector.StartCapturing();
    }

    private IEnumerator RestartPhraseRecognitionSystem()
    {
        yield return new WaitForSeconds(0.5f);

        if (PhraseRecognitionSystem.isSupported && PhraseRecognitionSystem.Status != SpeechSystemStatus.Running)
        {
            PhraseRecognitionSystem.Restart();
        }
    }

    private void StopTextToSpeech()
    {
        if (textToSpeech.IsSpeaking())
        {
            textToSpeech.StopSpeaking();
            OnTextToSpeechComplete();
        }
    }

    private void SendPromptToServer(Texture2D capturedImage)
    {
        byte[] imageBytes = capturedImage.EncodeToJPG();
        StartCoroutine(SendImageToServer(recognizedText, imageBytes));
    }

    private IEnumerator SendImageToServer(string prompt, byte[] imageBytes)
    {
        llamaTime.text = "\nSendImageToServer";
        loadingImage?.Activation(true);
        WWWForm form = new WWWForm();
        form.AddField("prompt", prompt);
        form.AddBinaryData("image", imageBytes, "image.jpg", "image/jpeg");
        stopwatch.Restart();

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            www.certificateHandler = new CustomCertificateHandler1();
            www.disposeCertificateHandlerOnDispose = true;
            yield return www.SendWebRequest();
            stopwatch.Stop();
            //llamaTime.text = $"Llama Vision - Request and Response Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds";

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                ChatUIManager.Instance.currentChatUI.rText?.SetText($"Error: {www.error}");
            }
            else
            {
                var response = JsonUtility.FromJson<DescriptionResponse>(www.downloadHandler.text);
                ChatUIManager.Instance.currentChatUI.rText?.SetText(response.description);
                StartCoroutine(MonitorTextToSpeech(response.description));
            }
        }

        loadingImage?.Activation(false);
        StopDictation();
    }

    private IEnumerator MonitorTextToSpeech(string responseDescription)
    {
        textToSpeech.StartSpeaking(responseDescription);
        yield return new WaitForSeconds(2f);
        isSpeaking = true;
    }

    private void OnTextToSpeechComplete()
    {
        isSpeaking = false;
        ChatUIManager.Instance.CreateNewSlide();
        ChatUIManager.Instance.ShowNextSlide();
        ChatUIManager.Instance.currentChatUI.qText?.SetText($"Say 'Start' to ask another question about {currentObjectName}");
    }

    private void OnDestroy()
    {
        dictationRecognizer?.Dispose();
        cameraDataCollector?.StopCamera();
        cameraDataCollector.OnImageCaptured -= SendPromptToServer;
    }

    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private class CustomCertificateHandler1 : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // Force accept any certificate
        }
    }

    [System.Serializable]
    public class DescriptionResponse
    {
        public string description;
    }
}