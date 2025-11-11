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
    bool triggerPressedLastFrame;
    bool rightTriggerEnabled = false;
    private bool canAcceptVRInput = true; // 新增：控制是否接受VR輸入
    private bool triggerPressed = false;

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
            rightTriggerEnabled = true;
            Debug.Log("RightTrigger Action Enabled");
        }
        else
        {
            Debug.LogWarning("RightTrigger Action Reference is null on Enable!");
        }
    }

    void OnDisable()
    {
        if (rightTriggerAction?.action != null)
        {
            rightTriggerAction.action.Disable();
            Debug.Log("RightTrigger Action Disabled");
        }
    }
    void OnDestroy()
    {
        if (rightTriggerAction?.action != null)
        {
            rightTriggerAction.action.started -= OnRightTriggerStarted;
            rightTriggerAction.action.canceled -= OnRightTriggerCanceled;
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
                    // 當顯示按鈕時，暫時禁用VR輸入
                    canAcceptVRInput = false;
                    flowerSys.SetupButtonGroup();
                    flowerSys.SetupButton("Yes", () => {
                        flowerSys.Resume();
                        flowerSys.RemoveButtonGroup();
                        flowerSys.ReadTextFromResource("NPC_nurse01(2)");
                        progress = 2;
                        canAcceptVRInput = true; // 重新啟用VR輸入
                    });
                    flowerSys.SetupButton("No", () => {
                        flowerSys.Resume();
                        flowerSys.RemoveButtonGroup();
                        flowerSys.ReadTextFromResource("retry");
                        progress = 1;
                        canAcceptVRInput = true; // 重新啟用VR輸入
                    });
                    break;

                case 2:
                    // 當顯示按鈕時，暫時禁用VR輸入
                    canAcceptVRInput = false;
                    flowerSys.SetupButtonGroup();
                    flowerSys.SetupButton("Yes", () => {
                        flowerSys.Resume();
                        flowerSys.RemoveButtonGroup();
                        flowerSys.ReadTextFromResource("NPC_nurse01(3)");
                        progress = 2;
                        canAcceptVRInput = true; // 重新啟用VR輸入
                    });
                    flowerSys.SetupButton("No", () => {
                        flowerSys.Resume();
                        flowerSys.RemoveButtonGroup();
                        flowerSys.ReadTextFromResource("retry");
                        progress = 1;
                        canAcceptVRInput = true; // 重新啟用VR輸入
                    });
                    break;
            }
            progress++;
        }

        if (!isGameEnd)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                flowerSys.Resume();
            }
        }
    }
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
    void SetupVRInput()
    {
        if (rightTriggerAction?.action != null)
        {
            rightTriggerAction.action.started += OnRightTriggerStarted;
            rightTriggerAction.action.canceled += OnRightTriggerCanceled;
            rightTriggerAction.action.Enable();
        }
    }
    private void OnRightTriggerStarted(InputAction.CallbackContext context)
    {
        if (!canAcceptVRInput || flowerSys == null || isGameEnd || isLocked) return;

        if (context.ReadValue<float>() > 0.1f)
        {
            triggerPressed = true;
            HandleVRTriggerPress();
        }
    }

    private void OnRightTriggerCanceled(InputAction.CallbackContext context)
    {
        triggerPressed = false;
    }

    private void HandleVRTriggerPress()
    {
        if (flowerSys != null && canAcceptVRInput && !isGameEnd && !isLocked)
        {
            flowerSys.Next();
        }
    }
}