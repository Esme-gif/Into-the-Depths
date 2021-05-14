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
public class Ashes2 : MonoBehaviour
{
    public float despawnDuration; // total time to despawn
    public float currentPos; // current value/ position
    public float despawnRate = 1;
    public float acceptHealRate;
    public float acceptHealAmount;

    ReferenceManager _refMan;

    [SerializeField] Slider ashesDespawnSlider;
    public int myListIndex;

    public bool playerIsNear;
    bool isAccepting;

    // Start is called before the first frame update
    void Start()
    {
        currentPos = despawnDuration; //start at max

        _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        myListIndex = _refMan.enemyAshes.Count;
        _refMan.enemyAshes.Add(gameObject);
        ashesDespawnSlider = _refMan.playspaceUIManager.SpawnEnemyAshesSlider(myListIndex);
    }

    // Update is called once per frame
    void Update()
    {
        currentPos -= despawnRate *Time.deltaTime;

        ashesDespawnSlider.value = Mathf.Clamp(currentPos / despawnDuration, 0, 1);

        if (playerIsNear)
        {
            if (Input.GetButton("Block"))
            {
                _refMan.player.ChangePlayerHealth(acceptHealAmount * acceptHealRate * Time.deltaTime);
            }
        }


        if(currentPos <= 0)
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
        if (collision.tag == "playerHitbox")
        {
            playerIsNear = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "playerHitbox")
        {
            playerIsNear = false;
        }
    }

}

