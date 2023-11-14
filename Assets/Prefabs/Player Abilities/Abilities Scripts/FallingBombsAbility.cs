using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Falling Bombs")]
public class FallingBombsAbility : Ability
{
    public ArchedBullet bulletPrefab;
    public float damagePerBomb = 3.5f;
    private float damage;
    private int numberOfBombs = 2;
    public float fireRate;
    public float rangeAroundTarget = 1f;
    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        cooldown = baseCooldown;
        numberOfBombs = 2 + Mathf.CeilToInt(level / 2f);
        damage = damagePerBomb + level - 1;
    }
    public override void TriggerAbility()
    {
        p.StartCoroutine(ShootBombs());
    }

    public IEnumerator ShootBombs()
    {
        for(int i = 0; i < numberOfBombs; i++)
        {
            ArchedBullet bullet = Instantiate(bulletPrefab, p.transform.position, Quaternion.Euler(0, 0, 90));
            bool isCrit = Random.Range(0, 101f) < p.critChance;
            bullet.isCrit = isCrit;
            bullet.damage = damage;

            if(p.targetEnemy)
            {
                bullet.targetPosition = p.targetEnemy.transform.position + new Vector3(Random.Range(-rangeAroundTarget, rangeAroundTarget), Random.Range(-rangeAroundTarget, rangeAroundTarget));
            }
            else
            {
                bullet.targetPosition = p.transform.position + p.transform.up * 2f + new Vector3(Random.Range(-rangeAroundTarget, rangeAroundTarget), Random.Range(-rangeAroundTarget, rangeAroundTarget));
            }
            yield return new WaitForSeconds(fireRate);
        }
    }


    public override void ToActivateUpdate()
    {
        throw new System.NotImplementedException();
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
