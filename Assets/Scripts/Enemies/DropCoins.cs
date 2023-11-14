using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropCoins : MonoBehaviour
{
    public Coin coinPrefab;
    int coinsDropped;
    Enemy enemy;
    public int chanceToDrop;
    bool shouldDrop;
    void Start()
    {
        enemy = GetComponent<Enemy>();
        coinsDropped = Mathf.CeilToInt(Random.Range(enemy.unitValue, enemy.unitValue + 2));
        shouldDrop = Random.Range(0, 101) > chanceToDrop;
    }

    /*private void OnDestroy()
    {
        if(shouldDrop)
        {
            for(int i = 0; i < coinsDropped; i++)
            {
                Instantiate(coinPrefab, enemy.transform.position, Quaternion.Euler(0, 0, Random.Range(0, 360)));
            }
        }
    }*/
}
