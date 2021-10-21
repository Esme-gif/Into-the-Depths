using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateAnimationEventScript : MonoBehaviour
{
    PlayerScript _playerScript;

    // Start is called before the first frame update
    void Start()
    {
        _playerScript = GetComponent<PlayerScript>();
    }

    public void SetPlayerStateAttacking()
    {
        if (_playerScript.currentState != PlayerScript.playerState.Frozen)
        {
            _playerScript.currentState = PlayerScript.playerState.Attacking;
        }
    }
    public void SetPlayerStateIdling()
    {
        if (_playerScript.currentState != PlayerScript.playerState.Frozen)
        {
            _playerScript.currentState = PlayerScript.playerState.Idling;
        }

    }
    public void SetPlayerStateStagger()
    {
        if (_playerScript.currentState != PlayerScript.playerState.Frozen)
        {
            _playerScript.currentState = PlayerScript.playerState.Staggered;
        }
    }

}
