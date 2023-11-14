using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ButtonRemap : MonoBehaviour
{
    //PlayerControls controls;
    DataHolder data;

    public Image[] buttons;
    public TextMeshProUGUI[] texts;
    [SerializeField] private InputActionReference[] action = new InputActionReference[8];
    [SerializeField] private GameObject startRebindObject = null;
    [SerializeField] private GameObject waitingForInputObject = null;
    private bool operationStarted;
    public InputActionAsset controls;

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private void Start()
    {
        data = DataHolder.instance;
    }
    public void StartRebinding(int index)
    {
        startRebindObject.SetActive(false);
        waitingForInputObject.SetActive(true);

        //data.PlayerInput.SwitchCurrentActionMap("Menu");

        action[index].action.ApplyBindingOverride(0, "<Gamepad>/buttonSouth");


        RebindComplete(index);
       /* rebindingOperation = action[index].action.PerformInteractiveRebinding()
            .WithControlsExcluding("Touchscreen")
            .OnMatchWaitForAnother(.1f)
            .OnComplete(operation => RebindComplete(index))
            .Start();*/
    }

    private void RebindComplete(int index)
    {
        startRebindObject.SetActive(true);
        waitingForInputObject.SetActive(false);

        texts[index].text = InputControlPath.ToHumanReadableString(
            action[index].action.bindings[0].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);



        data.controls.Enable();
        //data.PlayerInput.SwitchCurrentActionMap("Gameplay");
        operationStarted = false;
    }

    public void Save()
    {
       
    }
    public enum DesiredButton
    {
        square,
        triangle,
        circle,
        x,
        options,
        share,
        dleft,
        dup,
        dright,
        ddown,
        l1,
        l2,
        r1,
        r2
    };
    public DesiredButton desiredButton;

    public void OpenControlRemapMenu(int index)
    {
        if (!operationStarted)
        {
            operationStarted = true;
            data.controls.Disable();
            StartRebinding(index);
        }
    }

}
