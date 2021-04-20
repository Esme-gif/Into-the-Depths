using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenePersistence : MonoBehaviour
{
    public static ScenePersistence _scenePersist;

    public string selectedChar;
    public int specialCharges;
    public string lastScene;
    public string currentScene;

    private void Awake()
    {
        if(_scenePersist == null)
        {
            DontDestroyOnLoad(gameObject);
            _scenePersist = this;
        }
        else if(_scenePersist != this)
        {
            Destroy(gameObject);
        }
    }

}
