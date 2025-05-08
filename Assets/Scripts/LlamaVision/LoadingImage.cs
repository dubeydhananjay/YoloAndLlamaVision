using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingImage : MonoBehaviour
{
    Vector3 rotationEuler = Vector3.zero;

    private void Awake()
    {
        Activation(false);
    }
    void Update()
    {
        rotationEuler -= Vector3.forward * 30 * Time.deltaTime * 5;
        transform.rotation = Quaternion.Euler(rotationEuler);

    }

    public void Activation(bool activate)
    {
        gameObject.SetActive(activate);
    }
}
