using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Misc;
using UnityEngine;

enum StateVariableFilterType
{
    LowpassFilter,
    BandpassFilter,
    HighpassFilter
}

public record SVFCoeffs(float F1, float Q1);

public class StateVariableFilter : MonoBehaviour, IAudioEffect
{
    [SerializeField]
    private StateVariableFilterType filterType;

    [SerializeField]
    [Range(20f, 20000f)]
    private float frequency = 200;

    [SerializeField]
    [Range(0.5f, 20f)]
    private float q = 1 / Mathf.Sqrt(2);

    private int _version = 0;
    public int Version => Volatile.Read(ref _version);

    private SVFCoeffs _coeffs = new SVFCoeffs(0.5f, 0.5f);

    public SVFCoeffs Coeffs => Volatile.Read(ref _coeffs);
    public void PublishCoeffs(SVFCoeffs coeffs)
    {
        Volatile.Write(ref _coeffs, coeffs);
        Interlocked.Increment(ref _version);
    }

    private float sampleRate;

    private float[] _historyLPF;
    private float[] _historyBPF;

    // Start is called before the first frame update
    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
    }

    public void Process(float[] data, int channels)
    {
        _historyLPF ??= new float[channels];
        _historyBPF ??= new float[channels];

        var localFreq = Volatile.Read(ref frequency);
        var localQ = Volatile.Read(ref q);
        var localType = filterType;

        float f1 = 2 * Mathf.Sin(Mathf.PI * localFreq / sampleRate);
        float q1 = 1 / localQ;

        if (!Misc.NearlyEqual(Coeffs.F1, f1) || !Misc.NearlyEqual(Coeffs.Q1, q1))
        {
            PublishCoeffs(new SVFCoeffs(f1, q1));
        }

        var c = Coeffs;

        for (var i = 0; i < data.Length; i = i + channels)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                float yHpf = data[i + ch] - _historyLPF[ch] - c.Q1 * _historyBPF[ch];
                float yBpf = yHpf * c.F1 + _historyBPF[ch];
                float yLpf = yBpf * c.F1 + _historyLPF[ch];

                data[i + ch] = localType switch
                {
                    StateVariableFilterType.LowpassFilter => yLpf,
                    StateVariableFilterType.BandpassFilter => yBpf,
                    StateVariableFilterType.HighpassFilter => yHpf,
                    _ => throw new ArgumentException()
                };

                _historyBPF[ch] = yBpf;
                _historyLPF[ch] = yLpf;
            }
        }
    }
}
