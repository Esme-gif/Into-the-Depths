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
        _playerScript.currentState = PlayerScript.playerState.Attacking;
    }
    public void SetPlayerStateIdling()
    {
        _playerScript.currentState = PlayerScript.playerState.Idling;
    }
    public void SetPlayerStateStagger()
    {
        _playerScript.currentState = PlayerScript.playerState.Staggered;
    }


}
