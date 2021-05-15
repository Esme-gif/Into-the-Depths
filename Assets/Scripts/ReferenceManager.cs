using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ReferenceManager : MonoBehaviour
{
    public GameManager gameManager;
    public DialogueManager dialogueManager;
    public PlayerScript player;
    public MySceneManager mySceneManager;
    public PlayspaceUIManager playspaceUIManager;

    public GameObject CutsceneDialogueUIGO;
    public DialogueUI cutsceneDialogueUI;
    public DialogueRunner CutsceneDiaRunner;
    public Image speechBubble;
    public Image leftCharSprite;
    public Image rightCharSprite;
    public OptionButton optionButton1;
    public OptionButton optionButton2;
    public OptionButton optionButton3;

    public LiminalSceneStarter _limSceneStarter;

    public List<GameObject> enemyAshes = new List<GameObject>();
    public List<EnemyScript> enemies;

    private void Awake()
    {
        GetReferences();
    }
    public void GetReferences()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            player = playerGO.GetComponent<PlayerScript>();
            CutsceneDiaRunner = GameObject.FindGameObjectWithTag("CutsceneDiaRunner").GetComponent<DialogueRunner>();

            if (CutsceneDialogueUIGO == null)
            {
                CutsceneDialogueUIGO = GameObject.FindGameObjectWithTag("CutsceneDiaUI");
                cutsceneDialogueUI = CutsceneDialogueUIGO.GetComponent<DialogueUI>();
            }
            CutsceneDialogueUIGO.SetActive(true);
            speechBubble = CutsceneDialogueUIGO.transform.Find("TextBubble").GetComponent<Image>();
            leftCharSprite = CutsceneDialogueUIGO.transform.Find("CharSprites").
                transform.Find("leftCharSprite").GetComponent<Image>();
            rightCharSprite = CutsceneDialogueUIGO.transform.Find("CharSprites").
                transform.Find("rightCharSprite").GetComponent<Image>();
            GameObject optionButtons = CutsceneDialogueUIGO.transform.Find("Option Buttons").gameObject;
            optionButton1 = optionButtons.transform.Find("optionButton1").GetComponent<OptionButton>();
            optionButton2 = optionButtons.transform.Find("optionButton2").GetComponent<OptionButton>();
            optionButton3 = optionButtons.transform.Find("optionButton3").GetComponent<OptionButton>();

            CutsceneDialogueUIGO.SetActive(false);

            if (SceneManager.GetActiveScene().buildIndex == 1)
            {
                _limSceneStarter = GameObject.FindGameObjectWithTag("SceneStarter").GetComponent<LiminalSceneStarter>();
            }
            else
            {
                playspaceUIManager = GameObject.FindGameObjectWithTag("PlayspaceUI").GetComponent<PlayspaceUIManager>();
                playspaceUIManager._refMan = this;
            }

            foreach( GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            {
                enemies.Add(enemy.GetComponent<EnemyScript>());
            }
        }
    }
}
