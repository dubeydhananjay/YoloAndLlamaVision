using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System.Threading;

public class ChatUI : MonoBehaviour
{
    public TextMeshProUGUI qText;
    public TextMeshProUGUI rText;
    public Image qBG;
    public Image rBG;
    private static int count;

    public void ChangeBackgroundColor()
    {
        if (count % 2 == 0)
        {
            qBG.color = Color.yellow;
            rBG.color = Color.yellow;
        }
        else
        {
            qBG.color = Color.white;
            rBG.color = Color.white;
        }
        count++;
    }
}
