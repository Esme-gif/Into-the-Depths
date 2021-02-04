using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    [SerializeField] ReferenceManager refMan;

    public void LoadFirstScene()
    {
        ScenePersistence._scenePersist.lastScene = "Menu";
        if (ScenePersistence._scenePersist.selectedChar == "Elias")
        {
            ScenePersistence._scenePersist.currentScene = "Level 1E";
            SceneManager.LoadScene(2);
        }
        else if (ScenePersistence._scenePersist.selectedChar == "Nichelle")
        {
            ScenePersistence._scenePersist.currentScene = "Level 1N";
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
        switch (ScenePersistence._scenePersist.currentScene)
        {
            case "Level 1E":
            case "Level 1N":
            case "Level 2E":
            case "Level 2N":
                ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                ScenePersistence._scenePersist.currentScene = "Liminal";
                SceneManager.LoadScene(1);
                break;
            case "Level 3E":
                ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                if (ScenePersistence._scenePersist.selectedChar == "Elias")
                {
                    ScenePersistence._scenePersist.currentScene = "Liminal";
                    SceneManager.LoadScene(1); // go to liminal and then to 3N
                }
                else
                {
                    ScenePersistence._scenePersist.currentScene = "Boss";
                    SceneManager.LoadScene(8); //go to boss chamber
                }
                break;
            case "Level 3N":
                ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                if (ScenePersistence._scenePersist.selectedChar == "Elias")
                {
                    ScenePersistence._scenePersist.currentScene = "Boss";
                    SceneManager.LoadScene(8); // go to boss chamber
                }
                else
                {
                    ScenePersistence._scenePersist.currentScene = "Liminal";
                    SceneManager.LoadScene(1); // go to liminal and then to 3E
                }
                break;
            case "Liminal":
                switch (ScenePersistence._scenePersist.lastScene)
                {
                    case "Level 1E":
                        ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                        if (ScenePersistence._scenePersist.selectedChar == "Elias")
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 1N";
                            SceneManager.LoadScene(3); // go to level 1N
                        }
                        else
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 2N";
                            SceneManager.LoadScene(5); // go to level 2N
                        }
                        break;
                    case "Level 1N":
                        ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                        if (ScenePersistence._scenePersist.selectedChar == "Elias")
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 2E";
                            SceneManager.LoadScene(4); // go to level 2E
                        }
                        else
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 1E";
                            SceneManager.LoadScene(2); // go to level 1E
                        }
                        break;
                    case "Level 2E":
                        ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                        if (ScenePersistence._scenePersist.selectedChar == "Elias")
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 2N";
                            SceneManager.LoadScene(5); // go to level 2N
                        }
                        else
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 3N";
                            SceneManager.LoadScene(7); // go to level 3N
                        }
                        break;
                    case "Level 2N":
                        ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                        if (ScenePersistence._scenePersist.selectedChar == "Elias")
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 3E";
                            SceneManager.LoadScene(6); // go to level 3E
                        }
                        else
                        {
                            ScenePersistence._scenePersist.currentScene = "Level 2E";
                            SceneManager.LoadScene(4); // go to level 2E
                        }
                        break;
                    case "Level 3E":
                        ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                        ScenePersistence._scenePersist.currentScene = "Level 3N";
                        SceneManager.LoadScene(7); // go to level 3N
                        break;
                    case "Level 3N":
                        ScenePersistence._scenePersist.lastScene = ScenePersistence._scenePersist.currentScene;
                        ScenePersistence._scenePersist.currentScene = "Level 3E";
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
