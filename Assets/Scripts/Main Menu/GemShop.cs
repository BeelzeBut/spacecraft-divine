using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class GemShop : MonoBehaviour
{
    public static GemShop instance;
    public Text[] priceTexts;
    public TextMeshProUGUI gemsText;
    private void Awake()
    {
        instance = this;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        gemsText.text = DataHolder.instance.gems.ToString();
    }

}
