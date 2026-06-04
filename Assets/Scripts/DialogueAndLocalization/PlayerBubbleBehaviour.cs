using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;

public class PlayerBubbleBehaviour : MonoBehaviour
{

    [SerializeField] private Image wordContainer;
    [SerializeField] private TextMeshProUGUI wordText;
    [SerializeField] private float xOffset;
    [SerializeField] private float yOffset;
    public Image[] noteImages = new Image[4];



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
