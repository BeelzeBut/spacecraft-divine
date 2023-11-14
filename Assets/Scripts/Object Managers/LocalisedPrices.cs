using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class LocalisedPrices : MonoBehaviour
{
    public Text price;
    public IStoreController controller;
    void Start()
    {
        price = GetComponentInChildren<Text>();
        foreach (var product in controller.products.all)
            price.text = product.metadata.localizedPriceString;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
