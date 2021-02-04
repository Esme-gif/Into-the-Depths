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
        if(ScenePersistence._scenePersist.lastScene == "") //for testing purposes, allows us to load liminal directly
        {
            ScenePersistence._scenePersist.lastScene = "Level 1E";
            ScenePersistence._scenePersist.currentScene = "Liminal";
        }
        string whoseLevel = ScenePersistence._scenePersist.lastScene.Substring(7, 1);
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
