using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitCounter : MonoBehaviour
{
    float lastHitTime;
    public int comboCounter;
    const float comboThreshold = 0.3f; // will need to be changed
    float currentAnimEndTime;
    float nextAnimEndTime;

    // Start is called before the first frame update
    void Start()
    {
        lastHitTime = Time.time;
    }

    public int Hit()
    {
        float currentTime = Time.time;
        if(currentTime >= currentAnimEndTime)
        {
            currentAnimEndTime = nextAnimEndTime;
        }
        if(currentTime <= currentAnimEndTime)
        {
            if(comboCounter == 1)
            {
                comboCounter++;
                nextAnimEndTime = currentAnimEndTime + comboThreshold;
            }

        }
        else if (currentAnimEndTime <= currentTime && currentTime <= nextAnimEndTime)
        {
            comboCounter++;
            currentAnimEndTime = nextAnimEndTime;
            nextAnimEndTime += comboThreshold;

        }
        else //is the first hit
        {
            comboCounter = 1;
            currentAnimEndTime = currentTime + comboThreshold;
        }
        //lastHitTime = currentTime;
        return comboCounter;
    }


}
