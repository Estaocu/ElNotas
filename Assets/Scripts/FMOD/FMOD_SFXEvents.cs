using UnityEngine;
using FMODUnity;
using System.Reflection;

public class FMOD_SFXEvents : MonoBehaviour
{
    public static FMOD_SFXEvents Instance { get; private set; }

    [field: Header("Movimiento")]
    [field: SerializeField]
    public EventReference pisada { get; private set; }

    [field: SerializeField]
    public EventReference salto { get; private set; }

    [field: Header("Combate")]
    [field: SerializeField]
    public EventReference ataque { get; private set; }

    [field: SerializeField]
    public EventReference impacto { get; private set; }

    [field: SerializeField]
    public EventReference explosion { get; private set; }

    [field: Header("Interfaz")]
    [field: SerializeField]
    public EventReference uiButtonSubmit { get; private set; }

    [field: SerializeField]
    public EventReference uiButtonSelect { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public EventReference GetReference(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName))
            return default;

        PropertyInfo property = GetType().GetProperty(
            soundName,
            BindingFlags.Public | BindingFlags.Instance
        );

        if (property != null &&
            property.PropertyType == typeof(EventReference))
        {
            return (EventReference)property.GetValue(this);
        }

        Debug.LogWarning(
            $"[FMOD_SFXEvents] No existe el sonido '{soundName}'."
        );

        return default;
    }
}