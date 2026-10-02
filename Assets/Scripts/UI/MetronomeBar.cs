using System.Collections;
using System.Collections.Generic;
using Febucci.TextAnimatorCore.BuiltIn;
using UnityEngine;

public class MetronomeBar : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 endPos;
    [SerializeField] private float upDur;
    [SerializeField] private float downDur;
    [SerializeField] private AnimationCurve upCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve downCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private RectTransform rt;
    [SerializeField] float bouncePixels = 70f;
    [SerializeField] float upPixels;

    private RhythmClock clock;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
        endPos = new Vector2(startPos.x, startPos.y + bouncePixels);
        clock = FindFirstObjectByType<RhythmClock>();
    }
    public void Bounce(int i)
    {
        StartCoroutine(BounceRoutine());
        //Debug.Log($"Bounced {i}");

    }

    private IEnumerator BounceRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < upDur)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / upDur);
            float curveValue = upCurve.Evaluate(normalizedTime);

            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curveValue);
            yield return null;
            StartCoroutine(WaitForReturn());
            
        }
    }
    public static float ToSingle(double value)
    {
        return (float)value;
    }

    private IEnumerator WaitForReturn()
    {
        float time = ToSingle(clock.SubBeatDuration);
        yield return new WaitForSeconds(time);

        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < downDur)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / downDur);
            float curveValue = downCurve.Evaluate(normalizedTime);

            rt.anchoredPosition = Vector2.LerpUnclamped(endPos, startPos, curveValue);
            yield return null;
        }
    }

    public void Deploy(bool desiredState)
    {
        if (desiredState)
        {
        startPos.y += upPixels;
        endPos.y += upPixels;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startPos.y);
        return;
        }
        startPos.y -= upPixels;
        endPos.y -= upPixels;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startPos.y);
    }

}
