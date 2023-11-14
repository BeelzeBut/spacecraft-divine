using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AbilityDrop : Collectible
{
    private DataHolder data;
    private PlayerController p;

    public List<Ability> abilities = new List<Ability>();
    public Ability dropAbility;
    public TextMeshPro textName;
    private Ability oldAbility;
    public SpriteRenderer hologramSprite, abilityBg, abilitySprite;
    
    private int rarity;
    private int level;
    private int oldLevel;

    private void Start()
    {
        data = DataHolder.instance;
        p = PlayerController.instance;

        StartCoroutine(EnableCollider());
        textName.enabled = false;

        int k = Random.Range(0, abilities.Count);
        while (k <= 13 && !DataHolder.instance.dataSaved.isUnlocked[k])
            k = Random.Range(0, abilities.Count);
        dropAbility = abilities[k];

        abilitySprite.sprite = dropAbility.abilitySprite;
        rarity = Random.Range(0, 101) + 10;

        if (rarity < 5 * data.level / 1.5f)
        {
            level = 5;
        }
        else if (rarity < 15 * data.level / 1.5f)
        {
            level = 4;
        }
        else if (rarity < 30 * data.level / 1.5f)
        {
            level = 3;
        }
        else if (rarity < 55 * data.level / 1.5f)
        {
            level = 2;
        }
        else
        {
            level = 1;
        }

        //if (dropAbility.abilityName == p.ship.signatureAbility.abilityName)
        //level++;

        SetColor();
    }

    public override void UnlockCollectible()
    {
        
        if (p.ability)
        {
            oldAbility = p.ability;
            oldLevel = p.ability.level;
        }

        p.ship.ability = dropAbility;
        p.ability = p.ship.ability;
        p.ship.ability.level = level;
        p.abilityManager.Initialize(p.ability);

        dropAbility = oldAbility;
        if (dropAbility)
            abilitySprite.sprite = dropAbility.abilitySprite;
        else
        {
            GetComponent<Animator>().SetTrigger("close");
            textName.enabled = false;
            GetComponent<Collider2D>().enabled = false;
            return;
        }

        level = oldLevel;
        SetColor();         
    }
    IEnumerator EnableCollider()
    {
        yield return new WaitForSeconds(1.25f);
        GetComponent<Collider2D>().enabled = true;
    }

    public void SetColor()
    { 
        switch (level)
        {
            case 1:
                hologramSprite.color = Color.white;
                abilityBg.color = Color.white;
                textName.color = Color.white;
                break;
            case 2:
                hologramSprite.color = Color.green;
                abilityBg.color = Color.green;
                textName.color = Color.green;

                break;
            case 3:
                hologramSprite.color = new Color(0, .5f, 1);
                abilityBg.color = new Color(0, .5f, 1);
                textName.color = new Color(0, .5f, 1);

                break;
            case 4:
                hologramSprite.color = new Color(.75f, 0, 1);
                abilityBg.color = new Color(.75f, 0, 1);
                textName.color = new Color(.75f, 0, 1);

                break;
            case 5:
                hologramSprite.color = new Color(1, .5f, 0);
                abilityBg.color = new Color(1, .5f, 0);
                textName.color = new Color(1, .5f, 0);

                break;
            case 6:
                hologramSprite.color = Color.red;
                abilityBg.color = Color.red;
                textName.color = Color.red;
                break;
        }
        abilitySprite.sprite = dropAbility.abilitySprite;
        textName.text = dropAbility.abilityName;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        textName.enabled = true;
        PlayerController.instance.interactableObject = this;
        //PlayerController.instance.fireButton.gameObject.SetActive(false);
        PlayerController.instance.interactButton.gameObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        textName.enabled = false;
        //PlayerController.instance.fireButton.gameObject.SetActive(true);
        PlayerController.instance.interactButton.gameObject.SetActive(false);
        PlayerController.instance.interactableObject = null;
    }
}
