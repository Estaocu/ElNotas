using UnityEngine;
using UnityEngine.InputSystem;
using CMF; // Asegúrate de incluir el namespace donde está tu ActionMapsManager

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuUI;
    public AudioSource musicSource;

    public void TogglePause(InputAction.CallbackContext context)
{
    if (!context.performed) return;

    if (Time.timeScale == 1f)
    {
        // ANTES de pausar, guardamos qué mapa había (Player o Restricted)
        ActionMapsManager.Instance.StoreCurrentMap();
        
        Time.timeScale = 0f;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        RhythmManager.Instance?.Pause();

        // Forzamos el mapa de pausa/restricción
        ActionMapsManager.Instance.SetRestrictedInput();

        if (musicSource.isPlaying) musicSource.Pause(); 

    }
    else
    {
        Time.timeScale = 1f;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        RhythmManager.Instance?.Resume();

        // En lugar de SetPlayerInput, usamos RESTAURAR
        ActionMapsManager.Instance.RestoreLastMap();

        musicSource.UnPause();
    }
}

    private void DoPause()
    {
        // 1. Detener el tiempo del juego
        Time.timeScale = 0f;

        // 2. Mostrar el menú visual (si tienes uno asignado)
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);

        // 3. Cambiar controles usando el Singleton
        ActionMapsManager.Instance.SetRestrictedInput();

        Debug.Log("Juego Pausado");
    }

    private void DoUnpause()
    {
        // 1. Reanudar el tiempo del juego
        Time.timeScale = 1f;

        // 2. Ocultar el menú visual
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        // 3. Devolver controles al jugador usando el Singleton
        ActionMapsManager.Instance.SetPlayerInput();

        Debug.Log("Juego Reanudado");
    }
}