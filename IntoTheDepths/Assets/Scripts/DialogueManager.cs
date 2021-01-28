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

    // Start is called before the first frame update
    void Start()
    {
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        if (SceneManager.GetActiveScene().buildIndex != 0)
        { StartSetUI(); }
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
    [YarnCommand("SetPlayerSprite")]
    public void SetPlayerSprite(string playingCharacter)
    {
        if (playingCharacter == "Elias")
        {
            //change left sprite to be elias
            refMan.leftCharSprite.sprite = eliasMainSprite;
            //change right sprite to be nichelle
            refMan.rightCharSprite.sprite = nichelleMainSprite;
            //change option buttons to elias
            foreach (Button optionButton in refMan.cutsceneDialogueUI.optionButtons)
            {
                optionButton.GetComponent<Image>().sprite = eliasSpeechBubble;
            }
        }
        else
        {
            //change left sprite to be nichelle
            refMan.leftCharSprite.sprite = nichelleMainSprite;
            //change right to elias
            refMan.rightCharSprite.sprite = eliasMainSprite;
            //change option buttons to Nichelle
            foreach (Button optionButton in refMan.cutsceneDialogueUI.optionButtons)
            {
                optionButton.GetComponent<Image>().sprite = nichSpeechBubble;
            }
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
            switch (Singleton._singleton.lastScene)
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
}
