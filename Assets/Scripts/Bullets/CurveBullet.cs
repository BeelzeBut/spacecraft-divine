using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveBullet : Bullet
{
    public int direction = 1;
    public float curveAmount;
    public float stopRotationTimer;
    public bool shouldOscilate = false;
    public float oscilationAmount;
    public float oscilationTimer = 0;
    public float oscilationSpeed;
    public bool randomizeDirection;
    public bool startRotating = true;

    private void Start()
    {
        if(randomizeDirection)
        {
            direction = Random.Range(0, 2);
            if (direction == 0)
                direction = -1;
        }
    }
    void FixedUpdate()
    {
        if (startRotating)
        {
            stopRotationTimer -= Time.fixedDeltaTime;
            if (!shouldOscilate)
            {
                if (stopRotationTimer > 0)
                    transform.Rotate(Vector3.forward * curveAmount * Time.fixedDeltaTime * direction);
            }
            else
            {
                oscilationTimer += Time.fixedDeltaTime * oscilationSpeed * 2 * 3.14f;
                transform.Rotate(Vector3.forward * oscilationAmount * Mathf.Cos(oscilationTimer) * direction);
            }
        }
        rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);

    }
}
