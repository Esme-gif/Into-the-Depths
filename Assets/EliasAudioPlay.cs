using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliasAudioPlay : MonoBehaviour
{
    public AudioSource src;
    public AudioClip [] leftSteps;
    public AudioClip [] rightSteps;
    public float vol;
    public bool left;
    private int stepCount;
    
    // Start is called before the first frame update
    void Start()
    {
        stepCount = 0;
        left = true;
        src.volume = vol;  
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PlayStep() {
        if (left) {
            src.clip = leftSteps[Random.Range(0,leftSteps.Length)];
            src.pitch = Random.Range(0.9f,1.1f);
            left = false;
        }
        else {
            src.clip = rightSteps[Random.Range(0,rightSteps.Length)];
            src.pitch = Random.Range(1.0f,1.2f);
            left = true;
        }
        src.PlayOneShot(src.clip);
    }
}
