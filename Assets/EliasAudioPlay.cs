using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliasAudioPlay : MonoBehaviour
{
    public AudioSource src;
    public AudioClip [] steps;
    
    // Start is called before the first frame update
    void Start()
    {
        src = GetComponent<AudioSource>();   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PlayStep() {
        src.clip = steps[Random.Range(0,0)];
        src.PlayOneShot(steps[0]);
    }
}
