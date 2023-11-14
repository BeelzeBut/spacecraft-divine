using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = ("Spaceship"))]
public class Spaceship : ScriptableObject
{
    [Header("General")]
    public AudioClip primarySound;
    public AudioClip secondarySound;
    public int level = 1;
    public bool isUnlocked = true;
    public Sprite shipSprite;
    public Sprite shipImage;

    [Header ("Stats")]
    public float maxHealth;
    [HideInInspector]
    public float currentHealth;
    [HideInInspector]
    public float minHealth;
    public float fireRate;
    public List<Transform> firePoints;
    public float range;
    public int bulletsShot;
    public float spread;
    public float maxMoveSpeed;
    public float damagePerBullet;
    public float bulletPushBack;
    public Bullet bulletPrefab;
    public List<Transform> thrusters;
    public bool isSmall = true;
    public bool hasDifferentAttack = false;
    public Gun gun;
    public SecondaryGun secondaryGun;
    public int numberOfBursts;
    public float waitTimeBetweenBursts;
    public bool alternateFirePoints;
    public string shipName;
    public float critChance = 20f;
    public float critMultiplier = 2f;
    public bool selected = false;
    public int orderNumber;

    [Header ("Unlocking")]
    public float priceToUnlock;
    public bool canBeBoughtWithGems;
    public bool canBeBoughtWithCurrency;
    public bool canBeUnlockedInGame;
    public string unlockInstructions;
    public Ability signatureAbility;
    public Ability ability;

    [Header("Multipliers")]
    public float attackUpgradeMultiplier = 1;
    public float defenseUpgradeMultiplier = 1;
    public float speedUpgradeMultiplier = 1;
    public float attackMultiplier = 40;
    public float defenseMultiplier = 20;
    public float regenAmount;
    public float speedMultiplier = 30;
    public float damageReduction, damageMultiplier;
    public float bonusAttackStat, bonusDefenseStat, bonusSpeedStat;
    public int audioIndex;

    public bool canExplode, canPierce, canBounce;
    public float chanceToBlockAttack;

    public bool moveSpeedOnAbility;
    public float moveSpeedIncrease;

    public bool pushBackOnAbility;
    public float pushBackStrength;

    public bool moveSpeedOnEnemyKill;
    public float moveSpeedAmount;

    public bool resetCdOnKill;
    public float resetCdChance;

    public float regenPerSecond = 0.6f;
    public float constantRegenPerSecond = 0;

    public bool increaseDamageOnKill;
    public float damageIncreaseOnKill;
    private Coroutine activeDamageIncreaseCoroutine = null;

    [Header("Guns Information")]
    public Sprite primaryGunSprite;
    public Sprite secondaryGunSprite;
    public string primaryGunDescription, secondaryGunDescription;

    [Header("Upgrades")]
    public List<Upgrade> playerUpgrades = new List<Upgrade>();
    public List<int> upgradesIndex = new List<int>();

    public void ApplyUpgrades()
    {
        PlayerController p = PlayerController.instance;
        if (moveSpeedOnAbility)
        {
            p.moveSpeed *= (1 + moveSpeedIncrease / 100f);
            p.slowDuration = 1.5f;
        }

        if (pushBackOnAbility)
        {
           // PlayerController.instance.pushBackExplosion.GetComponentInChildren<ExplosionDamage>().pushBack = pushBackStrength;
            Instantiate(PlayerController.instance.pushBackExplosion, PlayerController.instance.transform.position, Quaternion.Euler(Vector3.forward * Random.Range(0, 360)));
        }
    }

    public void UpgradeOnEnemyKill()
    {
        PlayerController p = PlayerController.instance;
        if(moveSpeedOnEnemyKill)
        {
            p.moveSpeed *= (1 + moveSpeedAmount / 100f);
            p.slowDuration = 1.5f;
        }
        if(resetCdOnKill)
        {
            bool shouldReset = Random.Range(0, 100) <= resetCdChance;
            if(shouldReset)
            {
                AbilityCooldown.instance.AbilityReady();
                p.ship.secondaryGun.shootTimer = -0.1f;
            }
        }
        if(increaseDamageOnKill)
        {
            if(activeDamageIncreaseCoroutine != null)
            {
                GameManager.instance.StopCoroutine(activeDamageIncreaseCoroutine);
                ResetDamageMultiplier();
            }
            activeDamageIncreaseCoroutine = GameManager.instance.StartCoroutine(IncreaseDamageOnKill());
        }
    }


    public IEnumerator IncreaseDamageOnKill()
    {
        damageMultiplier += damageIncreaseOnKill;
        yield return new WaitForSeconds(5f);
        ResetDamageMultiplier();
    }

    void ResetDamageMultiplier()
    {
        damageMultiplier -= damageIncreaseOnKill;
        activeDamageIncreaseCoroutine = null;
    }





}

/*
 * public float GetFireRate()
    {
        return fireRate;
    }
  public float GetBullets()
    {
        return damagePerBullet;
    }
    public float GetSpread()
    {
        return spread;
    }
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
    public int GetDamage()
    {
        return damagePerBullet;
    }
    public string GetDescription()
    {
        return description;
    }
    public int GetAbilitynumber()
    {
        return abilityNumber;
    } */
