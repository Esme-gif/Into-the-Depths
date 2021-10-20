/************************************************************************
 * Written by Nicholas Mirchandani on 4/25/2021                         *
 *                                                                      *
 * The purpose of Enemy.cs is to provide a parent class for all         *
 * enemies so that in the future, when enemies need to be aggregated,   *
 * they can be stored in a List<Enemy> or similar data structure.       *
 *                                                                      *
 ************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class Enemy : MonoBehaviour {
    public int framesBetweenAIChecks = 3;
    [SerializeField] protected FSM enemyBrain;
    protected static int curID = 0;
    protected int enemyID;

    protected int layerMask;
    protected int wallMask;
    protected int enemyMask;

    protected Rigidbody2D rb2d;
    protected Animator animator;
    protected GameObject player;
    protected ReferenceManager _refMan;

    protected bool hasStarted = false; //Useful for drawGizmos

    [Header("Stats")]
    public float health;
    public float maxHealth;
    public float defense;
    public float attackDamage;
    public float staggerTime;
    public float knockBackForce;

    [SerializeField] Slider healthSlider;
    public int myListIndex;
    private float sliderCurrentPos;

    public bool knockedBack = false;

    public CinemachineImpulseSource _myImpulseSource;
    public Vector2 towardsPlayer;


    //A method for initializations that don't need to clutter up the individual enemy implementations
    protected void InitializeEnemy() {
        //Set Enemy ID
        enemyID = curID;
        curID += 1;

        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        layerMask = LayerMask.GetMask("Hitbox", "Map");
        wallMask = LayerMask.GetMask("Map");
        enemyMask = LayerMask.GetMask("Enemies");

        hasStarted = true;

        //TODO: Instead of doing this do something with ReferenceManager/PlayerScript as a singleton.
        player = GameObject.FindGameObjectWithTag("Player"); //find player game object

        //health bar stuff
        _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        _refMan.enemies.Add(this);
        myListIndex = _refMan.enemies.Count;
        healthSlider = _refMan.playspaceUIManager.SpawnEnemyHealthSlider(transform.position);
        healthSlider.value = health;
        maxHealth = health;
       
    }

    private void Start()
    {
        _myImpulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Update()
    {
    }

    public virtual void CollisionMovementDetection() { }

    public void TakeDamage(float amount)
    {
        health -= (amount - defense);
        if(amount > 0)
        {
            StopAllCoroutines(); // Interrupting anything and everything with the stagger call
            StartCoroutine(Stagger());
            animator.SetTrigger("Stagger");
            healthSlider.value = health;
            healthSlider.value = Mathf.Clamp(health / maxHealth, 0, 1);
            //needs to stop movement 
            knockedBack = true;
            StartCoroutine(KnockBackTimer());
            //rb2d.AddForce(-towardsPlayer.normalized * knockBackForce);
            //rb2d.AddForce(new Vector3(1,0) * knockBackForce);
            _myImpulseSource.GenerateImpulse();
        }
        if (health <= 0)
        {
            SpawnAshes();
            Destroy(healthSlider.gameObject);
            _refMan.enemies.Remove(this);
            _refMan.playspaceUIManager.enemyHealthSliderRt.Remove(healthSlider.GetComponent<RectTransform>());
            Destroy(gameObject);
        }
    }

    public virtual void SpawnAshes()
    {
        
    }

    protected virtual IEnumerator Stagger() {
        Debug.Log("Unimplemented Stagger!");
        yield return null;
    }

    //timer for how long emeny is knocked back after being hit
    IEnumerator KnockBackTimer()
    {
        Debug.Log("coroutine started for knockback");
        yield return new WaitForSeconds(.14f);
        knockedBack = false;
        rb2d.velocity = Vector2.zero; //return velocity to zero!
    }

}
