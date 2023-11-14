using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UpgradeDrop : Collectible
{
    PlayerController p;
    public Upgrade[] possibleUpgrades;
    public Upgrade upgrade;
    public TextMeshPro textName;
    public Sprite[] upgradeTypeSprites;
    public SpriteRenderer upgradeType;
    private Animator anim;
    private int index;

    void Start()
    {
        p = PlayerController.instance;
        anim = GetComponent<Animator>();
        index = Random.Range(0, possibleUpgrades.Length);
        while(p.ship.playerUpgrades.Contains(possibleUpgrades[index]))
            index = Random.Range(0, possibleUpgrades.Length);

        upgrade = possibleUpgrades[index];

        textName.text = upgrade.description;
        if (upgrade.isAttack)
            upgradeType.sprite = upgradeTypeSprites[0];
        else if (upgrade.isDefense)
            upgradeType.sprite = upgradeTypeSprites[1];
        else upgradeType.sprite = upgradeTypeSprites[2];
        StartCoroutine(ShowUpgrade());
    }

    IEnumerator ShowUpgrade()
    {
        yield return new WaitForSeconds(.55f);
        anim.SetTrigger("open");
    }

    public override void UnlockCollectible()
    {
        p.interactableObject = null;
        p.interactButton.gameObject.SetActive(false);
        upgrade.UpgradeShip();
        p.ship.playerUpgrades.Add(upgrade);
        p.ship.upgradesIndex.Add(index);
        UIManager.instance.UpdateUpgrades();

        upgradeType.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        anim.SetTrigger("close");
        UpgradeDrop[] drops = FindObjectsOfType<UpgradeDrop>();
        for (int i = 0; i < drops.Length; i++)
        {
            if ((drops[i].transform.position - transform.position).sqrMagnitude <= 16)
            {
                drops[i].anim.SetTrigger("close");
                drops[i].GetComponent<Collider2D>().enabled = false;
            }
        }
        if (GameManager.instance.subLevel == 5)
        {
            Physics2D.OverlapCircle(transform.position, 1, LayerMask.GetMask("Ignore Raycast")).GetComponentInParent<Room>().SpawnTeleporter();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        p.interactableObject = this;
        p.interactButton.gameObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        p.interactableObject = null;
        p.interactButton.gameObject.SetActive(false);
    }
}
