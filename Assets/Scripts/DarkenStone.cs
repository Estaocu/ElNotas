using System.Collections;
using UnityEngine;

public class DarkenStone : MonoBehaviour
{
    [SerializeField] private Material stoneMat;
    [SerializeField] private float wantedDarkness;
    [SerializeField, Min(0f)] private float transitionTime = 1f;

    private static readonly int darknessId = Shader.PropertyToID("_Darkness");
    private Coroutine transitionCoroutine;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Already at the wanted value: nothing to do
        // (Approximately instead of == because floats rarely match exactly after a Lerp)
        if (Mathf.Approximately(stoneMat.GetFloat(darknessId), wantedDarkness)) return;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(transitionDarkness());
    }

    private IEnumerator transitionDarkness()
    {
        float startDarkness = stoneMat.GetFloat(darknessId);
        float elapsedTime = 0f;

        if (transitionTime <= 0f)
        {
            stoneMat.SetFloat(darknessId, wantedDarkness);
            transitionCoroutine = null;
            yield break;
        }

        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / transitionTime);
            stoneMat.SetFloat(darknessId, Mathf.Lerp(startDarkness, wantedDarkness, t));
            yield return null;
        }

        stoneMat.SetFloat(darknessId, wantedDarkness);
        transitionCoroutine = null;
    }
}