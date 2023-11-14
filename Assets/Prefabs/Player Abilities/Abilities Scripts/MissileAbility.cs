using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;


[CreateAssetMenu(menuName = "Abilities/Missile Ability")]
public class MissileAbility : Ability
{
    public TargetedBullet missilePrefab;
    public Transform firePoint;
    private Transform firepoint;
    [SerializeField]
    public GameObject muzzleFlash;
    public float damagePerMissile;
    private float damage;
    private float chargeAmount = 0;
    public float chargeTime = .5f;
    private int maxMissiles = 3;
    public float missileRange;
    public Image charge;
    Vector3 deviation;
    Charge chargeCircles;

    Enemy[] closestEnemies;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        maxMissiles = 2 + Mathf.CeilToInt(level / 2f);
        damage = damagePerMissile + 1 * level - 1;
        chargeAmount = 0;
        closestEnemies = new Enemy[(int)maxMissiles];
        cooldown = baseCooldown;
        Quaternion playerRotation = p.transform.rotation;
        p.transform.rotation = Quaternion.identity;
        firepoint = Instantiate(firePoint, p.transform.position + firePoint.transform.position, firePoint.rotation, p.transform);
        p.spaceshipObjects.Add(firepoint);
        p.transform.rotation = playerRotation;
    }

    public override void TriggerAbility()
    {
        ShootMissiles();
    }

    public override void UpdateAbility()
    {
        if (abilityManager.abilityButton.GetComponent<ButtonScript>().buttonPressed)
        {
            chargeAmount += Time.deltaTime;
            if (chargeAmount >= chargeTime)
                chargeAmount = chargeTime;

            int i = Mathf.FloorToInt(chargeAmount / chargeTime * maxMissiles);
            if(i < maxMissiles)
                chargeCircles.circles[i].transform.localScale = Vector3.one * ((chargeAmount / chargeTime * maxMissiles) - i);
        }
        else
        {
            TriggerAbility();
            CancelUpdateAbility();
            abilityManager.GoOnCooldown();
        }
    }

    public override void CancelUpdateAbility()
    {
        p.abilityUpdateBool = false;
        abilityManager.abilityUpdateOn = false;
        chargeAmount = 0;
        for (int i = 0; i < maxMissiles; i++)
        {
            chargeCircles.circles[i].transform.localScale = Vector3.zero;
        }
    }
     public override void ToActivateUpdate()
    {
        p.abilityUpdateBool = true;
    }
    void ShootMissiles()
    {
        //int numberOfMissiles = 1 + Mathf.FloorToInt(chargeAmount / chargeTime * (maxMissiles - 1));
        int numberOfMissiles = maxMissiles;
        FindClosestEnemies();
        deviation = new Vector3(0, 0, -90);

        int maxNumberOfMissiles = numberOfMissiles;
        Instantiate(muzzleFlash, firepoint.position, p.firePoints[0].rotation);

        while (numberOfMissiles > 0)
        {
            bool hasFoundEnemies = false;
            for(int i = 0; i < maxNumberOfMissiles && numberOfMissiles > 0 && i < closestEnemies.Length; i++)
            {
                if (closestEnemies[i])
                {
                    hasFoundEnemies = true;
                    ShootMissile(closestEnemies[i]);
                    numberOfMissiles--;
                }
            }
            if (!hasFoundEnemies)
                break;
        }
        if(numberOfMissiles > 0)
        {
            for (int i = 0; numberOfMissiles > 0; i++)
            {
                ShootMissileAtLocation(p.transform.position + p.transform.up * 2f);
                numberOfMissiles--;
            }
        }
        
    }
    void ShootMissile(Enemy targetedEnemy)
    {
        TargetedBullet missile = Instantiate(missilePrefab,firepoint.position , Quaternion.Euler(firepoint.rotation.eulerAngles + deviation));
        deviation += Vector3.forward * (180f / (maxMissiles-1));
        missile.target = targetedEnemy.transform;
        missile.damage = damage;
    }

    void ShootMissileAtLocation(Vector3 targetPos)
    {
        TargetedBullet missile = Instantiate(missilePrefab, firepoint.position, Quaternion.Euler(firepoint.rotation.eulerAngles + deviation));
        deviation += Vector3.forward * (180f / (maxMissiles - 1));
        missile.locationToExplode = targetPos;
        missile.damage = damage;
    }
    public void FindClosestEnemies()
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        allEnemies = allEnemies.OrderBy(x => (x.transform.position - p.transform.position).sqrMagnitude).ToArray();

        for(int i = 0; i < Mathf.Min(maxMissiles, allEnemies.Length); i++)
        {
            if ((allEnemies[i].transform.position - p.transform.position).sqrMagnitude <= missileRange * missileRange && !Physics2D.Linecast(p.transform.position, allEnemies[i].transform.position, LayerMask.GetMask("Obstacles"))) 
                closestEnemies[i] = allEnemies[i];
        }
    }
}


