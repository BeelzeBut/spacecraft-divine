using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Abilities/Plasma Ability"))]
public class PlasmaAbility : Ability
{
    public FragBullet plasmaBulletPrefab;
    [SerializeField]
    public GameObject plasmaBulletMuzzle;
    public float initialBulletDamage;
    private float damage;
    public FixedJoystick plasmaJoystick;
    public float plasmaBulletRange = 3;
    private bool readyToLaunch = false;
    private GameObject target, rangeIndicator;
    public int numberOfSmallBullets;
    private int numberOfSBullets;
    public LineRenderer trajectoryPrefab;
    private LineRenderer trajectory;


    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        target = abilityManager.abilityTargetRange;
        target.transform.localScale = Vector3.one * .25f;
        target.SetActive(false);
        cooldown = baseCooldown;
        damage = initialBulletDamage + .33f * level - .33f;
        numberOfSBullets = numberOfSmallBullets + level * 2 - 2;
        p.transform.rotation = Quaternion.Euler(Vector3.zero);
        trajectory = Instantiate(trajectoryPrefab, p.transform.position, p.transform.rotation);
        p.spaceshipObjects.Add(trajectory.transform);
        trajectory.useWorldSpace = true;
        trajectory.enabled = false;
    }

    public override void UpdateAbility()
    {
        if (abilityManager.abilityButton.GetComponent<ButtonScript>().buttonPressed)
        {
            target.SetActive(true);
            trajectory.enabled = true;
            target.transform.position = p.transform.position + p.transform.up * plasmaBulletRange;
            trajectory.SetPosition(0, p.transform.position);
            trajectory.SetPosition(1, target.transform.position);
            readyToLaunch = true;
            Vector3 aimDir =p.transform.up;
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90;
            p.angle = angle;
        }
        else
        {
            target.SetActive(false);
            if (readyToLaunch)
            {
                TriggerAbility();
                CancelUpdateAbility();
            }
        }
    }

    public override void TriggerAbility()
    {
        plasmaBulletPrefab.damage = damage;
        plasmaBulletPrefab.explodeAtLocation = true;
        plasmaBulletPrefab.locationToExplode = target.transform.position;
        readyToLaunch = false;
        abilityManager.GoOnCooldown();
        ShootPlasmaBullet();
    }

    public override void CancelUpdateAbility()
    {
        trajectory.enabled = false;
        p.abilityUpdateBool = false;
        abilityManager.abilityUpdateOn = false;
    }

    public override void ToActivateUpdate()
    {
        p.abilityUpdateBool = true;
    }

    public void ShootPlasmaBullet()
    {
        Instantiate(plasmaBulletMuzzle, p.firePoints[0].position, p.firePoints[0].rotation);
        FragBullet plasmaBullet = Instantiate(plasmaBulletPrefab, p.firePoints[0].position, p.firePoints[0].rotation);
        plasmaBullet.smallBulletPrefab.damage = damage;
        plasmaBullet.numberOfSmallBullets = numberOfSBullets;      
        abilityCoroutineManager.SmallBulletsPulse(plasmaBullet, numberOfSBullets);

        Vector3 shootDir = plasmaBullet.locationToExplode - p.transform.position;
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        plasmaBullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
