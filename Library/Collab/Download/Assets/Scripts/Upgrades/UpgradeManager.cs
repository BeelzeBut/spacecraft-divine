using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;
    PlayerController p;
    DataHolder data;
    [SerializeField]
    public Upgrade[] upgrades = new Upgrade[10];
    public Upgrade[] upgrade = new Upgrade[3];
    public bool[] selected = new bool[3];
    public Image[] upgradeImage = new Image[3];
    public Sprite attackSprite, defenseSprite, speedSprite;
    public Image shipImage;
    public Image[] upgradeBg;
    public TextMeshProUGUI[] upgradeText;

    private void Awake()
    {
        instance = this;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {     
        p = PlayerController.instance;
        data = DataHolder.instance;
        /*bool shouldAppear = Random.Range(0, 100) <= (data.subLevel - 1) * 25;
        if (!shouldAppear)
            gameObject.SetActive(false);*/
        if (data.level == 1 && data.subLevel == 1)
        {
            foreach (Upgrade upgrade in upgrades)
            {
                upgrade.hasBeenChosen = false;
            }
            gameObject.SetActive(false);
            return;
        }

        shipImage.sprite = data.selectedShip.shipImage ? data.selectedShip.shipImage : data.selectedShip.shipSprite;
       
        for (int i = 0; i < 3; i++)
        {
            while (!upgrade[i] || upgrade[i].hasBeenChosen)
            {
                upgrade[i] = upgrades[Random.Range(0, upgrades.Length)];
            }
            upgrade[i].hasBeenChosen = true;
            if (upgrade[i].isAttack)
            {
                upgradeImage[i].sprite = attackSprite;
            }
            else
                if (upgrade[i].isDefense)
            {
                upgradeImage[i].sprite = defenseSprite;
            }
            else
            {
                upgradeImage[i].sprite = speedSprite;
            }

            upgradeText[i].text = upgrade[i].description;
        }
        Time.timeScale = 0;
    }
   

    public void UpgradeShip(int i)
    {
        StartCoroutine(UpgradeShipC(i));
    }
    public IEnumerator UpgradeShipC(int i)
    {
        if (!selected[i])
        {
            for (int j = 0; j < 3; j++)
            {
                selected[j] = false;
                //upgradeText[j].enabled = false;
                upgradeBg[j].color = Color.white;
            }
                     
            upgradeBg[i].color = new Color(1, .5f, 0);
            selected[i] = true;
           // upgradeText[i].enabled = true;
        }
        else
        {
            upgradeText[i].enabled = false;
            upgradeBg[i].GetComponent<Button>().interactable = false;
            upgradeBg[i].GetComponent<Animator>().SetTrigger("chosen");
            upgrade[i].hasBeenChosen = true;
            yield return new WaitForSecondsRealtime(.75f);
            float elapsed = 0;
            Color finalColor = Color.white;
            switch (i)
            {
                case 0: finalColor = Color.red;
                    break;
                case 1: finalColor = Color.yellow;
                    break;
                case 2: finalColor = Color.blue;
                    break;
            }
            while(elapsed < .1f)
            {
                shipImage.color = Color.Lerp(Color.white, finalColor, elapsed / .1f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            shipImage.color = finalColor;
            elapsed = 0;
            while(elapsed < .1f)
            {
                shipImage.color = Color.Lerp(finalColor, Color.white, elapsed / .1f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            for (int j = 0; j < 3; j++)
                if (j != i)
                    upgrade[j].hasBeenChosen = false;

            shipImage.color = Color.white;
            upgrade[i].UpgradeShip();
            yield return new WaitForSecondsRealtime(.5f);

            Time.timeScale = 1;
            this.gameObject.SetActive(false);
        }
    }
}
