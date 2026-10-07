using System;
using UnityEngine;

public class BubblePosition : MonoBehaviour
{
    [Serializable]
    private struct RectTransformState
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;

        public RectTransformState(RectTransform rectTransform)
        {
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
            pivot = rectTransform.pivot;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
        }

        public void ApplyTo(RectTransform rectTransform)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }
    }

    private RectTransform rectTransform;

    [SerializeField] private RectTransformState positionA;
    [SerializeField] private RectTransformState positionB;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void ApplyPositionA()
    {
        ApplyState(positionA);
    }

    public void ApplyPositionB()
    {
        ApplyState(positionB);
    }

    private void ApplyState(RectTransformState state)
    {
        state.ApplyTo(rectTransform);
    }
}
