using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Microsoft.MixedReality.Toolkit.Audio;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Windows.Speech;
using Microsoft.MixedReality.Toolkit;
using System.Diagnostics;

public class LlamaVisionSpeechRecogniser : MonoBehaviour, IMixedRealitySpeechHandler
{
    public TextToSpeech textToSpeech;
    public CameraDataCollector2 cameraDataCollector;
    public LoadingImage loadingImage;
    public TextMeshProUGUI res;

    private string serverUrl = "http://192.168.0.192:5001/generate_description";
    private DictationRecognizer dictationRecognizer;
    private string recognizedText;
    private bool isDictationActive = false;
    private Stopwatch stopwatch;
    private bool isSpeaking = false; // Prevent multiple TTS processes

    public TextMeshProUGUI llamaTime;

    private void Start()
    {
        stopwatch = new Stopwatch();
    }

    private void InitializeDictationRecognizer()
    {
        if (dictationRecognizer == null)
            dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += OnDictationResult;
        dictationRecognizer.DictationComplete += OnDictationComplete;
        dictationRecognizer.DictationError += OnDictationError;
        
    }

    private void Update()
    {
        if(!isSpeaking) return;
        if (textToSpeech.IsSpeaking()) return;
        OnTextToSpeechComplete();
    }

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        switch (eventData.Command.Keyword.ToLower())
        {
            case "initiate":
                llamaTime.text = "Initiate command!!!!";
                InitializeDictationRecognizer();
                StartDictation();
                break;

            case "stop":
                llamaTime.text = "Stop command!!!!";
                StopTextToSpeech();
                break;

            default:
                break;
        }
    }

    private void StartDictation()
    {
        if (isDictationActive) return;
        isSpeaking = false;
        if (PhraseRecognitionSystem.isSupported && PhraseRecognitionSystem.Status == SpeechSystemStatus.Running)
        {
            PhraseRecognitionSystem.Shutdown();
        }

        isDictationActive = true;
        cameraDataCollector.StartCamera();
        ChatUIManager.Instance.currentChatUI.qText?.SetText("Provide a prompt using speech to analyse image in front of you.");
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

        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        cameraDataCollector.OnImageCaptured -= SendPromptToServer;
        StartCoroutine(RestartPhraseRecognitionSystem());
    }

    private void OnDictationComplete(DictationCompletionCause completionCause)
    {
        StopDictation();
    }

    private void OnDictationError(string error, int hResult)
    {
        StopDictation();
    }

    private void OnDictationResult(string text, ConfidenceLevel confidenceLevel)
    {
        recognizedText = text;
        ChatUIManager.Instance.currentChatUI.qText?.SetText($"Recognized: {recognizedText}");
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
        loadingImage?.Activation(true);
        WWWForm form = new WWWForm();
        form.AddField("prompt", prompt);
        form.AddBinaryData("image", imageBytes, "image.jpg", "image/jpeg");
        stopwatch.Restart();

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            www.certificateHandler = new CustomCertificateHandler();
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
        //OnTextToSpeechComplete();
    }

    private void OnTextToSpeechComplete()
    {
        isSpeaking = false;
        ChatUIManager.Instance.CreateNewSlide();
        ChatUIManager.Instance.ShowNextSlide();
        ChatUIManager.Instance.currentChatUI.qText?.SetText("Speak 'Initiate' to start analysing the image!!");
    }

    private void OnDestroy()
    {
        dictationRecognizer?.Dispose();
    }

    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private class CustomCertificateHandler : CertificateHandler
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
