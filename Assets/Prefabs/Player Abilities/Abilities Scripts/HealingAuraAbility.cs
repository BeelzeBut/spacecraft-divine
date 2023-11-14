using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = ("Abilities/Healing Aura Ability"))]
public class HealingAuraAbility : Ability
{
    public float duration;
    private float durationTimer;
    private Image charge;
    public float healingPerSecond;
    private float heal;
    public GameObject spawningObjects;
    public GameObject areaPrefab;
    private GameObject activeArea;
    public float radius;
    private float repeatTimer;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        charge = UIManager.instance.charge;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .35f * level + .35f;
        heal = healingPerSecond + .25f * level - .25f;
    }

    public override void ToActivateUpdate()
    {
        p.abilityUpdateBool = true;
        activeArea = Instantiate(areaPrefab, p.transform.position, Quaternion.identity);
        activeArea.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        abilityCoroutineManager.StartCoroutine(SpawnArea());
        durationTimer = duration;
        repeatTimer = 0;
    }
    public override void UpdateAbility()
    {
        activeArea.transform.position = p.transform.position;
        charge.fillAmount = durationTimer / duration;

        if (durationTimer > 0)
        {
            durationTimer -= Time.deltaTime;
            if (durationTimer <= 0)
            {
                CancelUpdateAbility();
            }
        }

        p.health += heal * Time.deltaTime;
        p.minHealth += heal * Time.deltaTime;
        if (p.health > p.maxHealth)
            p.health = p.maxHealth;
        p.healthSlider.value = .125f + p.health / p.maxHealth * .875f;
        p.healthText.text = (Mathf.CeilToInt(p.health / p.maxHealth * 100f)).ToString();

        repeatTimer -= Time.deltaTime;
        if (repeatTimer <= 0)
        {
            repeatTimer = .25f;
            SpawnObjects();
        }
    }

    void SpawnObjects()
    {
        for (int i = 0; i < Random.Range(1, 3); i++)
        {
            float x, y;
            do
            {
                x = Random.Range(-radius, radius);
                y = Random.Range(-radius, radius);
            } while (x * x + y * y > radius * radius);
            Instantiate(spawningObjects, p.transform.position + new Vector3(x, y * .9f), Quaternion.identity, activeArea.transform);
        }
    }

    public override void CancelUpdateAbility()
    {
        //abilityCoroutineManager.CancelInvoke("CheckForEnemies");
        abilityCoroutineManager.StartCoroutine(DeSpawnArea());
        p.abilityUpdateBool = false;
        abilityManager.GoOnCooldown();
        charge.fillAmount = 0;
        abilityManager.abilityUpdateOn = false;
    }

    public override void TriggerAbility()
    {

    }

    IEnumerator SpawnArea()
    {
        float elapsed = 0;
        SpriteRenderer areaSprite = activeArea.GetComponent<SpriteRenderer>();
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while (elapsed < 0.3f)
        {
            areaSprite.color = Color.Lerp(areaSprite.color, Color.white, elapsed / 0.3f);

            elapsed += Time.deltaTime;
            yield return wait;
        }
        areaSprite.color = Color.white;
    }

    IEnumerator DeSpawnArea()
    {
        float elapsed = 0;
        SpriteRenderer areaSprite = activeArea.GetComponent<SpriteRenderer>();
        Color desiredColor = new Color(1, 1, 1, 0);
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while (elapsed < 0.15f)
        {
            areaSprite.color = Color.Lerp(areaSprite.color, desiredColor, elapsed / 0.15f);

            elapsed += Time.deltaTime;
            yield return wait;
        }
        Destroy(activeArea);

    }
}
