using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class CutsceneDialogueTrigger : MonoBehaviour
{
    public ReferenceManager refMan;
    public bool hasTriggered;
    public string dialogueNodeName;

    // Start is called before the first frame update
    void Start()
    {
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.transform.parent != null)
        {
            if (!hasTriggered && collision.transform.parent.tag == "Player")
            {
            //opn dialogue UI
            refMan.dialogueManager.OpenCutsceneDialogueUI();
            int nextCutsceneIndex = refMan.dialogueManager.NextCutsceneIndex();
            //refMan.CutsceneDiaRunner.StartDialogue(dialogueNodeName);
            refMan.CutsceneDiaRunner.StartDialogue(refMan.dialogueManager.cutsceneNodeNames[nextCutsceneIndex]);
            refMan.gameManager.PauseGame();
             hasTriggered = true;
            refMan.dialogueManager.cutsceneTriggered[nextCutsceneIndex] = true;
            //could destroy game object?
            }

        }
        
        
    }
}
