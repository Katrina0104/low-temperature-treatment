using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Flower;

public class UsageCase : MonoBehaviour
{
    FlowerSystem flowerSys;
    private string myName;
    private int progress = 0;
    private bool pickedUpTheKey = false;
    private bool isGameEnd = false;
    private bool isLocked = false;

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
                    flowerSys.ReadTextFromResource("NPC_nurse01");
                    progress++; // 避免重複
                    break;
                    /* 可延伸更多進度
                    case 1:
                        flowerSys.ReadTextFromResource("NPC_nurse01_2");
                        progress++;
                        break;
                    */
            }
        }

        if (!isGameEnd)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                flowerSys.Next(); // 下一句
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                flowerSys.Resume(); // 繼續
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
}
