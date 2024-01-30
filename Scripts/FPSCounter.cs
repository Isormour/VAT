using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class FPSCounter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    private const int FPS_SAMPLE_COUNT = 20;
    private readonly int[] _fpsSamples = new int[FPS_SAMPLE_COUNT];
    private int _sampleIndex;
    private void Update()
    {
        UpdateFPS();
    }
    void UpdateFPS()
    {
        _fpsSamples[_sampleIndex++] = (int)(1.0f / Time.deltaTime);
        if (_sampleIndex >= FPS_SAMPLE_COUNT) _sampleIndex = 0;

        var sum = 0;
        for (var i = 0; i < FPS_SAMPLE_COUNT; i++)
        {
            sum += _fpsSamples[i];
        }
        text.text = "FPS = " + sum / FPS_SAMPLE_COUNT;
    }
}
