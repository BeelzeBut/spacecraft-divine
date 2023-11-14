using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowPlayer : MonoBehaviour
{
    public float slowDuration, slowPercentage;
    void Start()
    {
        slowPercentage /= 100;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            PlayerController.instance.slowDuration = slowDuration;
            PlayerController.instance.moveSpeed = PlayerController.instance.maxMoveSpeed * slowPercentage;
        }
    }
}
