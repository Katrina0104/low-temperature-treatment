using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Flower;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class UsageCase : MonoBehaviour
{
    FlowerSystem flowerSys;
    private string myName;
    private int progress = 0;
    private bool isGameEnd = false;
    private bool isLocked = false;

    // VR devices
    [SerializeField] InputActionReference rightTriggerAction;
    [SerializeField] InputActionReference buttonAAction; // A 按鈕
    [SerializeField] InputActionReference buttonBAction; // B 按鈕
    bool triggerPressedLastFrame;
    private bool canAcceptVRInput = true;

    void Start()
    {
        var audioDemoFile = Resources.Load<AudioClip>("bgm") as AudioClip;
        if (!audioDemoFile)
        {
            Debug.LogWarning("The audio file : 'bgm' is necessary for the demonstration. Please add to the Resources folder.");
        }

        // 建立 FlowerSystem
        if (FlowerManager.Instance.HasFlowerSystem("FlowerSample"))
            FlowerManager.Instance.RemoveFlowerSystem("FlowerSample");

        flowerSys = FlowerManager.Instance.CreateFlowerSystem("FlowerSample", false);
        flowerSys.SetupDialog();
        //flowerSys.SetupButton();
        flowerSys.SetupUIStage();

        // 將 Nurse01 注入 FlowerSystem
        Nurse01_controller nurse = GameObject.Find("Nurse01")?.GetComponent<Nurse01_controller>();
        if (nurse != null)
        {
            nurse.SetFlowerSystem(flowerSys);
        }

        // Setup 變數
        myName = "Rempty (｢･ω･)｢";
        flowerSys.SetVariable("MyName", myName);

        // 註冊自訂指令和特效
        flowerSys.RegisterCommand("UsageCase", CustomizedFunction);
        flowerSys.RegisterEffect("customizedRotation", EffectCustomizedRotation);

        // VR
        SetupVRInput();
    }

    void OnEnable()
    {
        if (rightTriggerAction?.action != null)
        {
            rightTriggerAction.action.Enable();
            Debug.Log("RightTrigger Action Enabled");
        }
        else
        {
            Debug.LogWarning("RightTrigger Action Reference is null on Enable!");
        }

        if (buttonAAction?.action != null)
        {
            buttonAAction.action.Enable();
            Debug.Log("Button A Action Enabled");
        }

        if (buttonBAction?.action != null)
        {
            buttonBAction.action.Enable();
            Debug.Log("Button B Action Enabled");
        }
    }

    void OnDisable()
    {
        if (rightTriggerAction?.action != null)
        {
            rightTriggerAction.action.Disable();
            Debug.Log("RightTrigger Action Disabled");
        }

        if (buttonAAction?.action != null)
        {
            buttonAAction.action.Disable();
            Debug.Log("Button A Action Disabled");
        }

        if (buttonBAction?.action != null)
        {
            buttonBAction.action.Disable();
            Debug.Log("Button B Action Disabled");
        }
    }

    void OnDestroy()
    {
        if (rightTriggerAction?.action != null)
        {
            rightTriggerAction.action.started -= OnRightTriggerStarted;
        }

        if (buttonAAction?.action != null)
        {
            buttonAAction.action.started -= OnButtonAStarted;
        }

        if (buttonBAction?.action != null)
        {
            buttonBAction.action.started -= OnButtonBStarted;
        }
    }

    void Update()
    {
        if (flowerSys == null) return;

        // 進度控制訊息播放
        if (flowerSys.isCompleted && !isGameEnd && !isLocked)
        {
            switch (progress)
            {
                case 0:
                    flowerSys.ReadTextFromResource("NPC_nurse01(1)");
                    break;

                case 1:
                    SetupYesNoButtons("NPC_nurse01(2)", "retry", 2);
                    break;

                case 2:
                    SetupYesNoButtons("NPC_nurse01(3)", "retry", 3);
                    break;

                case 3:
                    SetupYesNoButtons("NPC_nurse01(4)", "retry", 4);
                    break;
            }
            progress++;
        }

        if (!isGameEnd)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Continue the messages, stoping by [w] or [lr] keywords.
                flowerSys.Next();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                flowerSys.Resume();
            }
        }
    }
    void SetupYesNoButtons(string yesResource, string noResource, int nextProgress)
    {
        canAcceptVRInput = true;
        flowerSys.SetupButtonGroup();

        flowerSys.SetupButton("Yes", () =>
        {
            flowerSys.Resume();
            flowerSys.RemoveButtonGroup();
            flowerSys.ReadTextFromResource(yesResource);
            progress = nextProgress;
        });

        flowerSys.SetupButton("No", () =>
        {
            flowerSys.Resume();
            flowerSys.RemoveButtonGroup();
            flowerSys.ReadTextFromResource(noResource);
        });
    }

