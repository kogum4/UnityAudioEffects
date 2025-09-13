using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAudioEffect
{
    void Process(float[] data, int channels);
}

[RequireComponent(typeof(AudioSource))]
public class AudioEffectInterface : MonoBehaviour
{
    [SerializeField]
    private MonoBehaviour[] audioEffects;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        foreach (IAudioEffect effect in audioEffects)
        {
            effect.Process(data, channels);
        }
    }
}
