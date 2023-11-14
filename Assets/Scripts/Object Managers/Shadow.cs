using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shadow : MonoBehaviour
{
    Transform parent;
    public bool getSpriteFromParent = true;
    public bool shouldBeLower = false;
    private float height = 0.09f;
    public SpriteRenderer sprite;
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        sprite.color = new Color(0, 0, 0, .6f);
        parent = transform.parent;
        if (getSpriteFromParent)
            sprite.sprite = parent.GetComponentInChildren<SpriteRenderer>().sprite;
        if (shouldBeLower)
            height = .125f;
    }

    void LateUpdate()
    {
        transform.position = parent.position - Vector3.up * height;
    }
}
