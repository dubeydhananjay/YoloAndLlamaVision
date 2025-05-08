using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Audio;

public class ChatUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject slidePrefab; // Prefab containing two text areas (Question and Answer)
    public ChatUI currentChatUI;

    [Header("Animation Settings")]
    public float slideTransitionDuration = 0.5f; // Duration for slide transitions
    public Vector2 offScreenPosition = new Vector2(0, -1000); // Off-screen position for slides

    private List<GameObject> slides = new List<GameObject>(); // List to hold all slides
    private int currentSlideIndex = -1; // Tracks the current slide index
    private bool isSpeaking = false; // Tracks if TTS is currently active
    
    public static ChatUIManager Instance;

    private void Awake()
    {
        Instance = this;
        CreateNewSlide();
    }

    private void Start()
    {
        //CreateNewSlide();
    }

    /// <summary>
    /// Creates a new slide with empty text areas for question and response.
    /// </summary>
    public void CreateNewSlide()
    {
        GameObject newSlide = Instantiate(slidePrefab, transform);
        newSlide.SetActive(false);
        slides.Add(newSlide);
        currentChatUI = newSlide.GetComponent<ChatUI>();
        currentChatUI.ChangeBackgroundColor();

        if (currentSlideIndex == -1) // If no slide exists, show the first slide
        {
            currentSlideIndex = 0;
            newSlide.SetActive(true);
        }
    }

    /// <summary>
    /// Shows the previous slide with animation.
    /// </summary>
    public void ShowPreviousSlide()
    {
        if (currentSlideIndex > 0)
        {
            AnimateSlideOut(slides[currentSlideIndex]);
            currentSlideIndex--;
            AnimateSlideIn(slides[currentSlideIndex], true);
        }
    }

    /// <summary>
    /// Shows the next slide with animation.
    /// </summary>
    public void ShowNextSlide()
    {
        if (currentSlideIndex < slides.Count - 1)
        {
            AnimateSlideOut(slides[currentSlideIndex]);
            currentSlideIndex++;
            AnimateSlideIn(slides[currentSlideIndex], false);
        }
    }

    /// <summary>
    /// Animates the slide to move in.
    /// </summary>
    private void AnimateSlideIn(GameObject slide, bool fromAbove)
    {
        slide.SetActive(true);
        RectTransform rect = slide.GetComponent<RectTransform>();
        rect.anchoredPosition = fromAbove ? new Vector2(0, 1000) : offScreenPosition;
        StartCoroutine(SmoothSlide(rect, Vector2.zero));
    }

    /// <summary>
    /// Animates the slide to move out.
    /// </summary>
    private void AnimateSlideOut(GameObject slide)
    {
        RectTransform rect = slide.GetComponent<RectTransform>();
        Vector2 targetPosition = offScreenPosition;
        StartCoroutine(SmoothSlide(rect, targetPosition, () => slide.SetActive(false)));
    }

    /// <summary>
    /// Smoothly moves a slide to the target position.
    /// </summary>
    private IEnumerator SmoothSlide(RectTransform rect, Vector2 targetPosition, System.Action onComplete = null)
    {
        Vector2 startPosition = rect.anchoredPosition;
        float elapsedTime = 0;

        while (elapsedTime < slideTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / slideTransitionDuration);
            yield return null;
        }
        rect.anchoredPosition = targetPosition;
        onComplete?.Invoke();
    }
}
