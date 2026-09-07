using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public PlayerController p;

    public float delay = .0875f;
    public float cameraSpeedLevel;
    private Vector2 velSpeed;
    public static CameraMovement instance;

    public Transform targetEnemy;
    public Transform activeRoom;
    public int roomSize;

    float originalOrtographicSize;
    Camera cam;
    float zoomVel;


    private void Awake()
    {
        // Referinta statica se stabileste in Awake, nu in Start. Unity nu garanteaza
        // ordinea de executie a metodelor Start intre componente, iar TrainingManager.Start
        // citeste CameraMovement.instance. Cand initializarea se facea in Start, referinta
        // ramanea, in functie de ordine, cea a camerei din scena anterioara — deja distrusa —
        // ceea ce arunca o exceptie si intrerupea initializarea modului de antrenament.
        instance = this;
    }

    void Start()
    {
        p = PlayerController.instance;
        cam = Camera.main;
        originalOrtographicSize = cam.orthographicSize;
        cameraSpeedLevel = 6 - DataHolder.instance.cameraSpeedLevel;
    }

    private void FixedUpdate()
    {
        if (targetEnemy != null && (p.canShoot || p.isShooting))
        {
            Vector3 desiredPos = new Vector3(targetEnemy.position.x + ((p.transform.position.x - targetEnemy.position.x) / 2f), targetEnemy.position.y + ((p.transform.position.y - targetEnemy.position.y) / 2f), -10f);
            transform.position = Vector2.SmoothDamp(transform.position, desiredPos,ref velSpeed, delay * cameraSpeedLevel);
        }
        else
        {
            Vector3 desiredPos;
            if (activeRoom == null)
            {
                if (p.canMove || p.cameraAheadOfPlayer)
                {
                    desiredPos = p.transform.position + (Vector3)p.moveDirection * 1.25f;
                }
                else
                {
                    desiredPos = p.transform.position;
                }
                transform.position = Vector2.SmoothDamp(transform.position, desiredPos, ref velSpeed, delay * 3);
            }
            else
            {
                if (!p.cameraAheadOfPlayer)
                {
                    Vector3 roomPos = activeRoom.position + new Vector3(.08f, -.35f);
                    float sqrDistance = (roomPos - p.transform.position).sqrMagnitude;
                    desiredPos = p.transform.position + (roomPos - p.transform.position) * (roomSize == 1 ? .6f : (roomSize == 2 ? .4f : .2f));                 
                }
                else
                {
                    desiredPos = p.transform.position + (Vector3)p.moveDirection * 1.25f;
                }
                transform.position = Vector2.SmoothDamp(transform.position, desiredPos, ref velSpeed, delay * 3);
            }
            
        }
        
    }


}
