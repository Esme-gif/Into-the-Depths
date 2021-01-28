using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    [SerializeField] ReferenceManager refMan;

    public void LoadFirstScene()
    {
        Singleton._singleton.lastScene = "Menu";
        if (Singleton._singleton.selectedChar == "Elias")
        {
            Singleton._singleton.currentScene = "Level 1E";
            SceneManager.LoadScene(2);
        }
        else if (Singleton._singleton.selectedChar == "Nichelle")
        {
            Singleton._singleton.currentScene = "Level 1N";
            SceneManager.LoadScene(3); //needs to be a different scene, eventually
        }
        else
        {
            Debug.Log("please select a character to begin");
        }
    }

    public void LoadNextScene()
    {
        Debug.Log("gonna load next scene, babey");
        switch (Singleton._singleton.currentScene)
        {
            case "Level 1E":
            case "Level 1N":
            case "Level 2E":
            case "Level 2N":
                Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                Singleton._singleton.currentScene = "Liminal";
                SceneManager.LoadScene(1);
                break;
            case "Level 3E":
                Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                if (Singleton._singleton.selectedChar == "Elias")
                {
                    Singleton._singleton.currentScene = "Liminal";
                    SceneManager.LoadScene(1); // go to liminal and then to 3N
                }
                else
                {
                    Singleton._singleton.currentScene = "Boss";
                    SceneManager.LoadScene(8); //go to boss chamber
                }
                break;
            case "Level 3N":
                Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                if (Singleton._singleton.selectedChar == "Elias")
                {
                    Singleton._singleton.currentScene = "Boss";
                    SceneManager.LoadScene(8); // go to boss chamber
                }
                else
                {
                    Singleton._singleton.currentScene = "Liminal";
                    SceneManager.LoadScene(1); // go to liminal and then to 3E
                }
                break;
            case "Liminal":
                switch (Singleton._singleton.lastScene)
                {
                    case "Level 1E":
                        Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                        if (Singleton._singleton.selectedChar == "Elias")
                        {
                            Singleton._singleton.currentScene = "Level 1N";
                            SceneManager.LoadScene(3); // go to level 1N
                        }
                        else
                        {
                            Singleton._singleton.currentScene = "Level 2N";
                            SceneManager.LoadScene(5); // go to level 2N
                        }
                        break;
                    case "Level 1N":
                        Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                        if (Singleton._singleton.selectedChar == "Elias")
                        {
                            Singleton._singleton.currentScene = "Level 2E";
                            SceneManager.LoadScene(4); // go to level 2E
                        }
                        else
                        {
                            Singleton._singleton.currentScene = "Level 1E";
                            SceneManager.LoadScene(2); // go to level 1E
                        }
                        break;
                    case "Level 2E":
                        Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                        if (Singleton._singleton.selectedChar == "Elias")
                        {
                            Singleton._singleton.currentScene = "Level 2N";
                            SceneManager.LoadScene(5); // go to level 2N
                        }
                        else
                        {
                            Singleton._singleton.currentScene = "Level 3N";
                            SceneManager.LoadScene(7); // go to level 3N
                        }
                        break;
                    case "Level 2N":
                        Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                        if (Singleton._singleton.selectedChar == "Elias")
                        {
                            Singleton._singleton.currentScene = "Level 3E";
                            SceneManager.LoadScene(6); // go to level 3E
                        }
                        else
                        {
                            Singleton._singleton.currentScene = "Level 2E";
                            SceneManager.LoadScene(4); // go to level 2E
                        }
                        break;
                    case "Level 3E":
                        Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                        Singleton._singleton.currentScene = "Level 3N";
                        SceneManager.LoadScene(7); // go to level 3N
                        break;
                    case "Level 3N":
                        Singleton._singleton.lastScene = Singleton._singleton.currentScene;
                        Singleton._singleton.currentScene = "Level 3E";
                        SceneManager.LoadScene(6); // go to level 3E
                        break;

                }
                break;
            default:
                Debug.LogWarning("No currentScene Entered!!");
                break;
        }

    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
