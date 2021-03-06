using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine;

public class PlayerScript : MonoBehaviour
{

    [Header("Object References")]
    public ReferenceManager _refMan;
    [SerializeField] AimOrb _aimOrb;

    //object references
    [SerializeField] Slider healthSlider = null; //player health slider UI
    public Slider specialSlider = null;

    //component references 
    public Rigidbody2D rb; //player's rigidbody
    public Animator myAnimator; //player's animator component
    //Animator myChildAnimator; //player childs' collidor animation component
    public SpriteRenderer playerSpriteRen; //player's image component to change sprite information 
    public CapsuleCollider2D _playerWallCollider;

    //states
    [Header("States")]
    public bool testingTriggerSpecial; //for testing purposes: if true, q or e keys trigger timed special buff
    public bool testingAcceptSpecial; //for testing purposes: if true, accept mechanic rewards brief buff using special code
    bool canTakeDamage = true; //used for invincibility frames (npt currently implemented)
    public bool canBlockHeal = false; //marks the time frame for block heal
    public bool isByInteractable;
    bool specialOn;
    bool specialRecharging = false;
    bool canStrongAttack = true;
    public bool canAttackClick = true;
    public enum playerState
    {
        Idling,
        Moving,
        Attacking,
        Blocking,
        Dashing,
        Aiming,
        Staggered,
        Frozen //for cutscene animation purposes
    }
    public playerState currentState;


    //stats
    [Header("Character Stats")]
    public float health;
    public float maxHealth = 50;
    public float damage = 5; //amound of damage the player does
    public float currentDefense = 0;
    public float baseDefense = 0;
    public float moveSpeed; //player movement speed
    Vector3 cameraOffset = new Vector3(0, 0, -10); //distance of camera from ground
    float combo1Timer = 1f; //time allowed to achieve combo - time between non-combo hits
    public float attackSpeedDecay; //amount player movement speed slows when attacking
    public float blockingDefense; //damage reduction by blocking - TO DO - Make factor of damage taken, not static
    public float blockHealAmount; //PERCENTAGE amount recovered by blocking with good timing - should currently always be at zero as we are scrapping blockheal
    public float dashSpeed;
    public float attackNudge;
    public float dashTime; // amount of time dash lasts for
    public float specialSpeedIncrease;
    public float specialDashSpdIncrease;
    public float specialDamageIncrease;
    public float specialBlockIncrease;
    public float specialDefenseIncrease; // need to actually make a defense stat.
    public float specialTime;
    public float specialRechargeTime;
    [SerializeField] float specialAnimSpeed;
    public float acceptSpecialTime;
    public float projectileDMG;
    public float stATKRecharge;

    [Header("Stored Information (debug)")]
    //stored/cached information    
    public Vector2 FacingDirection; //direction the player sprite is facing
    public int attackComboCounter = 0; //used to track place in combo
    public float lastAttackedTime = 0; //time when attack button was last pressed
    public float horizontalInput;
    public float verticalInput; //store input
    public float defaultSpeed; //used to restore speed to normal after attack speed decay
    string nearbyInteractable; //name of the near interactable object, usually fountain or spark.
    float specialEnded;
    float rechargePlace;
    float specialStartingTime;
    
    //should be obsolete below
    public  List<GameObject> weaponsGOThatHitPlayer = new List<GameObject>(); //a list of objects that have hit the player
                                                                              // public List<AnimationState> playerAnimStates = new List<AnimationState>();
    public List<GameObject> hitGOs = new List<GameObject>(); //list of objects the player has hit 
    int input; //1 keyboard/ mouse 2 - gamepad 

    [SerializeField] AnimationClip standardAttackAnim;
    public AnimationClip idleAnim;
    Coroutine returnToIdleCo = null;
    public float attackAnimLength;

    Vector2 prevPosition;
    int outBoundDamage;
    // Start is called before the first frame update
    void Start()
    {
        attackAnimLength = standardAttackAnim.length / 1.6f;
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        //myChildAnimator = transform.GetChild(0).GetComponent<Animator>(); //for some reason, GetComponentinChildren doesn't work
        playerSpriteRen = GetComponent<SpriteRenderer>();
        _playerWallCollider = transform.GetChild(0).GetComponent<CapsuleCollider2D>();
        defaultSpeed = moveSpeed; //set default movespeed so it can be reset after attacking/dashing
        _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        FacingDirection = new Vector2(0, 1);
    }

    private void FixedUpdate()
    {
        if (currentState != playerState.Blocking && currentState != playerState.Dashing
            && currentState != playerState.Aiming && currentState != playerState.Staggered
            && currentState != playerState.Frozen)
        {
            Move();
        }
        MoveCamera();//always update the camera
        
    }

