using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayspaceUIManager : MonoBehaviour
{
   public  ReferenceManager _refMan;

    public Slider ashesSlider; //will need to be a prefab that spawns with enemy ashes somehow
    public Slider enemyHealthSlider;

    Vector3 enemyAshesDespawnTimerOffset;
    RectTransform sliderRT;

    [SerializeField] Vector3 sliderOffset = new Vector2(-393.1f, -248.6f);

    public Canvas myCanvas;

    public List<RectTransform> ashesRespawnSliderRt = new List<RectTransform>();
    public List<RectTransform> enemyHealthSliderRt = new List<RectTransform>();


    // Start is called before the first frame update
    void Awake()
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
        if (enemyHealthSliderRt.Count > 0)
        {
            for (int i = 0; i < enemyHealthSliderRt.Count; i++)
            {
                enemyHealthSliderRt[i].anchoredPosition = Camera.main.WorldToScreenPoint(_refMan.enemies[i].transform.position)
                    / myCanvas.scaleFactor + sliderOffset;
            }
        }
    }

    public Slider SpawnEnemyAshesSlider(int index)
    { 
        Slider newSlider = Instantiate(ashesSlider);
        newSlider.transform.SetParent(gameObject.transform, false);
        sliderRT = newSlider.GetComponent<RectTransform>();
        Vector3 enemyAshesPos = _refMan.enemyAshes[index].transform.position;
        sliderRT.anchoredPosition = Camera.main.WorldToScreenPoint(enemyAshesPos) / myCanvas.scaleFactor + sliderOffset;
        ashesRespawnSliderRt.Add(sliderRT);
        return newSlider;
    }

    public Slider SpawnEnemyHealthSlider(Vector2 pos)
    {
        Slider newSlider = Instantiate(enemyHealthSlider);
        newSlider.transform.SetParent(gameObject.transform, false);
        sliderRT = newSlider.GetComponent<RectTransform>();
        //Vector3 enemyPos = _refMan.enemies[index].transform.position;
        sliderRT.anchoredPosition = Camera.main.WorldToScreenPoint(pos) / myCanvas.scaleFactor + sliderOffset;
        enemyHealthSliderRt.Add(sliderRT);
        return newSlider;
    }
}
