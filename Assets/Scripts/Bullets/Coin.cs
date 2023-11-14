using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public float baseSpeed;
    public float speed;
    public float speedDecrease;
    public bool goToPlayer = false;
    void Start()
    {
        speedDecrease = Random.Range(speedDecrease - 1f, speedDecrease + 1.5f);
        StartCoroutine(BecomeCollectible());
    }


    void Update()
    {
        if (speed > 0)
        {
            speed -= speedDecrease * Time.deltaTime;
            transform.position += transform.right * Time.deltaTime * speed;
        }

        if(goToPlayer)
        {
            transform.position += (PlayerController.instance.transform.position - transform.position).normalized * baseSpeed * 2f * Time.deltaTime;
           // baseSpeed += Time.deltaTime;
           // if (baseSpeed > 9)
                //baseSpeed = 9;
            if((transform.position - PlayerController.instance.transform.position).sqrMagnitude <= .01f)
            {
                DataHolder.instance.goldCoins++;
                Destroy(gameObject);
            }
        }
    }

    IEnumerator BecomeCollectible()
    {
        yield return new WaitForSeconds(.5f);
        goToPlayer = true;
    }
}
