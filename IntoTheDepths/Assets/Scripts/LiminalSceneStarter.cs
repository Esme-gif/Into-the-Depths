using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiminalSceneStarter : MonoBehaviour
{
    public GameObject playerElias;
    public GameObject playerNichelle;

    public bool hasSeenLimDialogue;

    // Start is called before the first frame update
    void Start()
    {
        if(Singleton._singleton.lastScene == "") //for testing purposes, allows us to load liminal directly
        {
            Singleton._singleton.lastScene = "Level 1E";
            Singleton._singleton.currentScene = "Liminal";
        }
        string whoseLevel = Singleton._singleton.lastScene.Substring(7, 1);
        if(whoseLevel == "N")
        {
            playerElias.SetActive(true);
            playerNichelle.SetActive(false);
        }
        else
        {
            playerNichelle.SetActive(true);
            playerElias.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
