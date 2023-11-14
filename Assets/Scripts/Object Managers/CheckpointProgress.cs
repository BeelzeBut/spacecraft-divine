using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointProgress : MonoBehaviour
{
    public Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            anim.SetTrigger("Checkpoint");
        }
    }
}