// nurse animation when talk
    private void CustomizedFunction(List<string> _params)
    {
        var resultValue = int.Parse(_params[0]) + int.Parse(_params[1]);
        Debug.Log($"Hi! This is called by CustomizedFunction with the result of parameters : {resultValue}");
    }

    IEnumerator CustomizedRotationTask(string key, GameObject obj, float endTime)
    {
        Vector3 startRotation = obj.transform.eulerAngles;
        Vector3 endRotation = obj.transform.eulerAngles + new Vector3(0, 180, 0);
        yield return flowerSys.EffectTimerTask(key, endTime, (percent) =>
        {
            obj.transform.eulerAngles = Vector3.Lerp(startRotation, endRotation, percent);
        });
    }

    private void EffectCustomizedRotation(string key, List<string> _params)
    {
        try
        {
            float endTime = float.Parse(_params[0]) / 1000f;
            GameObject sceneObj = flowerSys.GetSceneObject(key);
            StartCoroutine(CustomizedRotationTask($"CustomizedRotation-{key}", sceneObj, endTime));
        }
        catch (Exception)
        {
            Debug.LogError($"Effect - CustomizedRotation @ [{key}] failed.");
        }
    }

// VR
    void SetupVRInput()
    {
        if (rightTriggerAction?.action != null)
        {
            rightTriggerAction.action.started += OnRightTriggerStarted;
            rightTriggerAction.action.Enable();
        }

        if (buttonAAction?.action != null)
        {
            buttonAAction.action.started += OnButtonAStarted;
            buttonAAction.action.Enable();
        }

        if (buttonBAction?.action != null)
        {
            buttonBAction.action.started += OnButtonBStarted;
            buttonBAction.action.Enable();
        }
    }

    private void OnRightTriggerStarted(InputAction.CallbackContext context)
    {
        if (!canAcceptVRInput || flowerSys == null || isGameEnd || isLocked) return;

        if (context.ReadValue<float>() > 0.1f)
        {

            HandleVRTriggerPress();
        }
    }

    private void HandleVRTriggerPress()
    {
        if (flowerSys != null && canAcceptVRInput && !isGameEnd && !isLocked)
        {
            flowerSys.Next();
        }
    }
    private void OnButtonAStarted(InputAction.CallbackContext context)
    {
        if (!canAcceptVRInput || flowerSys == null || isGameEnd || isLocked) return;

        // A 按鈕對應 No
        if (progress >= 2 && progress <= 5) // 在需要選擇的進度範圍內
        {
            HandleButtonA();
        }
    }

    private void OnButtonBStarted(InputAction.CallbackContext context)
    {
        if (!canAcceptVRInput || flowerSys == null || isGameEnd || isLocked) return;

        // B 按鈕對應 Yes
        if (progress >= 2 && progress <= 5) // 在需要選擇的進度範圍內
        {
            HandleButtonB();
        }
    }

    private void HandleButtonA()
    {
        // A 按鈕對應 No
        switch (progress - 1)
        {
            case 1:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("retry");
                progress = 1;
                canAcceptVRInput = true;
                break;
            case 2:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("retry");
                progress = 1;
                canAcceptVRInput = true;
                break;
            case 3:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("retry");
                progress = 1;
                canAcceptVRInput = true;
                break;
            case 4:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("retry");
                progress = 1;
                canAcceptVRInput = true;
                break;
        }
    }

    private void HandleButtonB()
    {
        // B 按鈕對應 Yes
        switch (progress - 1)
        {
            case 1:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("NPC_nurse01(2)");
                progress = 2;
                canAcceptVRInput = true;
                break;
            case 2:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("NPC_nurse01(3)");
                progress = 3;
                canAcceptVRInput = true;
                break;
            case 3:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("NPC_nurse01(4)");
                progress = 4;
                canAcceptVRInput = true;
                break;
            case 4:
                flowerSys.Resume();
                flowerSys.RemoveButtonGroup();
                flowerSys.ReadTextFromResource("NPC_nurse01(5)");
                progress = 5;
                canAcceptVRInput = true;
                break;
        }
    }
}