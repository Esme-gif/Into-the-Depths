using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string objName;
    bool playerIsNear;
    [SerializeField] PlayerScript player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("BaseAttack") && playerIsNear)
        {
            switch (objName)
            {
                case "stairs":
                    player._refMan.mySceneManager.LoadNextScene();
                    break;
                case "spark":
                    player._refMan.dialogueManager.LiminalDiaTrigger();
                    break;
                case "fountain":
                    player.ChangePlayerHealth(player.maxHealth);
                    break;
            }

        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("stairs collision!");
        if(collision.transform.parent.tag == "Player")
        {
            playerIsNear = true;
            if (player == null)
            {
                player = collision.gameObject.GetComponentInParent<PlayerScript>();
            }
            player.isByInteractable = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.parent.tag == "Player")
        {
            playerIsNear = false;
            if (player == null)
            {
                player = collision.gameObject.GetComponentInParent<PlayerScript>();
            }
            player.isByInteractable = false;
        }
    }
}
