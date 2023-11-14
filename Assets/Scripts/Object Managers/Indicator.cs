using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    RectTransform pointerRectTransform;
    public Transform targetEnemy;

    Camera cam;
    [SerializeField]
    private SpriteRenderer arrowSprite;


    private void Awake()
    {
        cam = Camera.main;
        pointerRectTransform = transform.Find("Arrow").GetComponent<RectTransform>();
        targetEnemy = GetComponentInParent<Transform>();
    }

    void Update()
    {
        if (targetEnemy == null)
        {
            arrowSprite.enabled = false;
            return;
        }
        Vector3 toPosition = targetEnemy.position;
        Vector3 fromPosition = cam.transform.position;
        fromPosition.z = 0f;
        Vector3 dir = (toPosition - fromPosition).normalized;
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) % 360;
        pointerRectTransform.localEulerAngles = new Vector3(0, 0, angle);

        float borderSize = Screen.height / 18;
        Vector3 targetPositionScreenPoint = cam.WorldToScreenPoint(targetEnemy.position);
        bool isOffscreen = targetPositionScreenPoint.x <= -borderSize || targetPositionScreenPoint.x >= Screen.width + borderSize || targetPositionScreenPoint.y <= -borderSize || targetPositionScreenPoint.y >= Screen.height + borderSize;

        if (isOffscreen)
        {
            arrowSprite.enabled = true ;
            Vector3 cappedTargetScreenPosition = targetPositionScreenPoint;

            if (cappedTargetScreenPosition.x <= -borderSize)
                cappedTargetScreenPosition.x = borderSize;

            if (cappedTargetScreenPosition.y <= -borderSize)
                cappedTargetScreenPosition.y = borderSize;

            if (cappedTargetScreenPosition.x >= Screen.width + borderSize)
                cappedTargetScreenPosition.x = Screen.width - borderSize;

            if (cappedTargetScreenPosition.y >= Screen.height + borderSize)
                cappedTargetScreenPosition.y = Screen.height - borderSize;


            pointerRectTransform.position = cam.ScreenToWorldPoint(cappedTargetScreenPosition);
            //pointerRectTransform.localPosition = new Vector3(pointerRectTransform.localPosition.x, pointerRectTransform.localPosition.y, pointerRectTransform.localPosition.z);
        }
        else arrowSprite.enabled = false;
        
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, -90); 
    }
}
