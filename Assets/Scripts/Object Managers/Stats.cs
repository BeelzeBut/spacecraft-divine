using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats
{
    public static int statMin = 0;
    public static int statMax = 19;

    private SingleStat attackStat;
    private SingleStat armorStat;
    private SingleStat speedStat;
    public enum Type
    {
        Attack,
        Armor,
        Speed,
    }
    public Stats(float attackStatAmount, float armorStatAmount, float speedStatAmount)
    {
        attackStat = new SingleStat(attackStatAmount);
        armorStat = new SingleStat(armorStatAmount);
        speedStat = new SingleStat(speedStatAmount);
    }

    private SingleStat GetSingleStat(Type statType)
    {
        switch(statType)
        {
            default:
            case Type.Attack: return attackStat;
            case Type.Armor: return armorStat;
            case Type.Speed: return speedStat;
        }
    }

    public void SetStat(Type statType, float statAmount)
    {
        GetSingleStat(statType).SetStatAmount(statAmount);
    }

    public float GetStatAmount(Type statType)
    {
        return GetSingleStat(statType).GetStatAmount();
    }

    public float GetStatAmountNormalized(Type statType)
    {
        return GetSingleStat(statType).GetStatAmountNormalized();
    }

    private class SingleStat
    {
        private float stat;

        public SingleStat(float statAmount)
        {
            SetStatAmount(statAmount);
        }

        public void SetStatAmount(float statAmount)
        {
            stat = Mathf.Clamp(statAmount, statMin, statMax);
        }

        public float GetStatAmount()
        {
            return stat;
        }

        public float GetStatAmountNormalized()
        {
            return stat / statMax;
        }
    }
}
