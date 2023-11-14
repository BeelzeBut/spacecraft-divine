using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Spaceship"))]
public class Spaceship : ScriptableObject
{
    [HideInInspector]
    public int level = 1;
    public bool isUnlocked = true;
    public Sprite shipSprite;
    public Sprite shipImage;
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
    public int numberOfBursts;
    public float waitTimeBetweenBursts;
    public bool alternateFirePoints;
    public string shipName;
    public float critChance = 20f;
    public bool selected = false;
    public int orderNumber;
    public float priceToUnlock;
    public bool canBeBoughtWithGems;
    public bool canBeBoughtWithCurrency;
    public bool canBeUnlockedInGame;
    public string unlockInstructions;
    public Ability signatureAbility;
    public Ability ability;
    public float attackMultiplier = 40;
    public float defenseMultiplier = 20;
    public float regenAmount;
    public float speedMultiplier = 30;
    public float damageReduction, damageMultiplier;
    public int audioIndex;

    public bool canExplode, canPierce, canBounce;
    public float chanceToBlockAttack;

    public bool moveSpeedOnAbility;
    public float moveSpeedIncrease;


    public void ApplyUpgrades()
    {
        if(moveSpeedOnAbility)
        {
            PlayerController.instance.moveSpeed += moveSpeedIncrease / 100f * PlayerController.instance.moveSpeed;
            PlayerController.instance.slowDuration = 1.5f;
        }

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
