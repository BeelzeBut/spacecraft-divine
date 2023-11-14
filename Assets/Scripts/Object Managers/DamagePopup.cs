using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public bool isCriticalText = false;
    public TextMeshPro tmp;
    private float dissapearTimer;
    public float dissapearTimerMax;
    private Color textColor;
    private Vector3 moveVector,position;
    private static int sortingOrder;

    public static DamagePopup Create(Vector3 position, int damageAmount, bool isCriticalHit, GameObject target)
    {
        int addedValue = 0;
        DamagePopup oldDamage = target.GetComponentInChildren<DamagePopup>();
        if(oldDamage)
        {
            oldDamage.GetComponent<TextMeshPro>().text = (int.Parse(oldDamage.GetComponent<TextMeshPro>().text) + damageAmount).ToString();
            oldDamage.GetComponent<TextMeshPro>().color = isCriticalHit ? Color.red : new Color(1, .4f, 0, 1);
            oldDamage.GetComponent<TextMeshPro>().fontSize = 2.2f;
            oldDamage.dissapearTimer = .75f;
            if(isCriticalHit)
            {
                DamagePopup critText = Instantiate(GameManager.instance.criticalText, position, Quaternion.identity);
                critText.dissapearTimer = critText.dissapearTimerMax;
                critText.textColor = critText.tmp.color;
            }
            return null;
        }
        
        Transform damagePopupTransform = Instantiate(GameManager.instance.damagePopupPrefab, position + Vector3.right * Mathf.Pow(-1, Random.Range(1,3)), Quaternion.identity);
        DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();
        damagePopup.transform.SetParent(target.transform);
        damagePopup.Setup(damageAmount, position, isCriticalHit, addedValue);

        return damagePopup;
    }
    private void Awake()
    {
        position = Vector3.zero;
        if(isCriticalText)
            moveVector = new Vector3(Random.Range(-1.5f, 1.5f), 1f);
        dissapearTimer = dissapearTimerMax;
        textColor = tmp.color;
    }

    public void Setup(int damageAmount, Vector3 position, bool isCriticalHit, int addedValue)
    {
        tmp.SetText((damageAmount + addedValue).ToString());
        if(!isCriticalHit)
        {
            tmp.fontSize = 2.2f;
            textColor = new Color(1, .4f, 0, 1);
        }
        else
        {
            tmp.fontSize = 2.2f;
            textColor = Color.red;
        }
        dissapearTimer = dissapearTimerMax;

        tmp.color = textColor;
        moveVector = new Vector3(0, 1f);
        tmp.sortingOrder = sortingOrder++;
    }

    private void Update()
    {
        if (transform.parent)
        {
            position += moveVector * 4f * Time.deltaTime;
            transform.position = transform.parent.transform.position + position;
            moveVector -= moveVector * 8f * Time.deltaTime;
        }
        else
        {
            transform.position += moveVector * Time.deltaTime;
            moveVector -= moveVector * 2f * Time.deltaTime;
        }    
        dissapearTimer -= Time.deltaTime;
        if(dissapearTimer < 0)
        {
            float dissapearSpeed = 3f;
            textColor.a -= dissapearSpeed * Time.deltaTime;
            tmp.color = textColor;
              if (textColor.a < 0)
            {
               Destroy(gameObject);
            }
        }
    }

    private void LateUpdate()
    {
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }
}
