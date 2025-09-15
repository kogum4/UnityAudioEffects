using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CanonicalFilterGraph : MonoBehaviour
{
    [SerializeField]
    private CanonicalFilter canonicalFilter;
    [SerializeField]
    private int resolution = 100;

    private LineRenderer lineRenderer;
    private BiquadCoeffs biquadCoeffs;
    private int version;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        biquadCoeffs = canonicalFilter.Coeffs;
        if (version != canonicalFilter.Version)
        {
            DrawFunction();
        }
        version = canonicalFilter.Version;
    }

    void DrawFunction()
    {
        lineRenderer.positionCount = resolution;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        float step = Mathf.PI / (resolution - 1);

        for (int i = 0; i < resolution; i++)
        {
            float x = i * step;
            Complex e1 = Complex.Exp(-Complex.ImaginaryOne * x);
            Complex e2 = e1 * e1;

            Complex num = biquadCoeffs.B0 + biquadCoeffs.B1 * e1 + biquadCoeffs.B2 * e2;
            Complex den = 1 + biquadCoeffs.A1 * e1 + biquadCoeffs.A2 * e2;
            Complex h = num / den;

            float y = 20 * Mathf.Log10(Mathf.Max((float)h.Magnitude, 1e-20f));
            UnityEngine.Vector3 pos = new UnityEngine.Vector3(x, y, 0);
            lineRenderer.SetPosition(i, pos);
        }
    }
}
