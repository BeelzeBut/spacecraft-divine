using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Abilities/Rocket Ability")]
public class DarkBombAbility : Ability
{
    public ArchBulletTest blackBulletPrefab;
    [SerializeField]
    public GameObject rocketMuzzle;
    public float rocketDamage;
    private float damage;
    public FixedJoystick rocketJoystick;
    public float rocketRange;
    private bool readyToLaunch = false;
    private GameObject rocketTarget;
    public Transform firePoint;
    private Transform firepoint;


    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        rocketTarget = abilityManager.abilityTargetRange;
        rocketTarget.transform.localScale = Vector3.one;
        rocketTarget.SetActive(false);
        cooldown = baseCooldown - .25f * level + .25f;
        damage = rocketDamage + 1.5f * level - 1.5f;

        Quaternion playerRotation = p.transform.rotation;
        p.transform.rotation = Quaternion.identity;
        firepoint = Instantiate(firePoint, p.transform.position + firePoint.transform.position, firePoint.rotation, p.transform);
        p.spaceshipObjects.Add(firepoint);
        p.transform.rotation = playerRotation;
    }

    public override void UpdateAbility()
    {
        if (abilityManager.abilityButton.GetComponent<ButtonScript>().buttonPressed)
        {
            p.canShoot = false;
            readyToLaunch = true;
            if (p.targetEnemy)
            {
                rocketTarget.SetActive(true);
                rocketTarget.transform.position = p.targetEnemy.transform.position;
                //readyToLaunch = true;
                Vector3 aimDir = p.targetEnemy.transform.position - p.transform.position;
                float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90;
                p.angle = angle;
            }
            else
            {
                rocketTarget.SetActive(false);
                //readyToLaunch = false;
            }
        }
        else
        {
            //rocketTarget.SetActive(false);
            if (readyToLaunch)
            {
                if(p.targetEnemy)
                    TriggerAbility();
                CancelUpdateAbility();
            }
            //else
            {
                //CancelUpdateAbility();
            }
        }      
    }

    public override void TriggerAbility()
    { 
        //p.canShoot = true;
        blackBulletPrefab.damage = damage;
        abilityManager.GoOnCooldown();
        p.StartCoroutine(ShootBullet());
    }

    public override void CancelUpdateAbility()
    {
        rocketTarget.SetActive(false);
        p.abilityUpdateBool = false;
        abilityManager.abilityUpdateOn = false;
        abilityManager.GoOnCooldown();
    }

    public override void ToActivateUpdate()
    {
        p.abilityUpdateBool = true;
        CameraMovement.instance.targetEnemy = null;
    }

    public IEnumerator ShootBullet()
    {
        /*Instantiate(muzzleFlash, firepoint.position, firepoint.rotation);
        ExplosionBullet rocket = Instantiate(rocketPrefab, firepoint.position, firepoint.rotation);
        Vector3 shootDir = rocketPrefab.locationToExplode - p.transform.position;
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        rocket.transform.rotation = Quaternion.Euler(0, 0, angle);*/
        p.isShooting = true;

        yield return new WaitForSeconds(.05f);

        Instantiate(blackBulletPrefab.muzzleFlash, p.transform.position + p.transform.up * 0.2f, Quaternion.Euler(p.transform.rotation.eulerAngles + Vector3.forward * 90));
        ArchBulletTest bullet = Instantiate(blackBulletPrefab, p.transform.position + p.transform.up * 0.2f, Quaternion.Euler(p.transform.rotation.eulerAngles + Vector3.forward * 90));
        if (p.targetEnemy)
        {
            Enemy pEnemy = p.targetEnemy.GetComponent<Enemy>();
            if (pEnemy.isMoving)
                bullet.targetPos = pEnemy.transform.position + (Vector3)pEnemy.moveDirection / 2.25f * pEnemy.curMoveSpeed;
            else
                bullet.targetPos = pEnemy.transform.position;
        }
        else
            bullet.targetPos = p.transform.position + p.transform.up * 2.5f;
        p.isShooting = false;
    }
}
