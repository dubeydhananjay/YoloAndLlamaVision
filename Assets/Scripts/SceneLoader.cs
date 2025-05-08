
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public LoadingImage loadingImage;
   
    public void LoadNewScene(string sceneName)
    {
        StartCoroutine(WaitLoadScene(sceneName));
    }

    private IEnumerator WaitLoadScene(string sceneName)
    {
        loadingImage?.Activation(true);
        yield return new WaitForSeconds(1);
        loadingImage?.Activation(false);
        SceneManager.LoadScene(sceneName);

    }
}
