using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Sign : Collectible
{
    PlayerController p;
    private float glitchTimer = 2f;
    public Animator anim;
    public TextMeshPro indication;
    public string text;
    public bool shouldActivateFunction;
    public bool hasActivatedFunction;
    public Image indicationImage;
    public Room room;
    [SerializeField]
    public GameObject controllerLayout;

    private enum FunctionOption
    {
        functionA,
        functionB,
        functionC
    };
    [SerializeField]
    private FunctionOption selectedFunction;
    private Dictionary<FunctionOption, string> functionLookup;

    private void Awake()
    {
        functionLookup = new Dictionary<FunctionOption, string>()
        {
            { FunctionOption.functionA, "FunctionA"},
            { FunctionOption.functionB, "FunctionB"},
            { FunctionOption.functionC, "FunctionC"}
        };
    }
    private void Start()
    {
        StartCoroutine(Glitch());
        if(!string.IsNullOrEmpty(text))
            indication.text = text;
        indication.enabled = false;
        p = PlayerController.instance;
    }
    public void ActivateSelectedFunction()
    {
        StartCoroutine(functionLookup[selectedFunction]);//.Invoke();
    }

    private IEnumerator FunctionA()
    {
        room.waves = 0;
        room.roomValue = 15;
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        PlayerController p = PlayerController.instance;
        indicationImage.transform.position = p.fireButton.transform.position;
        indicationImage.enabled = true;
        p.movementJoystick.OnPointerUp(null);
        p.movementJoystick.gameObject.SetActive(false);
        p.moveDirection = Vector2.zero;

        //p.movementJoystick.gameObject.SetActive(false);

        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0);
        foreach (Enemy enemy in enemies)
        {
            enemy.transform.position -= Vector3.up * 3;
            enemy.GetComponentsInChildren<Collider2D>()[1].enabled = true;
        }
        while (true)
        {
            if (Time.timeScale > 0.1f)
                Mathf.Clamp(Time.timeScale -= Time.unscaledDeltaTime,0, 1);
            else
                Time.timeScale = 0;
            if (p.isShooting)
            {
                Time.timeScale = 1;
                p.movementJoystick.gameObject.SetActive(true);

                foreach (Enemy enemy in enemies)
                {                   
                    enemy.enabled = true;
                    enemy.ChooseBehaviour(1);
                }
                indicationImage.enabled = false;
                p.canMove = true;
                break;
            }
            yield return wait;
        }
    }
    private IEnumerator FunctionB()
    {
        controllerLayout.SetActive(true);

        yield return null;

    }
    private IEnumerator FunctionC()
    {
        Debug.Log("C");

        yield return null;

    }

    IEnumerator Glitch()
    {
        yield return new WaitForSeconds(glitchTimer);
        anim.SetTrigger("glitch");
        glitchTimer = Random.Range(1, 5f);
        StartCoroutine(Glitch());
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            indication.enabled = true;
            if (shouldActivateFunction)
            {
                PlayerController.instance.interactableObject = this;
                PlayerController.instance.interactButton.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            indication.enabled = false;
            if (shouldActivateFunction)
            {
                PlayerController.instance.interactableObject = null;
                PlayerController.instance.interactButton.gameObject.SetActive(false);
            }
        }
    }

    public override void UnlockCollectible()
    {
        controllerLayout.SetActive(true);
    }
}
