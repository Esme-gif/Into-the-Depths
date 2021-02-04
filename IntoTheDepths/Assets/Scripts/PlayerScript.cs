using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
    [Header("Object References")]
    public ReferenceManager refMan;
     
    //object references
    [SerializeField] Slider healthSlider = null; //player health slider UI
    public Slider specialSlider = null;

    //component references 
    Rigidbody2D rb; //player's rigidbody
    Animator myAnimator; //player's animator component
    Animator myChildAnimator; //player childs' collidor animation component
    SpriteRenderer playerSpriteRen; //player's image component to change sprite information 

    //states
    [Header("States")]
    public bool canStrike2 = false; //used by coroutine timer to check for second hit in combo
    public bool canStrike3 = false; // "" but for third
    public bool attack1AnimPlaying = false; //is the first attack animation playing
    public float attackAnim1Length = .3f; //length in seconds of first attack animation
    bool canTakeDamage = true; //used for invincibility frames (npt currently implemented)
    bool isAttacking = false; //is the player in an attack animation
    public bool isBlocking = false;
    public bool canBlockHeal = false; //marks the time frame for block heal
    bool isDashing = false;
    public bool isByInteractable;
    bool specialOn;
    bool specialRecharging = false;

    //stats
    [Header("Character Stats")]
    public float health;
    public float maxHealth = 50;
    public float damage = 5; //amound of damage the player does
    public float defense = 0;
    public float moveSpeed; //player movement speed
    Vector3 cameraOffset = new Vector3(0, 0, -10); //distance of camera from ground
    float combo1Timer = 1f; //time allowed to achieve combo - time between non-combo hits
    public float attackSpeedDecay; //amount player movement speed slows when attacking
    public float damageReduction; //damage reduction by blocking - TO DO - Make factor of damage taken, not static
    public float blockHealAmount; //PERCENTAGE amount recovered by blocking with good timing
    public float dashSpeed;
    public float dashTime; // amount of time dash lasts for
    public float specialSpeedIncrease;
    public float specialDashSpdIncrease;
    public float specialDamageIncrease;
    public float specialBlockIncrease;
    public float specialDefenseIncrease; // need to actually make a defense stat.
    public float specialTime;
    public float specialRechargeTime;
    public float imAChange;

    [Header("Stored Information (debug)")]
    //stored/cached information    
    public Vector2 FacingDirection; //direction the player sprite is facing
    public int attackComboCounter = 0; //used to track place in combo
    public float lastAttackedTime = 0; //time when attack button was last pressed
    float horizontalInput;
    float verticalInput; //store input
    float defaultSpeed; //used to restore speed to normal after attack speed decay
    string nearbyInteractable; //name of the near interactable object, usually fountain or spark.
    float specialEnded;
    float rechargePlace;
    float specialStartingTime;
    List<GameObject> goWeaponCollidedWith = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myChildAnimator = transform.GetChild(0).GetComponent<Animator>(); //for some reason, GetComponentinChildren doesn't work
        playerSpriteRen = GetComponent<SpriteRenderer>();
        defaultSpeed = moveSpeed; //set default movespeed so it can be reset after attacking/dashing
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        FacingDirection = new Vector2(0, 1);
    }

    private void FixedUpdate()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Dash(); //always be checking for dash input
        if (!isDashing)
        {
            if (!isBlocking)
            {
                Move();
            }
            // if not in a dash, always check for attacking, blocking, and animation changes
            if (Input.GetButtonUp("BaseAttack") && !isByInteractable)
            {
                Attack();
            }
            AnimBlendSetFloats();
            Block();
            Special();
        }

        MoveCamera();//always update the camera
    }

    //when detecting a trigger collision (most likely by an enemy weapon)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //check that we're not in invincibility frames and the collider is an enemy weapon
        if (collision.tag == "EnemyWeapon")
        {
            if (!goWeaponCollidedWith.Contains(collision.gameObject))
            {
                //add go to list of game objects that have hit the player
                goWeaponCollidedWith.Add(collision.gameObject);

                //take damage
                EnemyScript enemyInfo = collision.GetComponentInParent<EnemyScript>(); //get enemy information
                if (enemyInfo != null) //if successful,
                {
                    if (!isBlocking) //and not blocking,
                    {
                        ChangePlayerHealth(-enemyInfo.damage); //take damage.
                    }
                    else if (isBlocking && !canBlockHeal) //if IS blocking but not 
                    {
                        //calculate new damage - clamp so it is not less than zero
                        float newDamage = Mathf.Clamp(enemyInfo.damage - ((damageReduction * enemyInfo.damage) / 100), 0, enemyInfo.damage);
                        ChangePlayerHealth(-newDamage);
                    }
                }
            }
            else
            {
                Debug.Log("double hit! didn't get through :)");
            }
        }
    }

    //called in Update
    //special buff when player presses both triggers
    private void Special()
    {
        if (GameManager.canSpecial)
        {
            float maxSpecialTime = specialTime + ScenePersistence._scenePersist.specialCharges;
            
            if (!specialOn && !specialRecharging)
            {
                if (Input.GetButtonDown("Special1") || Input.GetButtonDown("Special2"))
                {
                    Debug.Log("hit special");
                    specialStartingTime = Time.time;
                    specialOn = true;
                    //increase stats
                    float storedMoveSpd = moveSpeed;
                    moveSpeed = moveSpeed + specialSpeedIncrease;
                    float storedDamage = damage;
                    damage += specialDamageIncrease;
                    float storedDashSpd = dashSpeed;
                    dashSpeed += specialDashSpdIncrease;
                    float storedBlockRed = damageReduction;
                    damageReduction += specialBlockIncrease;
                    float storedDefense = defense;
                    defense += specialDefenseIncrease;
                    //don't have a defense stat yet. 
                    //start timer
                    StartCoroutine(SpecialTimer(storedMoveSpd, storedDamage, storedDashSpd,
                        storedBlockRed, storedDefense));
                    //change color/start animation
                    playerSpriteRen.color = new Color(1, 0, 0, 1);
                }
            }
            else if (specialOn && !specialRecharging)
            {
                specialSlider.value = Mathf.Clamp((maxSpecialTime -
                    (Time.time - specialStartingTime))/ 
                    maxSpecialTime, 0, 1);
            }
            else if (!specialOn && specialRecharging) //during recharge
            {
                rechargePlace += Time.deltaTime;

                specialSlider.value = Mathf.Clamp((rechargePlace - specialEnded) /
                    (specialRechargeTime + ScenePersistence._scenePersist.specialCharges), 0, 1);

                if(rechargePlace - specialEnded >= 
                    specialRechargeTime + ScenePersistence._scenePersist.specialCharges)
                {
                    specialRecharging = false;
                    Debug.Log("special recharged!");
                }
            }

        

        }
        


    }

    IEnumerator SpecialTimer(float storedMoveSpd, float storedDmg, float storedDashSpd, 
        float storedBlockRed, float storedDefens)
    {
        Debug.Log("started special timer");
        yield return new WaitForSeconds(specialTime + ScenePersistence._scenePersist.specialCharges); //placeholder amount, will be based on charges
        //return stats to normal
        moveSpeed = storedMoveSpd;
        damage = storedDmg;
        dashSpeed = storedDashSpd;
        damageReduction = storedBlockRed;
        defense = storedDefens;
        //return color/animation to normal
        playerSpriteRen.color = new Color(1,1,1);
        Debug.Log("finished special timer");
        specialOn = false;
        specialRecharging = true;
        specialEnded = Time.time;
        rechargePlace = Time.time;
    }

    //Called in Update()
    //player dash
    private void Dash()
    {
        if (Input.GetButtonDown("Dash"))
        {
            isDashing = true;
            StartCoroutine(DashTimer(dashTime)); //start timer for amount of time player can dash
        }
        if (isDashing)
        {
            //while player is dashing, move position faster in the last direction there was input about
            rb.MovePosition(rb.position + (FacingDirection.normalized * dashSpeed) * Time.deltaTime);
        }
    }

    //called in Update()
    //player block
    private void Block()
    {
        if (Input.GetButtonDown("Block")) //if right click down
        {
            //player is blocking
            isBlocking = true;
            myAnimator.SetBool("Blocking", true); //set animator 
            myChildAnimator.SetBool("Blocking", true);//set fake child animator (for sake of lining up animations) 
            if (canBlockHeal == true)
            {
                //if pressed within block heal window (set by attacking enemy), heal player.
                ChangePlayerHealth(blockHealAmount);
            }
        }
        if (Input.GetButtonUp("Block")) //if right click up
        {
            //player stops blocking
            isBlocking = false;
            myAnimator.SetBool("Blocking", false); //return animators to false
            myChildAnimator.SetBool("Blocking", false);
        }
    }

    // moves the player sprite, called in fixed update
    private void Move()
    {
        if (!isAttacking)
        {
            //if not attacking, ensure movement speed is normal speed.
            moveSpeed = defaultSpeed;
        }
        else //if attacking, slow movement speed every frame until at 0
        {
            moveSpeed -= attackSpeedDecay;
            moveSpeed = Mathf.Clamp(moveSpeed, 0, defaultSpeed);
        }

        //store player input data
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        //create a vector based off input data
        Vector2 movement = new Vector2(horizontalInput, verticalInput);
        SetFacingDirection(movement);
        movement *= moveSpeed; //multiply movement by speed
        rb.MovePosition(rb.position + movement * Time.deltaTime); //move game object via rigidbody
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
            myChildAnimator.SetBool("Walking", true);
            myAnimator.SetBool("Idling", false);
            myChildAnimator.SetBool("Idling", false);
        }
        else //the player is standing still
        {
            //trigger animations
            myAnimator.SetBool("Walking", false);
            myChildAnimator.SetBool("Walking", false);
            myAnimator.SetBool("Idling", true);
            myChildAnimator.SetBool("Idling", true);
        }
    }

    //called in update
    private void Attack()
    {
        if (Time.time - lastAttackedTime > combo1Timer)//if a second(combo1Timer time) has passed since the last attack
                                                       //AKA missed combo window
        {
            //allow player to perform strike1 again
            if (attackComboCounter != 0)
            {
                attackComboCounter = 0;
            }
        }

        //if on first hit and not within timer for third hit
        if (attackComboCounter == 0 && !canStrike3)
        {                             //need !canStrike3 because combo counter resets to 0 before strike 3 timer is out
            lastAttackedTime = Time.time; //store time of last attack

            //start timer to track animation legnth - needed to ensure player cannot hit for strike3 during first attack anim
            StartCoroutine(Attack1LengthTimer());
            StartCoroutine(Strike2Timer()); //start timer for second hit
            attackComboCounter = 1; //increase combo counter
            PlayAttackAnimations();
        }
        else if (attackComboCounter == 1 && canStrike2) // if have hit once and within timer for second strike
        {
            lastAttackedTime = Time.time; //store last attacked time

            StartCoroutine(Strike3Timer()); // start timer for third
            attackComboCounter = 2; //increase combo counter
            PlayAttackAnimations(); //play attack 2 animations
        }
        //else if have struck twice (ready for third) and NOT in first attack animation and within window for third hit
        else if (attackComboCounter == 2 && !attack1AnimPlaying && canStrike3)
        {
            lastAttackedTime = Time.time; //store last attacked time
            attackComboCounter = 3;
            PlayAttackAnimations(); //play attack 3 animations
            attackComboCounter = 0; //reset combo counter
        }

        StartCoroutine(AttackingTimer());
        //trying to use method with a ref parameter, but invoke does not allow params
        //isAttacking = true;
        //Invoke("boolSwitchTimer(ref isAttacking)", 0.3f);
    }

    //called in attack()
    // plays attack animations for both player and invisible weapon collider
    private void PlayAttackAnimations()
    {
        if (attackComboCounter == 1)
        {
            //if player is holding down two buttons, moving diagonally
            if (Mathf.Abs(FacingDirection.x) == Mathf.Abs(FacingDirection.y))
            {//prioritize vertical animation
                if (FacingDirection.y > 0)
                {
                    myChildAnimator.SetTrigger("AttackUp");
                    myAnimator.SetTrigger("AttackingUp");
                }
                if (FacingDirection.y < 0)
                {
                    myChildAnimator.SetTrigger("AttackDown");
                    myAnimator.SetTrigger("AttackingDown");
                }
                return; //and stop the method here
            }
            //checks direction player is facing, then triggers animations for that direction.
            if (FacingDirection.x > 0)
            {
                myChildAnimator.SetTrigger("AttackRight");
                myAnimator.SetTrigger("AttackingRight");
            }
            if (FacingDirection.x < 0)
            {
                myChildAnimator.SetTrigger("AttackLeft");
                myAnimator.SetTrigger("AttackingLeft");
            }
            if (FacingDirection.y > 0)
            {
                myChildAnimator.SetTrigger("AttackUp");
                myAnimator.SetTrigger("AttackingUp");
            }
            if (FacingDirection.y < 0)
            {
                myChildAnimator.SetTrigger("AttackDown");
                myAnimator.SetTrigger("AttackingDown");
            }
        }
        if (attackComboCounter == 2)
        {
            if (Mathf.Abs(FacingDirection.x) == Mathf.Abs(FacingDirection.y))
            {
                if (FacingDirection.y > 0)
                {
                    myChildAnimator.SetTrigger("AttackUp2");
                    myAnimator.SetTrigger("AttackingUp2");
                }
                if (FacingDirection.y < 0)
                {
                    myChildAnimator.SetTrigger("AttackDown2");
                    myAnimator.SetTrigger("AttackingDown2");
                }
                return;
            }
            if (FacingDirection.x > 0)
            {
                myChildAnimator.SetTrigger("AttackRight2");
                myAnimator.SetTrigger("AttackingRight2");
            }
            if (FacingDirection.x < 0)
            {
                myChildAnimator.SetTrigger("AttackLeft2");
                myAnimator.SetTrigger("AttackingLeft2");
            }
            if (FacingDirection.y > 0
                && FacingDirection.x < 0.2 && FacingDirection.x > -0.2)
            {
                myChildAnimator.SetTrigger("AttackUp2");
                myAnimator.SetTrigger("AttackingUp2");
            }
            if (FacingDirection.y < 0
                && FacingDirection.x < 0.2 && FacingDirection.x > -0.2)
            {
                myChildAnimator.SetTrigger("AttackDown2");
                myAnimator.SetTrigger("AttackingDown2");
            }

        }
        if (attackComboCounter == 3)
        {
            if (Mathf.Abs(FacingDirection.x) == Mathf.Abs(FacingDirection.y))
            {
                if (FacingDirection.y > 0)
                {
                    myChildAnimator.SetTrigger("AttackUp3");
                    myAnimator.SetTrigger("AttackingUp3");
                }
                if (FacingDirection.y < 0)
                {
                    myChildAnimator.SetTrigger("AttackDown3");
                    myAnimator.SetTrigger("AttackingDown3");
                }
                return;
            }
            if (FacingDirection.x > 0)
            {

                myChildAnimator.SetTrigger("AttackRight3");
                myAnimator.SetTrigger("AttackingRight3");
            }
            if (FacingDirection.x < 0)
            {
                myChildAnimator.SetTrigger("AttackLeft3");
                myAnimator.SetTrigger("AttackingLeft3");
            }
            if (FacingDirection.y > 0
                && FacingDirection.x < 0.2 && FacingDirection.x > -0.2)
            {
                myChildAnimator.SetTrigger("AttackUp3");
                myAnimator.SetTrigger("AttackingUp3");
            }
            if (FacingDirection.y < 0
                && FacingDirection.x < 0.2 && FacingDirection.x > -0.2)
            {
                myChildAnimator.SetTrigger("AttackDown3");
                myAnimator.SetTrigger("AttackingDown3");
            }

        }
    }

    //called in Update
    //sets animation parameters so blend trees for walk and idle animations 
    //know the facing direction. 
    private void AnimBlendSetFloats()
    {
        myAnimator.SetFloat("FaceX", FacingDirection.x);
        myAnimator.SetFloat("FaceY", FacingDirection.y);
    }

    private void MoveCamera()
    {
        Camera.main.transform.position = transform.position + cameraOffset; //move camera with player
        //MESS AROUND WITH THIS
        //Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, transform.position + cameraOffset, 0.03f); //move camera with player

    }
    
    IEnumerator AttackingTimer()
    {
        isAttacking = true;
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    IEnumerator Attack1LengthTimer()
    {
        attack1AnimPlaying = true;
        yield return new WaitForSeconds(attackAnim1Length);
        attack1AnimPlaying = false;
    }

    IEnumerator Strike2Timer()
    {
        canStrike2 = true;
        yield return new WaitForSeconds(0.3f);
        canStrike2 = false;
    }

    IEnumerator Strike3Timer()
    {
        canStrike3 = true;
        yield return new WaitForSeconds(0.3f);
        canStrike3 = false;
    }

    IEnumerator DashTimer(float time)
    {
        yield return new WaitForSeconds(time);
        isDashing = false;
    }

    //resets the list of game objects that have hit the player since an enemy's last attack 
    public IEnumerator ResetWeaponHitGOs()
    {
        yield return new WaitForSeconds(0.3f); //needs to be length of enemy attack anim
        goWeaponCollidedWith.Clear();
    }

    private void boolSwitchTimer(ref bool boolToSwitch)
    {
        boolToSwitch = !boolToSwitch;
    }

    private void Heal(float amount) //increases player health
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth); //don't allow health below zero or above max
        healthSlider.value = Mathf.Clamp(health / maxHealth, 0, 1);
    }

    private void TakeDamage(float amount) //decreases player health
    {
        amount -= defense;
        health -= amount;
        healthSlider.value = Mathf.Clamp(health / maxHealth, 0, 1);
        if (health <= 0) //if health is less than zero
        {
            //die
            ResetScene();
        }
    }

    public void ChangePlayerHealth(float amount)
    {
        if(amount < 0)
        {
            amount -= defense;
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
        SceneManager.LoadScene(0);
    }


}

