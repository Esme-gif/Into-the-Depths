using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Needs a timer that, when spawned, counts down to destroy object
 * Also, when player enters trigger collider and holds down a button 
 * for a certain amount of time, also destroys object
 * some way to indicate which way the object was destroyed for respawning - probably
 * through a list of all enemies in the spawnManager
 * */
public class Ashes : MonoBehaviour
{
    public float despawnDuration; // total time to despawn
    public float currentPos; // current value/ position
    public float despawnRate; 
    public float acceptRate;
    public float minAcceptDuration;
    public float maxAcceptDuration;
    public float initialPos;


    ReferenceManager _refMan;

    [SerializeField]  Slider ashesDespawnSlider;
    public int myListIndex;

    bool playerIsNear;
    bool isAccepting;
    

    // Start is called before the first frame update
    void Start()
    {
        initialPos = 1 - (minAcceptDuration / maxAcceptDuration);
        currentPos = initialPos;
        acceptRate = 1 / maxAcceptDuration;
        despawnRate = initialPos / despawnDuration;


        _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        myListIndex = _refMan.enemyAshes.Count;
        _refMan.enemyAshes.Add(gameObject);
        ashesDespawnSlider = _refMan.playspaceUIManager.SpawnEnemyAshesSlider(myListIndex);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAccepting)
        {
            currentPos -= despawnRate * Time.deltaTime;
        }
        else
        {
            currentPos += acceptRate * Time.deltaTime;
        }

        ashesDespawnSlider.value = currentPos;

        if (playerIsNear)
        {
            if (Input.GetButton("Block"))
            {
                minAcceptDuration -= Time.deltaTime;
                isAccepting = true;
            }
            if (Input.GetButtonUp("Block"))
            {
                isAccepting = false;
            }
        }

        if (currentPos >= 1)
        {
            if (_refMan.player.testingAcceptSpecial)
            {
                _refMan.player.Buff();
            }
            DestroyEverything();
            Debug.Log("accepted! Yay!");
        }

        if (currentPos <= 0)
        {
            DestroyEverything();
        }
    }

    void DestroyEverything()
    {
        _refMan.enemyAshes.Remove(gameObject);
        _refMan.playspaceUIManager.ashesRespawnSliderRt.Remove(ashesDespawnSlider.GetComponent<RectTransform>());
        Destroy(ashesDespawnSlider.gameObject);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        playerIsNear = true;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        playerIsNear = false;
    }

}
