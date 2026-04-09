using CMF;
using UnityEngine;
using UnityEngine.InputSystem;

public class Instrument : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    private PlayerInputs input;
    public ActionMapsManager controlsManager;

    private void Awake()
    {
        // Inicializa la clase generada por el Input System
        input = new PlayerInputs();
    }

    private void OnEnable()
    {
        // Suscripción a los eventos de las notas en el mapa "Gameplay"
        input.Gameplay.Note1.performed += OnNote1Played;
        input.Gameplay.Note2.performed += OnNote2Played;
        input.Gameplay.Note3.performed += OnNote3Played;
        input.Gameplay.Note4.performed += OnNote4Played;

        // Habilita el mapa de acciones de Gameplay para que detecte entradas
        input.Gameplay.Enable();
    }

    private void OnDisable()
    {
        // Desuscripción de los eventos
        input.Gameplay.Note1.performed -= OnNote1Played;
        input.Gameplay.Note2.performed -= OnNote2Played;
        input.Gameplay.Note3.performed -= OnNote3Played;
        input.Gameplay.Note4.performed -= OnNote4Played;

        // Deshabilita el mapa de acciones
        input.Gameplay.Disable();
    }

    // Métodos que ejecutan el Debug.Log al tocar cada nota según los nombres del archivo
    private void OnNote1Played(InputAction.CallbackContext context)
    {
        Debug.Log("Nota 1 tocada"); // Vinculado a <Gamepad>/buttonSouth o <Keyboard>/e
    }

    private void OnNote2Played(InputAction.CallbackContext context)
    {
        Debug.Log("Nota 2 tocada"); // Vinculado a <Gamepad>/buttonEast o <Keyboard>/r
    }

    private void OnNote3Played(InputAction.CallbackContext context)
    {
        Debug.Log("Nota 3 tocada"); // Vinculado a <Gamepad>/buttonWest o <Keyboard>/f
    }

    private void OnNote4Played(InputAction.CallbackContext context)
    {
        Debug.Log("Nota 4 tocada"); // Vinculado a <Gamepad>/buttonNorth o <Keyboard>/c
    }
}