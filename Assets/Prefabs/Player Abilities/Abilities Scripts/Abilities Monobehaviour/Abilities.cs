using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Abilities : MonoBehaviour
{
    public PlayerController p;
    public float baseCooldown;
    public string abilityName;
    public Sprite abilitySprite;
    [HideInInspector]
    public AbilityHolder abilityHolder;
    [HideInInspector]public bool isUpdateOn = false;

    public bool isUpdate = false;

    public abstract void Initialize();

    public abstract void TriggerAbility();

    public abstract void UpdateAbility();

    public abstract void ToActivateUpdate();

    public abstract void CancelUpdateAbility();
}
