using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GemsTextUpdate : MonoBehaviour
{
    public TextMeshProUGUI gemsText;

    private void Update()
    {
        gemsText.text = DataHolder.instance.gems.ToString();
    }
}
