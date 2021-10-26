using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    ReferenceManager _refMan;
    [SerializeField] AudioSource exploreSource;
    [SerializeField] AudioSource combatSource;

    bool musicTrans =false;
    bool enemyNearby = false;
    // Start is called before the first frame update
    void Start()
    {
        _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        StartCoroutine(MusicCheck());
    }

    // Update is called once per frame
    void Update()
    {
        //Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(_refMan.player.transform.position, 5f);

        //for(int i =0; i<nearbyColliders.Length; i++)
        //{
        //    //Debug.Log(nearbyColliders[i].name);
        //    if(nearbyColliders[i].gameObject.tag == "EnemyHitbox")
        //    {
        //        exploreSource.volume = 0;
        //        combatSource.volume = 1;
        //    }
        //}
    }

    IEnumerator MusicCheck()
    {
        while (true)
        {
            Debug.Log("running coroutine");
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(_refMan.player.transform.position, 5f);

            for (int i = 0; i < nearbyColliders.Length; i++)
            {
                Debug.Log(nearbyColliders[i].name);
                if (nearbyColliders[i].gameObject.tag == "EnemyHitbox" && !enemyNearby)
                {
                    //musicTrans = false;
                    enemyNearby = true;
                    break;
                }
                else
                {
                    enemyNearby = false;
                }
            }
            if(enemyNearby && !musicTrans)
            {
                exploreSource.volume = 0;
                combatSource.volume = 1;
                //StartCoroutine(MusicTransition());
                //musicTrans = true;
            }
            else if(!enemyNearby && !musicTrans)
            {
                exploreSource.volume = 1;
                combatSource.volume = 0;
            }

            yield return new WaitForSecondsRealtime(2f);

        }
    }

    IEnumerator MusicTransition()
    {
        for (float ft = 1f; ft >= 0; ft -= 0.1f)
        {
            exploreSource.volume = ft;
            combatSource.volume = 1-ft;
            yield return new WaitForSeconds(.1f);
        }
    }
}
