using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayspaceUIManager : MonoBehaviour
{
    ReferenceManager _refMan;

    public Slider sampleSlider; //will need to be a prefab that spawns with enemy ashes somehow

    Vector3 enemyAshesDespawnTimerOffset;
    RectTransform sliderRT;

    [SerializeField] Vector3 sliderOffset = new Vector2(-393.1f, -248.6f);

    Canvas myCanvas;

    public List<RectTransform> ashesRespawnSliderRt = new List<RectTransform>();

    // Start is called before the first frame update
    void Start()
    {
        _refMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<ReferenceManager>();
        myCanvas = GetComponent<Canvas>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(ashesRespawnSliderRt.Count > 0)
        {
            for (int i = 0; i<ashesRespawnSliderRt.Count; i++)
            {
                ashesRespawnSliderRt[i].anchoredPosition = Camera.main.WorldToScreenPoint(_refMan.enemyAshes[i].transform.position) 
                    / myCanvas.scaleFactor + sliderOffset;
            }

        }
        
    }

    public Slider SpawnEnemyAshesSlider(int index)
    { 
        Slider newSlider = Instantiate(sampleSlider);
        newSlider.transform.SetParent(_refMan.playspaceUIManager.gameObject.transform, false);
        sliderRT = newSlider.GetComponent<RectTransform>();
        sliderRT.anchoredPosition = Camera.main.WorldToScreenPoint(_refMan.enemyAshes[index].transform.position) / myCanvas.scaleFactor + sliderOffset;
        ashesRespawnSliderRt.Add(sliderRT);
        return newSlider;
    }
}
