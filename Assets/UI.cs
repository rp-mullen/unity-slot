using UnityEngine;
using TMPro;


public class UI : MonoBehaviour
{

    int credits = 0;

    public Reelset reelSet;
    public TMP_Text canvasText;
    void Start()
    {
        canvasText.text = credits.ToString();
        
    }


    void Update()
    {
        
    }

    public void updateMeter() {
        canvasText.text = (reelSet.credits).ToString() + " Credits";
    }
    
}
