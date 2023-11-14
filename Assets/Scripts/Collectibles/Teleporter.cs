using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : Collectible
{
    public override void UnlockCollectible()
    {
        if (DataHolder.instance.dataSaved.hasCompletedTutorial == false)
        {
            DataHolder.instance.hasCompletedTutorial = true;
            DataHolder.instance.enableSaving = true;
            DataHolder.instance.gameHasEnded = true;
            DataHolder.instance.Save();
        }
        
            StartCoroutine(PlayerController.instance.GoToNextLevel());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController.instance.interactableObject = this;
            PlayerController.instance.interactButton.gameObject.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController.instance.interactableObject = null;
        PlayerController.instance.interactButton.gameObject.SetActive(false);
    }
}
