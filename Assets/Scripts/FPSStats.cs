using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSStats : MonoBehaviour
{
    [SerializeField]
    private Text m_fpsDisplay;
    
    // Update is called once per frame
    void Update()
    {
        float fps = 1 / Time.unscaledDeltaTime;
        m_fpsDisplay.text = $"{fps} FPS";
    }
}
