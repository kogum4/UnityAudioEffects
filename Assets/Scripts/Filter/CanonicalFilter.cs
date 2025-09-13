using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

enum FilterType
{
    FirstOrderLowpass,
    FirstOrderHighpass,
    FirstOrderAllpass,
    SecondOrderLowpass,
    SecondOrderHighpass,
    SecondOrderBandpass,
    SecondOrderBandreject,
    SecondOrderAllpass

}

public class CanonicalFilter : MonoBehaviour, IAudioEffect
{
    [SerializeField]
    private FilterType filterType;

    [SerializeField]
    [Range(20, 20000)]
    private float frequency = 200;

    [SerializeField]
    [Range(0.5f, 20.0f)]
    private float q = 1 / Mathf.Sqrt(2);

    private float samplingRate;
    private float[] history;
    private float a1;
    private float a2;
    private float b0;
    private float b1;
    private float b2;

    // Start is called before the first frame update
    void Start()
    {
        samplingRate = AudioSettings.outputSampleRate;
    }

    public void Process(float[] data, int channels)
    {
        history ??= new float[2 * channels];

        float local_freq = Volatile.Read(ref frequency);
        float local_q = Volatile.Read(ref q);
        float k = Mathf.Tan(Mathf.PI * local_freq / samplingRate);

        float coeff_2nd_denom = k * k * local_q + k + q;

        a1 = filterType switch
        {
            FilterType.FirstOrderLowpass => (k - 1) / (k + 1),
            FilterType.FirstOrderHighpass => (k - 1) / (k + 1),
            FilterType.FirstOrderAllpass => (k - 1) / (k + 1),
            _ => (2 * local_q * (k * k - 1)) / coeff_2nd_denom
        };

        a2 = filterType switch
        {
            FilterType.FirstOrderLowpass => 0,
            FilterType.FirstOrderHighpass => 0,
            FilterType.FirstOrderAllpass => 0,
            _ => (k * k * local_q - k + q) / coeff_2nd_denom
        };

        b0 = filterType switch
        {
            FilterType.FirstOrderLowpass => k / (k + 1),
            FilterType.FirstOrderHighpass => 1 / (k + 1),
            FilterType.FirstOrderAllpass => (k - 1) / (k + 1),
            FilterType.SecondOrderLowpass => (k * k * local_q) / coeff_2nd_denom,
            FilterType.SecondOrderHighpass => (local_q) / coeff_2nd_denom,
            FilterType.SecondOrderBandpass => (k) / coeff_2nd_denom,
            FilterType.SecondOrderBandreject => (local_q * (1 + k * k)) / coeff_2nd_denom,
            FilterType.SecondOrderAllpass => (k * k * local_q - k + q) / coeff_2nd_denom,
            _ => throw new ArgumentException()
        };

        b1 = filterType switch
        {
            FilterType.FirstOrderLowpass => k / (k + 1),
            FilterType.FirstOrderHighpass => -1 / (k + 1),
            FilterType.FirstOrderAllpass => 1,
            FilterType.SecondOrderLowpass => (2 * k * k * local_q) / coeff_2nd_denom,
            FilterType.SecondOrderHighpass => -(2 * local_q) / coeff_2nd_denom,
            FilterType.SecondOrderBandpass => 0,
            FilterType.SecondOrderBandreject => (2 * local_q * (k * k - 1)) / coeff_2nd_denom,
            FilterType.SecondOrderAllpass => (2 * local_q * (k * k - 1)) / coeff_2nd_denom,
            _ => throw new ArgumentException()
        };

        b2 = filterType switch
        {
            FilterType.FirstOrderLowpass => 0,
            FilterType.FirstOrderHighpass => 0,
            FilterType.FirstOrderAllpass => 0,
            FilterType.SecondOrderLowpass => (k * k * local_q) / coeff_2nd_denom,
            FilterType.SecondOrderHighpass => (local_q) / coeff_2nd_denom,
            FilterType.SecondOrderBandpass => -(k) / coeff_2nd_denom,
            FilterType.SecondOrderBandreject => (local_q * (1 + k * k)) / coeff_2nd_denom,
            FilterType.SecondOrderAllpass => 1,
            _ => throw new ArgumentException()
        };

        for (var i = 0; i < data.Length; i = i + channels)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                float temp = data[i + ch] - a1 * history[ch] - a2 * history[2 + ch];
                data[i + ch] = b0 * temp + b1 * history[ch] + b2 * history[2 + ch];
                history[2 + ch] = history[ch];
                history[ch] = temp;
            }
        }

    }
}
