using UnityEngine;
using TMPro;

public class BoundingBoxHolder : MonoBehaviour
{
    public GameObject boundingBox;
    public TextMeshPro text;

    public void SetBoundingBoxSize(Vector3 size)
    {
        boundingBox.transform.localScale = size;
    }

    public void SetClassText(string className)
    {
        text.text = className;
    }
}
