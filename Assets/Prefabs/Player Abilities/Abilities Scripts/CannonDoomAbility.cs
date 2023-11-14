using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = "Abilities/Cannon Doom")]
public class CannonDoomAbility : Ability
{
    public float duration;
    public Bullet bulletPrefab;
    public Transform muzzleFlash;
    private float durationTimer;
    public float damagePerBullet;
    private float damage;
    public float firerate;
    private float fireRate;
    private float shootTimer;
    private Image charge;
    public Transform cannonPrefab;
    Transform cannon;
    Transform firePoint;
    Transform targetEnemy;
    float chooseEnemyTimer;
    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        charge = UIManager.instance.charge;
        cooldown = baseCooldown;
        damage = damagePerBullet + .225f * level - .225f;
        fireRate = firerate - .0375f * level + .0375f;

        Quaternion playerRotation = p.transform.rotation;
        p.transform.rotation = Quaternion.identity;
        cannon = Instantiate(cannonPrefab, p.transform.position + cannonPrefab.transform.position, Quaternion.identity, p.transform);
        p.transform.rotation = playerRotation;
        p.spaceshipObjects.Add(cannon);
        firePoint = cannon.GetComponentsInChildren<Transform>()[1];
    }
    public override void UpdateAbility()
    {
        charge.fillAmount = durationTimer / duration;
        shootTimer -= Time.deltaTime;
        chooseEnemyTimer -= Time.deltaTime;

        if (durationTimer > 0)
        {
            durationTimer -= Time.deltaTime;
            if (durationTimer <= 0)
            {
                CancelUpdateAbility();
            }
        }

        if ((!targetEnemy || Physics2D.Linecast(p.transform.position, targetEnemy.position, LayerMask.GetMask("Obstacles"))) && chooseEnemyTimer <= 0)
        {
            chooseEnemyTimer = 1;
            ChooseEnemy();
        }

        if (targetEnemy && !Physics2D.Linecast(p.transform.position, targetEnemy.position, LayerMask.GetMask("Obstacles")))
        {
            Vector3 lookDir = targetEnemy.position - p.transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90;
            cannon.transform.rotation = Quaternion.Euler(0, 0, angle);

            if(shootTimer <= 0)
            {
                SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.shootSounds[5]);
                shootTimer = fireRate;
                Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
                Bullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
                bullet.damage = damage;
                bullet.canExplode = true;
            }
        }       
    }

    public override void ToActivateUpdate()
    {
        cannon.SetParent(null);
        abilityCoroutineManager.CannonOnPlayer(cannon, cannonPrefab.transform.position);
        p.abilityUpdateBool = true;
        durationTimer = duration;
        shootTimer = fireRate / 2;
        ChooseEnemy();
    }

    public override void CancelUpdateAbility()
    {
        p.abilityUpdateBool = false;
        abilityManager.GoOnCooldown();
        charge.fillAmount = 0;
        abilityManager.abilityUpdateOn = false;
        cannon.SetParent(p.transform);
        abilityCoroutineManager.StopCannonOnPlayer();
        cannon.transform.localRotation = Quaternion.identity;
    }

    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }

    public void ChooseEnemy()
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        int x = 0;
        if (allEnemies.Length > 0)
        {
            while (x < allEnemies.Length && Physics2D.Linecast(p.transform.position, allEnemies[x].transform.position, LayerMask.GetMask("Obstacles") | LayerMask.GetMask("Default")))
                x++;
            if (x < allEnemies.Length && allEnemies[x] != null)
                targetEnemy = allEnemies[x].transform;
            else
                targetEnemy = null;
        }
    }
}
