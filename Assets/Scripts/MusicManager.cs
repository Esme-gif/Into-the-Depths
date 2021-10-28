using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    ReferenceManager _refMan;
    [SerializeField] AudioSource exploreSource;
    [SerializeField] AudioSource combatSource;

    bool enemyNearby = false;
    bool enemyNearbyChanged = false;
    bool transitioning = false;
    // Start is called before the first frame update
    void Start()
    {
        _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        StartCoroutine(MusicCheck());
    }

    private void Update()
    {
        if (!_refMan)
        {
            _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        }

        /*if (_refMan.player)
        {
            Debug.Log("found player!");
            // If the previous value of enemyNearby is not equal to the current  
            // calculated value, then the current state of enemyNearby has changed.
            enemyNearbyChanged = (enemyNearby != EnemyNearby());
            enemyNearby = EnemyNearby();

            if (enemyNearbyChanged)
            {
                transitioning = true;
            }

            if (enemyNearby && transitioning)
            {
                StartCoroutine(MusicTransitionExploretoCombat());
                transitioning = false;
            }
            else if (!enemyNearby && transitioning)
            {
                StartCoroutine(MusicTransitionCombattoExplore());
                transitioning = false;
            }


        }*/
    }

    IEnumerator MusicCheck()
    {
        while (true)
        {
            Debug.Log("running coroutine");

            if (_refMan.player)
            {
                // If the previous value of enemyNearby is not equal to the current  
                // calculated value, then the current state of enemyNearby has changed.
                enemyNearbyChanged = (enemyNearby != EnemyNearby());
                enemyNearby = EnemyNearby();

                if (enemyNearbyChanged)
                {
                    transitioning = true;
                }

                if (enemyNearby && transitioning)
                {
                    StartCoroutine(MusicTransitionExploretoCombat());
                    transitioning = false;
                }
                else if (!enemyNearby && transitioning)
                {
                    StartCoroutine(MusicTransitionCombattoExplore());
                    transitioning = false;
                }

            }
            yield return new WaitForSecondsRealtime(1f);

        }
    }

    IEnumerator MusicTransitionExploretoCombat()
    {
        for (int i = 10; i >= 0; i -= 1)
        {
            float ft = (float)i / 10;
            exploreSource.volume = ft;
            combatSource.volume = 1 - ft;
            yield return new WaitForSeconds(.1f);
        }
    }

    IEnumerator MusicTransitionCombattoExplore()
    {
        for (int i = 10; i >= 0; i -= 1)
        {
            float ft = (float)i / 10;
            exploreSource.volume = 1 - ft;
            combatSource.volume = ft;
            yield return new WaitForSeconds(.1f);
        }
    }
    private bool EnemyNearby()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(_refMan.player.transform.position, 8f);

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            if (nearbyColliders[i].gameObject.tag == "EnemyHitbox")
            {
                return true;
            }
        }
        return false;
    }
}
