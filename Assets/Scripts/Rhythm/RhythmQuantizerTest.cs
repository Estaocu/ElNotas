using UnityEngine;
using UnityEngine.InputSystem;


public class RhythmQuantizerTest : MonoBehaviour
{
    [SerializeField] private RhythmQuantizer rhythmQuantizer;

    public void PlayNote(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        RhythmQuantizationResult result = rhythmQuantizer.QuantizeCurrentTime();

        Debug.Log(
            $"Input: {result.inputDspTime:F4} | " +
            $"Target: {result.targetDspTime:F4} | " +
            $"Offset: {result.offset * 1000.0:F1} ms | " +
            $"Bar: {result.position.bar} | " +
            $"Beat: {result.position.beat} | " +
            $"SubBeat: {result.position.subBeat} | " +
            $"Timing: {result.timing} | " +
            $"Valid: {result.isValid}"
        );
    }
}