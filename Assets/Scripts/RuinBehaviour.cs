using UnityEngine;

public class RuinBehaviour : MonoBehaviour, IReactToMelody
{
    public enum RuinState
    {
        Fallen,
        Risen
    }

    [Header("Visuals & Initial State")]
    [SerializeField] private GameObject risenRuin;
    [SerializeField] private GameObject fallenRuin;
    [SerializeField] private RuinState myRuinState;

    [Header("Rhythm Settings")]
    [SerializeField] private int resetAfterBeats = 16;

    private RuinState initialRuinState;
    private BeatWaitHandle currentWaitHandle;

    private void Awake()
    {
        // Guardamos el estado original configurado en el Editor al iniciar la escena
        initialRuinState = myRuinState;
    }

    private void OnValidate()
    {
        UpdateRuinVisuals();
    }

    public void React(Melody receivedMelody)
    {
        if (receivedMelody == null || string.IsNullOrEmpty(receivedMelody.melodyName)) return;

        switch (receivedMelody.melodyName)
        {
            case "Ruin":
                ApplyTemporaryState(RuinState.Risen);
                break;

            case "InverseRuin":
                ApplyTemporaryState(RuinState.Fallen);
                break;
        }
    }

    private void ApplyTemporaryState(RuinState targetState)
    {
        // 1. Cancelamos cualquier temporizador/beat pendiente activo
        CancelPendingReset();

        // 2. Si el nuevo estado es idéntico al actual, no hace falta cambiar nada más
        if (myRuinState == targetState) return;

        // 3. Cambiamos de estado visual y lógico
        ChangeRuinState(targetState);

        // 4. Si el estado actual es distinto al original del Editor, programamos la vuelta
        if (myRuinState != initialRuinState)
        {
            currentWaitHandle = RhythmBeatWaiter.WaitForSubBeats(
                resetAfterBeats, 
                BeatWaitMode.Immediate, 
                ResetToInitialState
            );
        }
    }

    private void ResetToInitialState()
    {
        ChangeRuinState(initialRuinState);
        currentWaitHandle = null;
    }

    private void CancelPendingReset()
    {
        if (currentWaitHandle != null)
        {
            currentWaitHandle.Cancel();
            currentWaitHandle = null;
        }
    }

    public void ChangeRuinState(RuinState newState)
    {
        myRuinState = newState;
        UpdateRuinVisuals();
        Debug.Log($"Ruin state changed to: {myRuinState}");
    }

    private void UpdateRuinVisuals()
    {
        bool isRisen = myRuinState == RuinState.Risen;

        if (risenRuin != null)
        {
            risenRuin.SetActive(isRisen);
        }

        if (fallenRuin != null)
        {
            fallenRuin.SetActive(!isRisen);
        }
    }

    private void OnDisable()
    {
        // Evita callbacks huérfanos o errores si el objeto se desactiva/destruye durante la cuenta atrás
        CancelPendingReset();
    }

    public void OnProjectileHit()
    {
        if(initialRuinState == RuinState.Risen) ApplyTemporaryState(RuinState.Fallen);
        if(initialRuinState == RuinState.Fallen) ChangeRuinState(RuinState.Fallen);
    }
}