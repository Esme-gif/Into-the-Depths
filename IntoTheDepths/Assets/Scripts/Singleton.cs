using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton : MonoBehaviour
{
    public static Singleton _singleton;

    public string selectedChar;
    public int specialCharges;
    public string lastScene;
    public string currentScene;

    private void Awake()
    {
        if(_singleton == null)
        {
            DontDestroyOnLoad(gameObject);
            _singleton = this;
        }
        else if(_singleton != this)
        {
            Destroy(gameObject);
        }
    }

}
