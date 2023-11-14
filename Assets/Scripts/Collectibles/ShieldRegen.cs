using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShieldRegen : Collectible
{
    public float shieldAmount;
    public float chanceToBeMedium;
    public float chanceToBeLarge;
    public TextMeshPro shieldAmountText, textName;
    Vector3 initialScale;
    void Start()
    {
        int x = Random.Range(0, 101);
        if (x <= chanceToBeLarge)
        {
            shieldAmount *= 3;
        }
        else if (x <= chanceToBeMedium)
        {
            shieldAmount *= 2;
        }
        shieldAmountText.text = (shieldAmount / PlayerController.instance.maxHealth * 100).ToString("0") + "%";
        initialScale = transform.localScale;
        transform.localScale = Vector3.zero;
        GetComponent<BoxCollider2D>().enabled = false;
    }

    void Update()
    {
        if (transform.localScale.x < initialScale.x)
        {
            transform.localScale += Vector3.one * 3.5f * Time.deltaTime;
            transform.position += Vector3.up * .5f * Time.deltaTime;
        }
        else
        {
            GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            transform.localScale = Vector3.one * 1.6f;
            textName.enabled = true;
            PlayerController.instance.interactableObject = this;
            PlayerController.instance.interactButton.gameObject.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        transform.localScale = Vector3.one * 1.3f;
        textName.enabled = false;
        PlayerController.instance.interactButton.gameObject.SetActive(false);
    }
    public override void UnlockCollectible()
    {
        PlayerController.instance.health += shieldAmount;
        PlayerController.instance.minHealth += shieldAmount;
        PlayerController.instance.health = Mathf.Clamp(PlayerController.instance.health, 0, PlayerController.instance.maxHealth);
        PlayerController.instance.healthSlider.value = .125f + PlayerController.instance.health / PlayerController.instance.maxHealth * .875f;
        PlayerController.instance.healthText.text = (100f * PlayerController.instance.health / PlayerController.instance.maxHealth).ToString("0");
        PlayerController.instance.ShieldRegen();
        Destroy(gameObject);
    }
}
