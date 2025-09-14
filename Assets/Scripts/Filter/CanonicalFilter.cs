using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

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

public record BiquadCoeffs(float B0, float B1, float B2, float A1, float A2);

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
    
    private BiquadCoeffs _coeffs = new BiquadCoeffs(1, 0, 0, 0, 0);
    public BiquadCoeffs Coeffs => Volatile.Read(ref _coeffs);
    public void publishCoeffs(float b0, float b1, float b2, float a1, float a2)
    {
        Volatile.Write(ref _coeffs, new BiquadCoeffs(b0, b1, b2, a1, a2));
    }

    // Start is called before the first frame update
    void Start()
    {
        samplingRate = AudioSettings.outputSampleRate;
    }

    public void Process(float[] data, int channels)
    {
        history ??= new float[2 * channels];

        var localType = filterType;
        float localFreq = Mathf.Min(Volatile.Read(ref frequency), 0.499f * samplingRate);
        float localQ = Volatile.Read(ref q);
        float k = Mathf.Tan(Mathf.PI * localFreq / samplingRate);

        float coeff_2nd_denom = k * k * localQ + k + q;

        float a1 = localType switch
        {
            FilterType.FirstOrderLowpass => (k - 1) / (k + 1),
            FilterType.FirstOrderHighpass => (k - 1) / (k + 1),
            FilterType.FirstOrderAllpass => (k - 1) / (k + 1),
            _ => (2 * localQ * (k * k - 1)) / coeff_2nd_denom
        };

        float a2 = localType switch
        {
            FilterType.FirstOrderLowpass => 0,
            FilterType.FirstOrderHighpass => 0,
            FilterType.FirstOrderAllpass => 0,
            _ => (k * k * localQ - k + localQ) / coeff_2nd_denom
        };

        float b0 = localType switch
        {
            FilterType.FirstOrderLowpass => k / (k + 1),
            FilterType.FirstOrderHighpass => 1 / (k + 1),
            FilterType.FirstOrderAllpass => (k - 1) / (k + 1),
            FilterType.SecondOrderLowpass => (k * k * localQ) / coeff_2nd_denom,
            FilterType.SecondOrderHighpass => (localQ) / coeff_2nd_denom,
            FilterType.SecondOrderBandpass => (k) / coeff_2nd_denom,
            FilterType.SecondOrderBandreject => (localQ * (1 + k * k)) / coeff_2nd_denom,
            FilterType.SecondOrderAllpass => (k * k * localQ - k + localQ) / coeff_2nd_denom,
            _ => throw new ArgumentException()
        };

        float b1 = localType switch
        {
            FilterType.FirstOrderLowpass => k / (k + 1),
            FilterType.FirstOrderHighpass => -1 / (k + 1),
            FilterType.FirstOrderAllpass => 1,
            FilterType.SecondOrderLowpass => (2 * k * k * localQ) / coeff_2nd_denom,
            FilterType.SecondOrderHighpass => -(2 * localQ) / coeff_2nd_denom,
            FilterType.SecondOrderBandpass => 0,
            FilterType.SecondOrderBandreject => (2 * localQ * (k * k - 1)) / coeff_2nd_denom,
            FilterType.SecondOrderAllpass => (2 * localQ * (k * k - 1)) / coeff_2nd_denom,
            _ => throw new ArgumentException()
        };

        float b2 = localType switch
        {
            FilterType.FirstOrderLowpass => 0,
            FilterType.FirstOrderHighpass => 0,
            FilterType.FirstOrderAllpass => 0,
            FilterType.SecondOrderLowpass => (k * k * localQ) / coeff_2nd_denom,
            FilterType.SecondOrderHighpass => (localQ) / coeff_2nd_denom,
            FilterType.SecondOrderBandpass => -(k) / coeff_2nd_denom,
            FilterType.SecondOrderBandreject => (localQ * (1 + k * k)) / coeff_2nd_denom,
            FilterType.SecondOrderAllpass => 1,
            _ => throw new ArgumentException()
        };

        publishCoeffs(b0, b1, b2, a1, a2);

        for (var i = 0; i < data.Length; i = i + channels)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                float temp = data[i + ch] - a1 * history[ch] - a2 * history[channels + ch];
                data[i + ch] = b0 * temp + b1 * history[ch] + b2 * history[channels + ch];
                history[channels + ch] = history[ch];
                history[ch] = temp;
            }
        }

    }
}
