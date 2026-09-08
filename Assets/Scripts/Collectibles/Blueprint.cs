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

    // NOTE: priceToUnlock is overloaded here as a boolean. Setting it to 1 does not mean the
    // ship costs 1 gem — it means "blueprint collected, this ship is now claimable for free
    // in the menu", which MainMenu.UnlockShip() reads in its canBeUnlockedInGame branch.
    // PlayerProfile.MarkBlueprintRedeemable() replaces this once DataHolder is cut over to
    // SaveService; until then the two must agree, so do not change one without the other.
    public override void UnlockCollectible()
    {
        DataHolder.instance.dataSaved.priceToUnlock[unlockableShip.orderNumber] = 1;
        unlockableShip.priceToUnlock = 1;
        DataHolder.instance.dataSaved.hasBeenUnlocked[dropIndex] = true;
        gameObject.SetActive(false);
    }
}