    // Update is called once per frame
    void Update()
    {
        if(currentState != playerState.Frozen)
        {
            if (currentState != playerState.Dashing)
            {
                // if not in a dash, always check for attacking, blocking, and animation changes
                if (!isByInteractable)// another bool

                {
                    if (Input.GetButtonDown("BaseAttack"))
                    {
                        input = 1;
                        Attack();
                    }
                    else if (Input.GetButtonDown("BaseAttackGP"))
                    {
                        input = 2;
                        Attack();
                    }
                }
                AnimBlendSetFloats();
                Block();
                Special();
            }
            Dash(); //always be checking for dash input

            if (canStrongAttack)
            {
                StrongAttack();
            }

        }
        
    }

    //when detecting a trigger collision (most likely by an enemy weapon)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "EnemyHitbox")
        {
            if (!hitGOs.Contains(collision.gameObject))
            {
                Debug.Log("hit an enemy!");
                hitGOs.Add(collision.gameObject);
                Enemy enemyInfo = collision.GetComponentInParent<Enemy>(); //get enemy information
                enemyInfo.TakeDamage(damage);
            }
        }
        
    }

    //called in Update
    //special buff when player presses both triggers
    private void Special()
    {
        if (GameManager.canSpecial && testingTriggerSpecial)
        {
            float maxSpecialTime = specialTime + ScenePersistence._scenePersist.specialCharges;

            if (!specialOn && !specialRecharging)
            {
                if (Input.GetButtonDown("Special1") || Input.GetButtonDown("Special2"))
                {
                    Debug.Log("hit special");
                    Buff();
                }
            }
            else if (specialOn && !specialRecharging)
            {
                specialSlider.value = Mathf.Clamp((maxSpecialTime -
                    (Time.time - specialStartingTime)) /
                    maxSpecialTime, 0, 1);
            }
            else if (!specialOn && specialRecharging) //during recharge
            {
                rechargePlace += Time.deltaTime;

                specialSlider.value = Mathf.Clamp((rechargePlace - specialEnded) /
                    (specialRechargeTime + ScenePersistence._scenePersist.specialCharges), 0, 1);

                if (rechargePlace - specialEnded >=
                    specialRechargeTime + ScenePersistence._scenePersist.specialCharges)
                {
                    specialRecharging = false;
                    Debug.Log("special recharged!");
                }
            }
        }
    }
    
    void StrongAttack()
    {
        if (Input.GetButtonDown("StrongAttack"))
        {
            //instantiate aim orb
            Instantiate(_aimOrb, transform);
            currentState = playerState.Aiming;
            canStrongAttack = false;
            StartCoroutine(StrongAttackResetTimer());
        }
    }

    IEnumerator SpecialTimer(float storedMoveSpd, float storedDmg, float storedDashSpd,
        float storedBlockRed, float storedDefens)
    {
        myAnimator.speed = specialAnimSpeed;
        if (testingTriggerSpecial)
        {
            Debug.Log("started special timer");
            yield return new WaitForSeconds(specialTime + ScenePersistence._scenePersist.specialCharges); //placeholder amount, will be based on charges
            
        }
        else if (testingAcceptSpecial)
        {
            yield return new WaitForSeconds(acceptSpecialTime); 

        }
        //return stats to normal
        defaultSpeed = storedMoveSpd;
        damage = storedDmg;
        dashSpeed = storedDashSpd;
        blockingDefense = storedBlockRed;
        currentDefense = storedDefens;
        myAnimator.speed = 1;
        Debug.Log("finished special timer");
        specialOn = false;
        specialRecharging = true;
        specialEnded = Time.time;
        rechargePlace = Time.time;
    }

    public void Buff()
    {
        specialStartingTime = Time.time;
        specialOn = true;
        //increase stats
        float storedMoveSpd = moveSpeed;
        defaultSpeed = moveSpeed + specialSpeedIncrease;
        float storedDamage = damage;
        damage += specialDamageIncrease;
        float storedDashSpd = dashSpeed;
        dashSpeed += specialDashSpdIncrease;
        float storedBlockRed = blockingDefense;
        blockingDefense += specialBlockIncrease;
        float storedDefense = currentDefense;
        currentDefense += specialDefenseIncrease;
        //don't have a defense stat yet. 
        //start timer
        StartCoroutine(SpecialTimer(storedMoveSpd, storedDamage, storedDashSpd,
            storedBlockRed, storedDefense));
    }

    //Called in Update()
    //player dash
    private void Dash()
    {

        if (Input.GetButtonDown("Dash"))
        {
            currentState = playerState.Dashing;
            StartCoroutine(ReturntoIdleTimer(dashTime)); //start timer for amount of time player can dash
        }
        if (currentState == playerState.Dashing)
        {
            //while player is dashing, move position faster in the last direction there was input about
            rb.MovePosition(rb.position + (FacingDirection.normalized * dashSpeed) /** Time.deltaTime*/);
           
        }
    }

    //called in Update()
    //player block
    private void Block()
    {
        if (Input.GetButtonDown("Block")) //if right click down
        {
            //player is blocking
            currentState = playerState.Blocking;
            myAnimator.SetBool("Blocking", true); //set animator 
            currentDefense = blockingDefense;
        }
        if (Input.GetButtonUp("Block")) //if right click up
        {
            //player stops blocking
            currentState = playerState.Idling;
            myAnimator.SetBool("Blocking", false);
            currentDefense = baseDefense;
        }
    }

    // moves the player sprite, called in fixed update
    private void Move()
    {
        if(currentState == playerState.Attacking) //if attacking, slow movement speed every frame until at 0
        {
            moveSpeed -= attackSpeedDecay;
            moveSpeed = Mathf.Clamp(moveSpeed, 0, defaultSpeed);
        }
        else
        {
            //if not attacking, ensure movement speed is normal speed.
            moveSpeed = defaultSpeed;
        }

        //store player input data
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //create a vector based off input data
        Vector2 movement = new Vector2(horizontalInput, verticalInput);
        //movement = Vector2.ClampMagnitude(movement, 1); // keeps from going faster on diagonal movement
        movement.Normalize(); // just just normalize it you dum dum
        SetFacingDirection(movement);
        movement *= moveSpeed; //multiply movement by speed
        rb.MovePosition(rb.position + movement /* * Time.fixedDeltaTime*/); //move game object via rigidbody
        if(currentState != playerState.Attacking && movement != Vector2.zero
            && currentState != playerState.Aiming)
        {
            currentState = playerState.Moving;
        }
    }

    private void SetFacingDirection(Vector2 movement)
    {
        //if the vector does not equal zero (and therefore the player is moving)
        if (movement != Vector2.zero)
        {
            //set facing direction 
            FacingDirection = movement;
            //trigger animations
            myAnimator.SetBool("Walking", true);
            myAnimator.SetBool("Idling", false);
        }
        else //the player is standing still
        {
            //trigger animations
            myAnimator.SetBool("Walking", false);
            myAnimator.SetBool("Idling", true);
        }
    }

    private void Attack()
    {
        if (canAttackClick)
        {
            int animStateTag = myAnimator.GetCurrentAnimatorStateInfo(0).tagHash;
            bool attacked = false;

            if (animStateTag != Animator.StringToHash("attack1")
                && animStateTag != Animator.StringToHash("attack2")
                && animStateTag != Animator.StringToHash("attack3"))
            {
                attackComboCounter = 1;
                attacked = true;
            }
            else if (animStateTag == Animator.StringToHash("attack1"))
            {
                attackComboCounter = 2;
                attacked = true;
                canAttackClick = false;

            }
            else if (animStateTag == Animator.StringToHash("attack2"))
            {
                attackComboCounter = 3;
                attacked = true;
                canAttackClick = false; 
            }
            if (attacked)
            {
                if (input == 1)//if using keyboard/mouse input
                {
                    Vector2 vToMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
                    vToMousePos.Normalize();
                    myAnimator.SetFloat("AttackFaceX", vToMousePos.x);
                    myAnimator.SetFloat("AttackFaceY", vToMousePos.y);
                    PlayBlendTreeAttackAnims();

                }
                else if (input == 2) // if using gamepad
                {
                    myAnimator.SetFloat("AttackFaceX", FacingDirection.x);
                    myAnimator.SetFloat("AttackFaceY", FacingDirection.y);
                    PlayBlendTreeAttackAnims();
                }
                
            }
        }
        else
        {
            Debug.Log("blocked an attack w canattackclick");
        }
    }

    void PlayBlendTreeAttackAnims()
    {
        if (attackComboCounter == 1)
        {
            myAnimator.SetTrigger("Attack1");
        }
        else if(attackComboCounter == 2)
        {
            myAnimator.SetTrigger("Attack2");
        }
        else if(attackComboCounter == 3)
        {
            myAnimator.SetTrigger("Attack3");
        }
    }

    public void AttackNudge() //called by animator
    {
        if(input == 1)
        {
            Vector2 vToMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            rb.AddForce(vToMousePos.normalized * attackNudge);
        }
        else
        {
            rb.AddForce(FacingDirection.normalized * attackNudge);
        }
    }

    //removes player go from list of go that have hit each enemy at the end of player's attack
    public void ClearHitGOList() // called by animator at end of every attack
    {
        hitGOs.Clear();
    }

    //called in Update
    //sets animation parameters so blend trees for walk and idle animations 
    //know the facing direction. 
    private void AnimBlendSetFloats()
    {
        myAnimator.SetFloat("FaceX", FacingDirection.x);
        myAnimator.SetFloat("FaceY", FacingDirection.y);
    }

    //currently replaced with cinemachine
    private void MoveCamera()
    {
        //Camera.main.transform.position = transform.position + cameraOffset; //move camera with player
    }

    //IEnumerator ReturntoIdleTimer(float time)
    //{
    //    Physics2D.IgnoreLayerCollision(14, 11, true);
    //    _playerWallCollider.enabled = false; 
    //    Debug.Log("layer collision off");
    //    yield return new WaitForSeconds(time);
    //    _playerWallCollider.enabled = true;
    //    Physics2D.IgnoreLayerCollision(14, 11, false);
    //    Debug.Log("layer collision on");
    //    currentState = playerState.Idling;
    //}
    IEnumerator ReturntoIdleTimer(float time)
    {
        //Debug.Log(transform.GetChild(0).GetComponent<CapsuleCollider2D>());
        //Debug.Log(Physics2D.IsTouchingLayers(transform.GetChild(0).GetComponent<CapsuleCollider2D>(), LayerMask.GetMask("Map")));
        if (Physics2D.IsTouchingLayers(transform.GetChild(0).GetComponent<CapsuleCollider2D>(), LayerMask.GetMask("Map_2")))
        {
            Debug.Log("dash in bound");
            prevPosition = transform.position; //updates when before dash in boundary
        }

        //for(int i =0;i< (Physics2D.OverlapCircleAll(transform.position, 2f).Length); i++)
        //{
        //    if(Physics2D.OverlapCircleAll(transform.position, 2f)[i].gameObject.name == "Grid")
        //    {
        //        Debug.Log("Dash started ");
        //    }
        //}
        Physics2D.IgnoreLayerCollision(14, 11, true);
        _playerWallCollider.enabled = false;
        //Debug.Log("layer collision off");
        yield return new WaitForSeconds(time);


        _playerWallCollider.enabled = true;
        Physics2D.IgnoreLayerCollision(14, 11, false);
        //Debug.Log("layer collision on");
        currentState = playerState.Idling;
        //Debug.Log(Physics2D.IsTouchingLayers(transform.GetChild(0).GetComponent<CapsuleCollider2D>(), LayerMask.GetMask("Map")));
        //if (!Physics2D.IsTouchingLayers(transform.GetChild(0).GetComponent<CapsuleCollider2D>(), LayerMask.GetMask("Map_2")))
        //{
        //    Debug.Log("dash not in bound");
        //    //StartCoroutine(Fall());
        //}

        ////Collider2D foundCollider  = _playerWallCollider.OverlapCollider(ContactFilter2D.no).

        //bool foundGround = false;

        //for (int i = 0; i < (Physics2D.OverlapCircleAll(transform.position, 2f).Length); i++)
        //{
        //    Debug.Log(Physics2D.OverlapCircleAll(transform.position, 2f)[i].gameObject.name);
        //    if (Physics2D.OverlapCircleAll(transform.position, 2f)[i].gameObject.name == "Grid")
        //    {

        //        foundGround = true;
        //        break;
        //    }
        //}
        //if (foundGround)
        //{
        //    Debug.Log("yes");
        //}
        //else
        //{
        //    Debug.Log("no");
        //}

    }

    IEnumerator Fall()
    {
        yield return new WaitForSeconds(3f);
        ChangePlayerHealth(outBoundDamage, "hit"); // damage for being out of bound
        transform.position = prevPosition; //respawn in last position in bound
        Debug.Log("out of bound taking damage");
    }

    IEnumerator StrongAttackResetTimer()
    {
        yield return new WaitForSeconds(stATKRecharge);
        canStrongAttack = true;
    }

    public void ChangePlayerHealth(float amount, string dmgType)
    {
        if (amount < 0)
        {
            if(dmgType == "hit")//in case we want some DoT effects that blocking won't help with
            {
                amount += currentDefense;
                int animStateTag = myAnimator.GetCurrentAnimatorStateInfo(0).tagHash;

                if (animStateTag != Animator.StringToHash("attack1")
                    && animStateTag != Animator.StringToHash("attack2")
                    && animStateTag != Animator.StringToHash("attack3")
                    && currentState != playerState.Staggered)
                {
                    myAnimator.SetTrigger("Stagger"); //playerstate.stagger set in animation event
                }
            }
        }

        health += amount;
        healthSlider.value = Mathf.Clamp(health / maxHealth, 0, 1);
        if (health <= 0) //if health is less than zero
        {
            //die
            ResetScene();
        }
    }

    private void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


}

