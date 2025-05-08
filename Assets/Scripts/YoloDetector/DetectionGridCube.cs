using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetectionGridCube : MonoBehaviour
{
    public TMPro.TextMeshPro m_TextMeshPro;
    public RawImage m_RawImage;
    public void SetText(string text)
    {
        m_TextMeshPro.text = text;
    }

    public void SetImage(string cropped_image)
    {
        if (m_RawImage != null && !string.IsNullOrEmpty(cropped_image))
        {
            byte[] imageBytes = System.Convert.FromBase64String(cropped_image);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageBytes))
            {
                m_RawImage.texture = texture;
            }
            else
            {
                Debug.LogWarning("Failed to load cropped image for " + m_TextMeshPro.text);
            }
        }
    }

    public void GoToScene()
    {
        Debug.Log("GoToScene Clicked");

        PlayerPrefs.SetString(Constants.DETECTED_OBJECT_STRING, m_TextMeshPro.text);
        PlayerPrefs.Save();
        StartCoroutine(WaitToGoToScene());
    }

    private IEnumerator WaitToGoToScene()
    {
        yield return null;
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleSceneNew");

    }
}
