using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomChest : MonoBehaviour
{
    public Coin coinPrefab;
    public float value;
    public List<Collectible> randomDrops = new List<Collectible>();
    public List<float> dropChances = new List<float>();
    public SpriteRenderer spriteRenderer;
    public Sprite openChestSprite;
    GameManager gm;
    AudioSource source;
    public AudioClip coinSound;
    public Blueprint bossBlueprint;
    void Start()
    {
        gm = GameManager.instance;
        value = Random.Range(value * .5f, value * 1.5f);
        source = GetComponent<AudioSource>();
    }

    public void SpawnCoins()
    {
        spriteRenderer.sprite = openChestSprite;
        GetComponent<BoxCollider2D>().enabled = false;
        if (value > 0)
            source.Play();
        for (int i = 0; i < value / 2; i++)
        {
            Coin coin = Instantiate(coinPrefab, transform.position, Quaternion.Euler(0, 0, Random.Range(0, 360)));
            coin.speed = Random.Range(coin.baseSpeed * .5f, coin.baseSpeed);
        }

        if (!gm.shouldDropBossBlueprint)
        {
            int[] v = new int[201];
            int k = 0;
            for (int i = 1; i < 201; i++)
            {
                if (dropChances[k] <= 0 || (randomDrops[k].onlyDropsOnce && DataHolder.instance.dataSaved.hasBeenUnlocked[k]))
                {
                    k++;
                    if (k == dropChances.Count)
                        break;
                }
                v[i] = k;
                dropChances[k] -= .5f;
            }
            int randomDropIndex = Random.Range(1, 201);
            randomDropIndex = v[randomDropIndex];
            if (randomDropIndex != 0)
            {
                if (randomDrops[randomDropIndex].onlyDropsOnce)
                {
                    if (!DataHolder.instance.dataSaved.hasBeenUnlocked[randomDropIndex])
                    {
                        Instantiate(randomDrops[randomDropIndex], transform.position, Quaternion.identity);
                    }
                }
                else
                {
                    Collectible drop = Instantiate(randomDrops[randomDropIndex], transform.position, Quaternion.identity);
                    if (randomDrops[randomDropIndex].GetComponent<AbilityDrop>())
                    {
                        StartCoroutine(MoveAbilityDrop(drop));
                    }
                }
            }
        }
        else
        {
            Instantiate(bossBlueprint, transform.position, Quaternion.identity);
            gm.shouldDropBossBlueprint = false;
        }
    }

    IEnumerator MoveAbilityDrop(Collectible drop)
    {
        Vector3 dir = new Vector2(Random.Range(-1f, 1f), 0).normalized;
        if (dir.x == 0)
            dir.x = 1;
        float elapsed = 0;
        while(elapsed < .66f)
        {
            drop.transform.position += dir * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            SpawnCoins();
        }
    }
}
