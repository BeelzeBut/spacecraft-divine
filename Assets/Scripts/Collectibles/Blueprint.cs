using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Blueprint : Collectible
{
    public TextMeshPro textName;
    public Spaceship unlockableShip;
    Vector3 initialScale;
    private void Start()
    {
        initialScale = transform.localScale;
        transform.localScale = Vector3.zero;
        GetComponent<BoxCollider2D>().enabled = false;
    }

    private void Update()
    {
        if(transform.localScale.x < initialScale.x)
        {
            transform.localScale += Vector3.one * 2f * Time.deltaTime;
            transform.position += Vector3.up * .5f * Time.deltaTime;
        }
        else
        {
            GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        transform.localScale = Vector3.one * 1.25f;
        textName.enabled = true;
        PlayerController.instance.interactableObject = this;
        //PlayerController.instance.fireButton.gameObject.SetActive(false);
        PlayerController.instance.interactButton.gameObject.SetActive(true) ;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        transform.localScale = Vector3.one;
        textName.enabled = false;
        //PlayerController.instance.fireButton.gameObject.SetActive(true);
        PlayerController.instance.interactButton.gameObject.SetActive(false);
    }

    public override void UnlockCollectible()
    {
        DataHolder.instance.dataSaved.priceToUnlock[unlockableShip.orderNumber] = 1;
        unlockableShip.priceToUnlock = 1;
        DataHolder.instance.dataSaved.hasBeenUnlocked[dropIndex] = true;
        gameObject.SetActive(false);
    }
}
