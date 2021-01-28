using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public ReferenceManager refMan;

    public static bool gameIsPaused;
    public static bool canSpecial = true;

    public string selectedChar;

    [SerializeField]GameObject specialSegment;
    // Start is called before the first frame update
    void Start()
    {
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();

        SetSpecialBarSize();
    }
    
    public void SetSpecialBarSize()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            GameObject specialSliderBG = refMan.player.specialSlider.gameObject.transform.
                Find("Background").gameObject;
            RectTransform specialSliderRT = refMan.player.specialSlider.GetComponent<RectTransform>();
            for (int i = 0; i < Singleton._singleton.specialCharges; i++)
            {
                Instantiate(specialSegment, specialSliderBG.transform);
                refMan.player.specialSlider.GetComponent<RectTransform>().sizeDelta =
                 new Vector2(15 * Singleton._singleton.specialCharges,specialSliderRT.sizeDelta.y);
                    
            }
        }
    }

    public void IncreaseSpecialBarSize(int amount)
    {
        GameObject specialSliderBG = refMan.player.specialSlider.gameObject.transform.
                Find("Background").gameObject;
        RectTransform specialSliderRT = refMan.player.specialSlider.GetComponent<RectTransform>();
        for (int i = 0; i < amount; i++)
        {
            Instantiate(specialSegment, specialSliderBG.transform);
            refMan.player.specialSlider.GetComponent<RectTransform>().sizeDelta =
             new Vector2(15 * Singleton._singleton.specialCharges, specialSliderRT.sizeDelta.y);

        }
    }

    public void PauseGame()
    {
        Debug.Log("called pause game");
        gameIsPaused = !gameIsPaused;
        if (gameIsPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1;
        }
    }
}
