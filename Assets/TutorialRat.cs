using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialRat : MonoBehaviour
{
    EnemyRat myMainScript;

    // Start is called before the first frame update
    void Start()
    {
        myMainScript = GetComponent<EnemyRat>();
    }
    
    //public void FTUEAttack()
    //{
    //    myMainScript.enemyBrain.applyTransition((uint)RatActions.IN_ATTACK_RANGE);
    //}

}
