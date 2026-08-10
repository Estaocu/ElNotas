using UnityEngine;

public class ObstacleFader : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] [Range(0f, 1f)] private float fadedAlpha = 0.2f;

    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_Base_Color");

    private float currentAlpha = 1f;
    private float targetAlpha = 1f;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (targetRenderer == null) return;

        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            ApplyAlpha(currentAlpha);
        }
    }

    // Call this to start fading out the obstacle
    public void FadeOut()
    {
        targetAlpha = fadedAlpha;
    }

    // Call this to restore opacity
    public void FadeIn()
    {
        targetAlpha = 1f;
    }

    // Call this to set the state directly based on a condition
    public void SetFaded(bool isFaded)
    {
        targetAlpha = isFaded ? fadedAlpha : 1f;
    }

    private void ApplyAlpha(float alpha)
    {
        targetRenderer.GetPropertyBlock(propertyBlock);
        Color currentColor = targetRenderer.sharedMaterial.GetColor(BaseColorId);
        currentColor.a = alpha;
        propertyBlock.SetColor(BaseColorId, currentColor);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}