using System.Collections;
using UnityEngine;

public class Soundwave : MonoBehaviour
{
    public Melody myMelody;

    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float expandDuration = 2f;
    [SerializeField] private float maxScale = 10f;
    public AnimationCurve easeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    private void Start()
    {
        // Se desvincula del padre para que no se mueva con él.
        transform.SetParent(null);

        ApplyMelodyColor();

        StartCoroutine(Expand());
        Destroy(gameObject, lifetime);
    }

    private void ApplyMelodyColor()
    {
        if (myMelody == null) return;

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        // Instancia el material para no modificar el asset compartido.
        Material mat = renderer.material;
        Color baseColor = mat.color;
        Color melodyColor = myMelody.color;
        mat.color = new Color(melodyColor.r, melodyColor.g, melodyColor.b, baseColor.a);
    }

    private IEnumerator Expand()
    {
        float elapsed = 0f;
        float fixedY = transform.localScale.y;

        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / expandDuration;
            float tEaseOut = easeCurve.Evaluate(t);
            float scale = Mathf.Lerp(0f, maxScale, tEaseOut);
            transform.localScale = new Vector3(scale, fixedY, scale);
            yield return null;
        }

        transform.localScale = new Vector3(maxScale, fixedY, maxScale);
    }
}
