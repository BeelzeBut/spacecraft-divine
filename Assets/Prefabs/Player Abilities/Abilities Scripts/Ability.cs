using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public abstract class Ability : ScriptableObject
{
    [HideInInspector]
    public PlayerController p;
    public float baseCooldown;
    [HideInInspector] public float cooldown = 0;
    public string abilityName;
    public Sprite abilitySprite;
    [HideInInspector]
    public AbilityCooldown abilityManager;
    [HideInInspector]
    public AbilityCoroutineManager abilityCoroutineManager;
    public bool isUpdate = false;
    public int level = 1;
    public string description;
    
    public bool canBeUsedWhileMoving = true;
    public bool canBeUsedWhileShooting = true;

    public abstract void Initialize();

    public abstract void TriggerAbility();

    public abstract void UpdateAbility();

    public abstract void ToActivateUpdate();

    public abstract void CancelUpdateAbility();


}
