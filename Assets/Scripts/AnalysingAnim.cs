using Unity.VisualScripting;
using UnityEngine;

public class AnalysingAnim : MonoBehaviour
{
    public Transform centerPoint; // The "Analyzing" text or center of the circle
    public float radius = 1f;     // Radius of the circular path
    public float speed = 2f;      // Speed of rotation (radians per second)
    private float angle = 0f;     // Current angle in radians

    Vector3 scale = new Vector3(-0.3f, 0.3f, 0.3f);

    void Update()
    {
        float x = centerPoint.position.x + Mathf.Cos(angle) * radius;
        float z = centerPoint.position.z;
        float y = centerPoint.position.y + Mathf.Sin(angle) * radius;

        if (x <= -2) scale.x = 0.3f;
        else if(x >= 2) scale.x = -0.3f;
        transform.localScale = scale;

        transform.position = new Vector3(x, y, z);
        angle += speed * Time.deltaTime;
    }
}