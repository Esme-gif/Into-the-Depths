using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Menu : MonoBehaviour
{
    public Image beginButton;

    [SerializeField] GameObject mainMenu;
    [SerializeField] GameObject charSelectScreen;

    ReferenceManager refMan;

    public void Start()
    {
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        mainMenu.SetActive(true);
        charSelectScreen.SetActive(false);
    }

    public void PickCharacter(string name)
    {
        Singleton._singleton.selectedChar = name;
        beginButton.color = new Color(beginButton.color.r, beginButton.color.b, beginButton.color.g, 1); ;
    }

    public void OpenCharSelect()
    {
        mainMenu.SetActive(false);
        charSelectScreen.SetActive(true);
    }
}
