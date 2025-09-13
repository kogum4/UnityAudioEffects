using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SimplePlayer : MonoBehaviour, IAudioEffect
{
    [SerializeField]
    private AudioClip audioClip;

    private float[] buffer;
    private float samplingRate;
    private int position;

    // Start is called before the first frame update
    void Start()
    {
        samplingRate = AudioSettings.outputSampleRate;

        buffer = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(buffer, 0);

        position = 0;
    }

    public void Process(float[] data, int channels) {
        for (var i = 0; i < data.Length; i = i+channels)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                data[i + ch] = buffer[position];
                position++;

                if (position >= buffer.Length)
                {
                    position = 0;
                }
            }
        }
    }
}
