using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Abilities/Mine Field"))]
public class MineFieldAbility : Ability
{
    public float mineSpeed;
    public float mineDamage;
    public int numberOfMines;
    private int mineNumber;
    private float damage;
    public float lifeTime;
    public Transform minesFirepoint;
    private Transform firePoint;
    public Mine minePrefab;

    public override void Initialize()
    {
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        p = PlayerController.instance;
        damage = mineDamage + .5f * level - .5f;
        cooldown = baseCooldown;
        mineNumber = numberOfMines + level - 1;
        firePoint = Instantiate(minesFirepoint, p.transform.position, p.transform.rotation);
        firePoint.SetParent(p.transform);
        p.spaceshipObjects.Add(firePoint);
    }

    public override void ToActivateUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void TriggerAbility()
    {
        Vector3 deviation = Vector3.zero;
        for(int i = 0; i < mineNumber; i++)
        {
            firePoint.rotation = Quaternion.Euler(deviation);
            deviation += Vector3.forward * (360f / (mineNumber));
            Mine mine = Instantiate(minePrefab, firePoint.transform.position, firePoint.transform.rotation);
            mine.speed = mineSpeed;
            mine.damage = damage;
            mine.lifeTime = Random.Range(lifeTime * 3f/4f, lifeTime * 5f/4f);
        }
        abilityManager.GoOnCooldown();
    }

    public override void UpdateAbility()
    {
        throw new System.NotImplementedException();
    }
    public override void CancelUpdateAbility()
    {
        throw new System.NotImplementedException();
    }


}
