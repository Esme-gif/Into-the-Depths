using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemySpawner : MonoBehaviour
{
    public GameObject player;
    public GameObject enemy;
    public Transform spawnPoint;
    public int spawnTime;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnEnemyTimer());
    }

    IEnumerator SpawnEnemyTimer()
    {
        while (true)
        {//started spawn timer
            yield return new WaitForSeconds(spawnTime);
            GameObject newEnemy = Instantiate(enemy, spawnPoint.transform.position, Quaternion.identity);
        }

    }

    public void ResetSecene()
    {
        SceneManager.LoadScene(1);
    }

    public void Quit()
    {
        Quit();
    }
}
