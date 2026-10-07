using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MetronomeBar : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 endPos;
    private Vector2 deployedStartPos;
    private Vector2 deployedEndPos;

    [SerializeField] private float upDur;
    [SerializeField] private float downDur;
    [SerializeField] private AnimationCurve upCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve downCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float bouncePixels = 70f;
    [SerializeField] private float upPixels;

    private RectTransform rt;
    private Image sprite;
    private RhythmClock clock;

    private Coroutine bounceCoroutine;
    private Coroutine returnCoroutine;

    private bool isDeployed;

    private void Awake()
    {
        sprite = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        clock = FindFirstObjectByType<RhythmClock>();

        // Store the original position before OnEnable can run.
        startPos = rt.anchoredPosition;
        endPos = new Vector2(startPos.x, startPos.y + bouncePixels);

        UpdateDeployedPositions();
    }

    private void OnEnable()
    {
        StopAnimations();
        UpdateDeployedPositions();

        rt.anchoredPosition = deployedStartPos;
    }

    private void OnDisable()
    {
        StopAnimations();
    }

    public void Bounce(int i)
    {
        if (!isActiveAndEnabled)
            return;

        StopAnimations();

        bounceCoroutine = StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < upDur)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / upDur);
            float curveValue = upCurve.Evaluate(normalizedTime);

            rt.anchoredPosition = Vector2.LerpUnclamped(
                deployedStartPos,
                deployedEndPos,
                curveValue
            );

            yield return null;
        }

        yield return StartCoroutine(WaitForReturn());

        bounceCoroutine = null;
    }

    private IEnumerator WaitForReturn()
    {
        if (clock == null)
            yield break;

        float time = (float)clock.SubBeatDuration;

        yield return new WaitForSeconds(time);

        returnCoroutine = StartCoroutine(ReturnRoutine());

        yield return returnCoroutine;

        returnCoroutine = null;
    }

    private IEnumerator ReturnRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < downDur)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / downDur);
            float curveValue = downCurve.Evaluate(normalizedTime);

            rt.anchoredPosition = Vector2.LerpUnclamped(
                deployedEndPos,
                deployedStartPos,
                curveValue
            );

            yield return null;
        }

        rt.anchoredPosition = deployedStartPos;
    }

    public void Deploy(bool desiredState)
    {
        isDeployed = desiredState;

        StopAnimations();
        UpdateDeployedPositions();

        rt.anchoredPosition = deployedStartPos;
    }

    private void UpdateDeployedPositions()
    {
        float offset = isDeployed ? upPixels : 0f;

        deployedStartPos = new Vector2(
            startPos.x,
            startPos.y + offset
        );

        deployedEndPos = new Vector2(
            endPos.x,
            endPos.y + offset
        );
    }

    private void StopAnimations()
    {
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
        }

        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    public void ToggleVisibility(bool desiredState)
    {
        sprite.enabled = desiredState;
    }
}