using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeCircle : MonoBehaviour
{
    public Animator anim;
    bool hasPlayed;

    private void Update()
    {
        if(!PlayerController.instance.isAlive)
        {
            if (hasPlayed)
            {
                anim.SetTrigger("GoToNormal");
            }
            hasPlayed = false;
        }
        if (transform.localScale.x == 0)
        {
            if (hasPlayed)
            {              
                anim.SetTrigger("GoToNormal");
            }
            hasPlayed = false;
        }

        if(transform.localScale.x >= .85f && !hasPlayed)
        {
            hasPlayed = true;
            anim.ResetTrigger("GoToNormal");
            anim.SetTrigger("FullSize");
        }
    }
}
