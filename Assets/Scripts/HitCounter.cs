using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitCounter : MonoBehaviour
{
    public int comboCounter;
    int comboLength;
    float comboThreshold; // will need to be changed
    float slowThreshold;
    float currentAnimEndTime;
    float nextAnimEndTime;
    float slowEndTime;


    public void Initialize(float threshhold, float missComboTresh, int length)
    {
        comboThreshold = threshhold;
        slowThreshold = missComboTresh;
        comboLength = length;
    }

    public (int,bool)  Hit()
    {
        float currentTime = Time.time;
        bool incrementedCC = false;
        if(currentTime <= currentAnimEndTime)
        {
            if(comboCounter == 1) // first anim is currently playing
            {
                comboCounter++;
                nextAnimEndTime = currentAnimEndTime + comboThreshold;
                slowEndTime = nextAnimEndTime + slowThreshold;
                incrementedCC = true;
            }
            
        }
        else if (currentAnimEndTime <= currentTime && currentTime <= nextAnimEndTime)
        {
            if(comboCounter >= comboLength) //if this is one click past the length of the combo
            {
                comboCounter = 1;
                currentAnimEndTime = currentTime + comboThreshold;
            }
            else
            {
                comboCounter++;
                currentAnimEndTime = nextAnimEndTime;
                nextAnimEndTime += comboThreshold;
                slowEndTime = nextAnimEndTime + slowThreshold;
                incrementedCC = true;
            }

        }
        /*else if(nextAnimEndTime <= currentTime && currentTime <= slowEndTime)
        {
            comboCounter = 0;
        }*/
        else //is the first hit
        {
            comboCounter = 1;
            currentAnimEndTime = currentTime + comboThreshold;
            slowEndTime = currentAnimEndTime + slowThreshold;

        }
        //lastHitTime = currentTime;
        Debug.Log("currentTime is: " + Time.time + " comboCounter is " + comboCounter + ", incremented is " + incrementedCC
            + " currentAnimEndTime is " + currentAnimEndTime + ", nextAnimEndTime is " + nextAnimEndTime + ", slowEndTime is "
            + slowEndTime);
        return (comboCounter, incrementedCC);
    }


}
