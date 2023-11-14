using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTexture : MonoBehaviour
{
    public Renderer sprite;
    public Material mat;
    public Vector2 moveTexture;
    void Start()
    {
        sprite.material = new Material(sprite.material);
        mat = sprite.material;
    }


    void Update()
    {
        mat.mainTextureOffset += moveTexture * Time.deltaTime;
    }
}
