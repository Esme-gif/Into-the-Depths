using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class LiminalDialogueTrigger : MonoBehaviour
{
    //needs to check place in game
    //and then load a dialogue based on that. 
    public YarnProgram[] liminalYarns;
    public YarnProgram liminalYarntoShow;

    public ReferenceManager refMan;

    // Start is called before the first frame update
    void Start()
    {
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        

    }
    
    public void StartDialogue()
    {
        refMan.CutsceneDiaRunner.StartDialogue("Start");
    }
}
