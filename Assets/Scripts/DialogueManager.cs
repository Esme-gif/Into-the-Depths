using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    ReferenceManager refMan;
    
    public Sprite eliasSpeechBubble;
    public Sprite nichSpeechBubble;
    public Sprite monSpeechBubble;
    [SerializeField] Sprite eliasMainSprite;
    [SerializeField] Sprite nichelleMainSprite;
    [SerializeField] Sprite ratMainSprite;

    public string[] cutsceneNodeNames;
    public bool[] cutsceneTriggered;

    // Start is called before the first frame update
    void Start()
    {
        refMan = GetComponent<ReferenceManager>();
        if (SceneManager.GetActiveScene().buildIndex != 0)
        { StartSetUI(); }
        cutsceneTriggered = new bool[cutsceneNodeNames.Length];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenCutsceneDialogueUI()
    {
        refMan.CutsceneDialogueUIGO.SetActive(true);
        
    }

    public void CloseCutsceneDialogueUI()
    {
        refMan.CutsceneDialogueUIGO.SetActive(false);
        refMan.gameManager.PauseGame();
    }

    private void StartSetUI()
    {
        refMan.CutsceneDialogueUIGO.SetActive(false);
    }

    [YarnCommand("ChangeSpeechBubble")]
    public void ChangeSpeechBubble(string talkingCharacter)
    {
        if(talkingCharacter == "Elias")
        {
            refMan.speechBubble.sprite = eliasSpeechBubble;
        }
        else if(talkingCharacter == "Nichelle")
        {
            refMan.speechBubble.sprite = nichSpeechBubble;
        }
        else if(talkingCharacter == "Monster")
        {
            refMan.speechBubble.sprite = monSpeechBubble;
        }
    }
    [YarnCommand("SetSprites")]
    public void SetSprites(string actorL, string actorR)
    {
        refMan.rightCharSprite.color = Color.white;

        if (actorL == "Elias") //left side should be reserved for the player, so it will only ever be N or E
        {
            //change left sprite to be elias
            refMan.leftCharSprite.sprite = eliasMainSprite;
            
            //change option buttons to elias
            foreach (Button optionButton in refMan.cutsceneDialogueUI.optionButtons)
            {
                optionButton.GetComponent<Image>().sprite = eliasSpeechBubble;
            }

        }
        else if (actorL == "Nichelle")
        {
            //change left sprite to be nichelle
            refMan.leftCharSprite.sprite = nichelleMainSprite;
            //change option buttons to Nichelle
            foreach (Button optionButton in refMan.cutsceneDialogueUI.optionButtons)
            {
                optionButton.GetComponent<Image>().sprite = nichSpeechBubble;
            }
            
        }

        switch (actorR)
        {
            case "Elias":
                refMan.rightCharSprite.sprite = eliasMainSprite;
                break;
            case "Nichelle":
                refMan.rightCharSprite.sprite = nichelleMainSprite;
                break;
            case "None":
                refMan.rightCharSprite.color = Color.clear;
                break;
            case "Rat":
                refMan.rightCharSprite.sprite = ratMainSprite;
                break;
        }
    }

    [YarnCommand("SetOptionValue")]
    public void SetOptionValue(string optionNum, string value)
    {
        if (GameManager.canSpecial)
        {
            
            if (int.TryParse(optionNum, out int intOptNum))
            {
                if (int.TryParse(value, out int intValue))
                {
                    if (intOptNum == 1)
                    {
                        refMan.optionButton1.value = intValue;
                        Debug.Log("set Option1 value to " + value);
                    }
                    else if (intOptNum == 2)
                    {
                        refMan.optionButton2.value = intValue;
                        Debug.Log("set option2 value to " + value);
                    }
                    else if (intOptNum == 3)
                    {
                        refMan.optionButton3.value = intValue;
                        Debug.Log("set option 3 value to " + value);
                    }
                }
                else
                {
                    Debug.Log("could not parse " + value);
                }
            }
            else
            {
                Debug.Log("coudl not parse option number " + optionNum);
            }
        }
        
    }

    public void LiminalDiaTrigger()
    {
        if (!refMan._limSceneStarter.hasSeenLimDialogue)
        {
            refMan._limSceneStarter.hasSeenLimDialogue = true;
            refMan.gameManager.PauseGame();
            OpenCutsceneDialogueUI();
            switch (ScenePersistence._scenePersist.lastScene)
            {
                case "Level 1E":
                    refMan.CutsceneDiaRunner.StartDialogue("Discuss_1E");
                    break;
                case "Level 1N":
                    refMan.CutsceneDiaRunner.StartDialogue("Discuss_1N");
                    break;
                case "Level 2E":
                    refMan.CutsceneDiaRunner.StartDialogue("Discuss_2E");
                    break;
                case "Level 2N":
                    refMan.CutsceneDiaRunner.StartDialogue("Discuss_2N");
                    break;
                case "Level 3E":
                    refMan.CutsceneDiaRunner.StartDialogue("Discuss_3E");
                    break;
                case "Level 3N":
                    refMan.CutsceneDiaRunner.StartDialogue("Discuss_3N");
                    break;

            }
        }
    }

    public int NextCutsceneIndex()
    {
        int indx = 100;
        for (int i = 0; i < cutsceneNodeNames.Length; i++)
        {
            if (cutsceneTriggered[i] == false)
            {
                indx = i;
                break;
            }
        }

        if(indx == 100)
        {
            Debug.Log("Dialogue Manager ran out of Cutscenes!");
        }

        Debug.Log("Dialogue Manager found index: " + indx);
        return indx;
    }
}
