using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    ReferenceManager refMan;

    public GameObject CutDialogueUI;
    public DialogueRunner CutsceneDialogueRunner;

    public Image speechBubble;
    public Sprite eliasSpeechBubble;
    public Sprite nichSpeechBubble;
    public Sprite monSpeechBubble;

    // Start is called before the first frame update
    void Start()
    {
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        StartSetUI();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenCutsceneDialogueUI()
    {
        CutDialogueUI.SetActive(true);
        CutsceneDialogueRunner.StartDialogue("Start");
    }

    private void StartSetUI()
    {
        CutDialogueUI.SetActive(false);
    }

    [YarnCommand("ChangeSpeechBubble")]
    public void ChangeSpeechBubble(string talkingCharacter)
    {
        if(talkingCharacter == "Elias")
        {
            speechBubble.sprite = eliasSpeechBubble;
        }
        else if(talkingCharacter == "Nichelle")
        {
            speechBubble.sprite = nichSpeechBubble;
        }
        else if(talkingCharacter == "Monster")
        {
            speechBubble.sprite = monSpeechBubble;
        }
    }
}
