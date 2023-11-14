using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = ("Abilities/Orbiting Ability"))]
public class OrbitingAbility : Ability
{
    public float damagePerBullet;
    public Bullet bulletPrefab;
    public float duration;
    public float rotationSpeed;
    public float maxOrbitSize = 1.75f;
    public Transform rotativeObjectPrefab;
    private float orbitSize;
    private Transform rotativeObject;
    private float damage;
    private float numberOfBullets;
    private float durationTimer;
    private Image charge;
    private List<Transform> orbitingBullets;
    private bool shouldGoBackIn = false;
    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        charge = UIManager.instance.charge;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        if (level % 2 == 1)
            damage = damagePerBullet + .5f;
        numberOfBullets = 2 + Mathf.CeilToInt(level / 2f);
        cooldown = baseCooldown;
        if(!rotativeObject)
            rotativeObject = Instantiate(rotativeObjectPrefab);
    }    

    public override void ToActivateUpdate()
    {
        p.abilityUpdateBool = true;
        durationTimer = duration;
        orbitingBullets.Clear();
        orbitSize = maxOrbitSize;
        shouldGoBackIn = false;
        SpawnSmallBullets();
    }
    public override void UpdateAbility()
    {
        charge.fillAmount = durationTimer / duration;

        if (durationTimer > 0 && !shouldGoBackIn)
        {
            durationTimer -= Time.deltaTime;
            if (durationTimer <= 0)
            {
                CancelUpdateAbility();              
            }
        }

        rotativeObject.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        rotativeObject.position = p.transform.position;

        if (orbitSize > 0 && !shouldGoBackIn)
        {
            orbitSize -= 3 * Time.deltaTime;
            for (int i = 0; i < orbitingBullets.Count; i++)
            {
                orbitingBullets[i].transform.position += orbitingBullets[i].right * 3 * Time.deltaTime;
            }
        }

    }
    public override void CancelUpdateAbility()
    {
        shouldGoBackIn = true;
        orbitSize = maxOrbitSize;
        GameManager.instance.StartCoroutine(GoBackIn());
    }

    public IEnumerator GoBackIn()
    {
        while (orbitSize > 0)
        {
            orbitSize -= 3 * Time.deltaTime;
            for (int i = 0; i < orbitingBullets.Count; i++)
            {
                orbitingBullets[i].transform.position -= orbitingBullets[i].right * 3 * Time.deltaTime;
            }
            yield return null;
        }
        for (int i = 0; i < orbitingBullets.Count; i++)
        {
            Destroy(orbitingBullets[i].gameObject);
        }
        abilityManager.GoOnCooldown();
        p.abilityUpdateBool = false;
        abilityManager.abilityUpdateOn = false;
        charge.fillAmount = 0;
    }

    public void SpawnSmallBullets()
    {
        Vector3 deviation = Vector3.forward * (180f / numberOfBullets - 180);
        for (int i = 0; i < numberOfBullets; i++)
        {
            Bullet bullet = Instantiate(bulletPrefab, rotativeObject.position, Quaternion.Euler(rotativeObject.rotation.eulerAngles + deviation), rotativeObject);
            bullet.transform.localPosition += bullet.transform.right * .01f;
            bullet.damage = damage;
            orbitingBullets.Add(bullet.transform);
            deviation += Vector3.forward * 360f / numberOfBullets;
        }
    }
    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }
}
