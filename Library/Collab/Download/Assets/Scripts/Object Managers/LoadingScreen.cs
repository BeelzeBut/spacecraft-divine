using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public List<string> tips = new List<string>();
    public TextMeshProUGUI tipText;
    private void OnEnable()
    {
        tipText.text = tips[Random.Range(0, tips.Count)];
    }
}
