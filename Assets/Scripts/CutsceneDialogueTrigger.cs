using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneDialogueTrigger : MonoBehaviour
{
    public ReferenceManager refMan;
    public bool hasTriggered;
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
        if (!hasTriggered)
        {
            //opn dialogue UI
             refMan.dialogueManager.OpenCutsceneDialogueUI();
            refMan.CutsceneDiaRunner.StartDialogue("Start");
            refMan.gameManager.PauseGame();
             hasTriggered = true;
            //could destroy game object?
        }
        
    }
}
