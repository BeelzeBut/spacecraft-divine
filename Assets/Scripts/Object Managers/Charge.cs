using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Charge : MonoBehaviour
{
    public static Charge instance;
    public List<GameObject> circles = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        //transform.position = PlayerController.instance.transform.position;
        transform.position = Camera.main.WorldToScreenPoint(PlayerController.instance.transform.position);
    }
}
