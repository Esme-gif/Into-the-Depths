using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionButton : MonoBehaviour
{
    public int value;
    public ReferenceManager refMan;

    private void Start()
    {
        refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();

    }

    public void OnClickOptionButton()
    {
        //apply value to total charges
        if (GameManager.canSpecial && refMan.player.testingTriggerSpecial)
        {
            ScenePersistence._scenePersist.specialCharges += value;
            refMan.gameManager.IncreaseSpecialBarSize(value);
            //will also need UI animation triggers here.
        }
    }

}
