using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveManager
{
    /*public static void SaveData(PlayerController p)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/player.xml";
        FileStream stream = new FileStream(path, FileMode.Create);

        SaveData data = new SaveData(p);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static SaveData LoadData()
    {
        string path = Application.persistentDataPath + "/player.xml";
        FileStream stream = new FileStream(path, FileMode.Open);
        if (File.Exists(path) && stream.Length > 0)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            SaveData data = formatter.Deserialize(stream) as SaveData;
            stream.Close();

            return data;
        }
        else
        {
            stream.Close();
            Debug.Log("No save file exists");
            return null;
        }
    }*/

    

}



/*
 public SaveData SavePlayerData(PlayerController p)
    {
        if (p)
        {
            for (int i = 0; i < 3; i++)
            {
                orderNumber[i] = p.shipSelect.s[i].orderNumber;
                maxHealth[i] = p.shipSelect.s[i].maxHealth;
                currentHealth[i] = p.shipSelect.s[i].currentHealth;
                fireRate[i] = p.shipSelect.s[i].fireRate;
                bulletsShot[i] = p.shipSelect.s[i].bulletsShot;
                spread[i] = p.shipSelect.s[i].spread;
                maxMoveSpeed[i] = p.shipSelect.s[i].maxMoveSpeed;
                damagePerbullet[i] = p.shipSelect.s[i].damagePerBullet;
                numberOfBursts[i] = p.shipSelect.s[i].numberOfBursts;
                waitTimeBetweenBursts[i] = p.shipSelect.s[i].waitTimeBetweenBursts;
                critChance[i] = p.shipSelect.s[i].critChance;
            }
        }
        else if (DataHolder.instance.selectedShips != null)
        {
            for (int i = 0; i < 3; i++)
            {
                //Save ships from dataHolder
                orderNumber[i] = DataHolder.instance.selectedShips[i].orderNumber;
                maxHealth[i] = DataHolder.instance.selectedShips[i].maxHealth;
                currentHealth[i] = DataHolder.instance.selectedShips[i].currentHealth;
                fireRate[i] = DataHolder.instance.selectedShips[i].fireRate;
                bulletsShot[i] = DataHolder.instance.selectedShips[i].bulletsShot;
                spread[i] = DataHolder.instance.selectedShips[i].spread;
                maxMoveSpeed[i] = DataHolder.instance.selectedShips[i].maxMoveSpeed;
                damagePerbullet[i] = DataHolder.instance.selectedShips[i].damagePerBullet;
                numberOfBursts[i] = DataHolder.instance.selectedShips[i].numberOfBursts;
                waitTimeBetweenBursts[i] = DataHolder.instance.selectedShips[i].waitTimeBetweenBursts;
                critChance[i] = DataHolder.instance.selectedShips[i].critChance;
            }
        }

        level[0] = DataHolder.instance.level;
        level[1] = DataHolder.instance.subLevel;
        goldCoins = DataHolder.instance.goldCoins;
        gems = DataHolder.instance.gems;
        gameHasEnded = DataHolder.instance.gameHasEnded;
    }*/


