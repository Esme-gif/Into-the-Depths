using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//script will allow us to start the game with a number of special charges 
//randomized based on what scene we are in, hopefully preventing errors. 
public class EditorSceneStarter : MonoBehaviour
{
    public ReferenceManager refMan;

    private void Awake()
    {
        switch (ScenePersistence._scenePersist.currentScene)
        {
            case ("Level1E"):
                return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
