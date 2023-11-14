using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public Animator anim;
    public float speed = 1;
    void Start()
    {
        Destroy(gameObject, anim.GetCurrentAnimatorStateInfo(0).length + .02f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += speed * transform.right * Time.deltaTime;
    }
}
