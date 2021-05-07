using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimOrb : MonoBehaviour
{
    PlayerScript _player;
    [SerializeField] Projectile _projectile;
    SpriteRenderer _mySR;
    public float radius = 0.5f; //radial distance from player
    

    public Vector2 lastDirection;

    // Start is called before the first frame update
    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
        _player.currentState = PlayerScript.playerState.Aiming;
        _mySR = GetComponent<SpriteRenderer>();
        transform.position = _player.transform.position + (new Vector3(0,1) * radius);
        lastDirection = new Vector3(0, 1);
    }

    // Update is called once per frame
    void Update()
    {
        RotateOrb();

        if (Input.GetButtonUp("StrongAttack"))
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward: Vector3.forward, 
                upwards: lastDirection);
            Projectile newProj = Instantiate(_projectile, transform.position, targetRotation);

            newProj.direction = lastDirection;
            _player.currentState = PlayerScript.playerState.Idling;
            Destroy(gameObject);
        }
    }

    //rotates orb around player according to axis input
    void RotateOrb()
    {
        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");

        var offset = new Vector3(xInput, yInput);

        if (offset != Vector3.zero)
        {
            offset.Normalize();
            lastDirection = offset;
            offset *= radius;
            transform.position = _player.transform.position + offset;
        }

        //changes the sprite to go behind the player if above 
        if(yInput > 0)
        {
            _mySR.sortingOrder = -1;
        }
        else
        {
            _mySR.sortingOrder = 1;
        }
    }
}
