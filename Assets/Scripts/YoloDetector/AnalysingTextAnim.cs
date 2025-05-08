using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AnalysingTextAnim : MonoBehaviour
{
    private TextMeshPro _textMeshPro;
    private void Awake()
    {
        _textMeshPro = GetComponent<TextMeshPro>();
        StartCoroutine(Anim());
    }
    private IEnumerator Anim()
    {
        while (true)
        {
            _textMeshPro.text = "Analysing.";
            yield return new WaitForSeconds(0.5f);
            _textMeshPro.text = "Analysing..";
            yield return new WaitForSeconds(0.5f);
            _textMeshPro.text = "Analysing...";
            yield return new WaitForSeconds(0.5f);
        }
    }
}
