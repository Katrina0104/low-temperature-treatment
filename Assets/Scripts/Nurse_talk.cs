using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Nurse_talk : MonoBehaviour
{
    public GameObject dialogueBox;
    public TMP_Text dialogueText;
    private bool isDialogueActive = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ShowDialogueWithDelay(1f));
    }

    // Update is called once per frame
    void Update()
    {
        // 如果玩家按下空白鍵，隱藏對話框
        if (Input.GetKeyDown(KeyCode.Space))
        {
            dialogueBox.SetActive(false); // 隱藏對話框
            isDialogueActive = false;
        }
    }

    IEnumerator ShowDialogueWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dialogueText != null)
        {
            dialogueText.text = "把手上的病歷與病床尾端的病人資料進行核對確定是否為患者本人";
        }
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }
        isDialogueActive = true;
    }
}
