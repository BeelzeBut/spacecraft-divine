using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShipSelect : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    PlayerController p;
    [Header("Spaceships")]
    public Spaceship s;
    DataHolder data;
    Spaceship oldShip;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        data = DataHolder.instance;
        if(data.selectedShip)
            s = data.selectedShip;
        SetSpaceship();
        p.abilityManager.Initialize(p.ability);
        StartCoroutine(p.ShowPlayer());
        data.isInMenu = false;
        data.gameHasEnded = false;
        data.Save();
    }
    public void SetSpaceship()
    {
        //s[i].gun.ship = s[i];
        p = PlayerController.instance;

        s.gun.Initialize();
        s.bulletPrefab.canBounce = s.canBounce;
        s.bulletPrefab.canPierce = s.canPierce;
        s.bulletPrefab.canExplode = s.canExplode;

        p.spriteRenderer.sprite = s.shipSprite;
        p.spriteRenderer.GetComponent<SpriteMask>().sprite = s.shipSprite;
        p.ship = s;
        p.ability = s.ability;
        p.maxHealth = s.maxHealth;
        p.fireRate = s.fireRate;
        p.attackRange = s.range;
        p.bulletsShot = s.bulletsShot;
        p.spread = s.spread;
        p.maxMoveSpeed = s.maxMoveSpeed;
        p.damagePerBullet = s.damagePerBullet;
        p.bulletPrefab = s.bulletPrefab;
        p.numberOfBursts = s.numberOfBursts;
        p.waitTimeBetweenBursts = s.waitTimeBetweenBursts;
        p.critChance = s.critChance;
        p.bulletPushBack = s.bulletPushBack;
        p.damageReduction = s.damageReduction;
        p.damageMultiplier = s.damageMultiplier;        

        int k = 0;
        p.transform.rotation = Quaternion.Euler(Vector3.zero);

        for (int j = p.firePoints.Count - 1; j >= 0; j--)
        {
            if (p.firePoints[j])
            {
                Destroy(p.firePoints[j].gameObject);
                p.firePoints.RemoveAt(j);
            }
        }
        p.firePoints.Clear();
        foreach (Transform firePoint in s.firePoints)
        {
            p.firePoints.Add(Instantiate(firePoint, p.transform.position + firePoint.position, Quaternion.Euler(p.transform.rotation.eulerAngles + firePoint.rotation.eulerAngles)));
            p.firePoints[k++].SetParent(p.transform);
        }
        k = 0;
        for (int j = p.thrusters.Count - 1; j >= 0; j--)
        {
            if (p.thrusters[j])
            {
                Destroy(p.thrusters[j].gameObject);
                p.thrusters.RemoveAt(j);
            }
        }
        p.thrusters.Clear();
        foreach (Transform thruster in s.thrusters)
        {
            p.thrusters.Add(Instantiate(thruster, p.transform.position + thruster.position, Quaternion.Euler(p.transform.rotation.eulerAngles + thruster.rotation.eulerAngles)));
            p.thrusters[k].gameObject.SetActive(false);
            p.thrusters[k++].SetParent(p.transform);
        }
        foreach (Transform obj in p.spaceshipObjects)
        {
            Destroy(obj.gameObject);
        }
        p.spaceshipObjects.Clear();  
    }
}

