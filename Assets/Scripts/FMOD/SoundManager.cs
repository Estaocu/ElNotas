using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Snapshot
{
    allMusic,
    combatMusic,
    bossMusic,
    normalMusic,
    ambienceMusic
}

public enum MusicTrack
{
    Music1,
    Music2,
    CombatMusic,
    BossMusic,
    Ambience
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Volumen")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float textSoundVolume = 0.5f;
    [Range(0f, 1f)] public float sFXVolume = 0.5f;

    [Header("Rutas de los buses de FMOD")]
    [SerializeField] private string musicBusPath = "bus:/Music";
    [SerializeField] private string sfxBusPath = "bus:/SFX";
    [SerializeField] private string textBusPath = "bus:/TextSound";

    [Header("Cambios de escena")]
    [SerializeField] private bool stopAudioOnSceneChange = true;

    [Header("Sliders opcionales")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider textSlider;

    [Header("Debug")]
    public bool debugMode;

    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus textSoundBus;

    public EventInstance currentMusic;
    private EventInstance currentMusic2;
    private EventInstance currentCombatMusic;
    public EventInstance currentBossMusic;
    public EventInstance currentAmbience;

    public string currentMusicString;
    public string currentAmbienceString;

    public EventInstance combatSnapshot;
    public EventInstance ambienceSnapshot;
    public EventInstance bossSnapshot;
    public EventInstance pauseSnapshot;
    public EventInstance fadeNormalSnapshot;
    public EventInstance fadeMusicSnapshot;
    public EventInstance fadeMusic5Snapshot;

    private readonly List<EventInstance> eventInstances =
        new List<EventInstance>();

    private readonly List<EventInstance> activeLoopInstances =
        new List<EventInstance>();

    private readonly List<StudioEventEmitter> studioEventEmitters =
        new List<StudioEventEmitter>();

    private bool initialized;

    // ---------------------------------------------------------
    // INICIALIZACIÓN
    // ---------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        masterVolume = PlayerPrefs.GetFloat("MasterVolume", masterVolume);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
        sFXVolume = PlayerPrefs.GetFloat("SFXVolume", sFXVolume);
        textSoundVolume = PlayerPrefs.GetFloat("TextVolume", textSoundVolume);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        EnsureInitialized();
        FindSliders();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        masterBus = FindBus("bus:/");
        musicBus = FindBus(musicBusPath);
        sfxBus = FindBus(sfxBusPath);
        textSoundBus = FindBus(textBusPath);

        combatSnapshot = CreateOptionalSnapshot(
            "snapshot:/CombatSnapshot");

        ambienceSnapshot = CreateOptionalSnapshot(
            "snapshot:/AmbienceSnapshot");

        bossSnapshot = CreateOptionalSnapshot(
            "snapshot:/BossSnapshot");

        pauseSnapshot = CreateOptionalSnapshot(
            "snapshot:/PauseSnapshot");

        fadeNormalSnapshot = CreateOptionalSnapshot(
            "snapshot:/FadeNormalMusicSnapshot");

        fadeMusicSnapshot = CreateOptionalSnapshot(
            "snapshot:/FadeAllMusic1Snapshot");

        fadeMusic5Snapshot = CreateOptionalSnapshot(
            "snapshot:/FadeAllMusic5Snapshot");

        initialized = true;
        ApplyVolumes();
    }

    private Bus FindBus(string path)
    {
        FMOD.RESULT result =
            RuntimeManager.StudioSystem.getBus(path, out Bus bus);

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogWarning(
                $"[SoundManager] No se encuentra el bus '{path}'. " +
                "Comprueba su ruta y que los bancos estén cargados.");

            return default;
        }

        return bus;
    }

    private EventInstance CreateOptionalSnapshot(string path)
    {
        FMOD.RESULT result =
            RuntimeManager.StudioSystem.getEvent(
                path, out EventDescription description);

        if (result != FMOD.RESULT.OK)
        {
            if (debugMode)
                Debug.LogWarning(
                    $"[SoundManager] Snapshot opcional no disponible: {path}");

            return default;
        }

        result = description.createInstance(out EventInstance instance);

        return result == FMOD.RESULT.OK ? instance : default;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (initialized &&
            stopAudioOnSceneChange &&
            mode == LoadSceneMode.Single)
        {
            CleanUpAllInstances();
        }

        FindSliders();
    }

    private void OnDestroy()
    {
        // Un duplicado no debe limpiar el audio del manager principal.
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        DisconnectSliders();

        CleanUpAllInstances();

        ReleaseSnapshot(ref combatSnapshot);
        ReleaseSnapshot(ref ambienceSnapshot);
        ReleaseSnapshot(ref bossSnapshot);
        ReleaseSnapshot(ref pauseSnapshot);
        ReleaseSnapshot(ref fadeNormalSnapshot);
        ReleaseSnapshot(ref fadeMusicSnapshot);
        ReleaseSnapshot(ref fadeMusic5Snapshot);

        Instance = null;
    }

    // ---------------------------------------------------------
    // SONIDOS
    // ---------------------------------------------------------

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        EnsureInitialized();

        if (sound.IsNull)
            return;

        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public void PlayOneShotWithVolume(
        EventReference sound,
        Vector3 worldPos,
        float volume)
    {
        EnsureInitialized();

        if (sound.IsNull)
            return;

        EventInstance instance = RuntimeManager.CreateInstance(sound);

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPos));
        instance.setVolume(Mathf.Clamp01(volume));
        instance.start();

        // FMOD la libera cuando termina de sonar.
        instance.release();
    }

    public EventInstance CreateEventInstance(EventReference reference)
    {
        EnsureInitialized();

        if (reference.IsNull)
            return default;

        EventInstance instance = RuntimeManager.CreateInstance(reference);

        if (instance.isValid())
            eventInstances.Add(instance);

        return instance;
    }

    // Utiliza este método para detener y liberar las instancias
    // creadas mediante CreateEventInstance.
    public void StopAndReleaseInstance(
        EventInstance instance,
        bool allowFadeOut = true)
    {
        eventInstances.Remove(instance);
        activeLoopInstances.Remove(instance);

        if (!instance.isValid())
            return;

        instance.stop(
            allowFadeOut
                ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
                : FMOD.Studio.STOP_MODE.IMMEDIATE);

        instance.release();
    }

    public IEnumerator PlayTimedLoop(
        EventReference sound,
        Vector3 worldPos,
        float time)
    {
        EventInstance instance = CreateEventInstance(sound);

        if (!instance.isValid())
            yield break;

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPos));

        activeLoopInstances.Add(instance);
        instance.start();

        // La duración no se congela si Time.timeScale pasa a cero.
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, time));

        // Puede haberse limpiado antes por muerte o cambio de escena.
        if (activeLoopInstances.Contains(instance))
            StopAndReleaseInstance(instance);
    }

    // Inicia la rutina en el propio manager.
    public void StartTimedLoop(
        EventReference sound,
        Vector3 worldPos,
        float time)
    {
        StartCoroutine(PlayTimedLoop(sound, worldPos, time));
    }

    public StudioEventEmitter InitializeEventEmitter(
        EventReference reference,
        GameObject emitterGameObject)
    {
        EnsureInitialized();

        if (emitterGameObject == null || reference.IsNull)
            return null;

        StudioEventEmitter emitter =
            emitterGameObject.GetComponent<StudioEventEmitter>();

        if (emitter == null)
            emitter = emitterGameObject.AddComponent<StudioEventEmitter>();

        emitter.EventReference = reference;

        if (!studioEventEmitters.Contains(emitter))
            studioEventEmitters.Add(emitter);

        return emitter;
    }

    // ---------------------------------------------------------
    // MÚSICA
    // ---------------------------------------------------------

    public EventInstance InitializeMusicByType(
        MusicTrack track,
        EventReference musicReference,
        bool allowFadeOut = true,
        bool pauseOtherMusicIfMusic2 = true,
        bool forceRestart = false)
    {
        EnsureInitialized();

        if (musicReference.IsNull)
            return GetTrackInstance(track);

        ConfigureMusicMix(track, pauseOtherMusicIfMusic2);

        EventInstance existing = GetTrackInstance(track);

        if (!forceRestart && IsSameEventPlaying(existing, musicReference))
        {
            existing.setPaused(false);
            return existing;
        }

        // Sustituye la pista sin ejecutar acciones sobre otras pistas.
        StopAndReleaseInstance(existing, allowFadeOut);
        SetTrackInstance(track, default);

        EventInstance instance = CreateEventInstance(musicReference);

        if (!instance.isValid())
            return default;

        SetTrackInstance(track, instance);
        instance.start();

        string path = GetEventPath(instance);

        if (track == MusicTrack.Music1)
            currentMusicString = path;

        if (track == MusicTrack.Ambience)
            currentAmbienceString = path;

        return instance;
    }

    public EventInstance InitializeMusicByTypeWithString(
        MusicTrack track,
        string musicName,
        bool allowFadeOut = true,
        bool pauseOtherMusicIfMusic2 = true,
        bool forceRestart = false)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(musicName))
            return GetTrackInstance(track);

        // Aquí se espera una ruta completa: event:/Music/MiMusica
        FMOD.RESULT result = RuntimeManager.StudioSystem.getEvent(
            musicName, out EventDescription description);

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogWarning(
                $"[SoundManager] No se encuentra el evento '{musicName}'.");

            return GetTrackInstance(track);
        }

        result = description.getID(out FMOD.GUID guid);

        if (result != FMOD.RESULT.OK)
            return GetTrackInstance(track);

        EventReference reference = new EventReference { Guid = guid };

        return InitializeMusicByType(
            track,
            reference,
            allowFadeOut,
            pauseOtherMusicIfMusic2,
            forceRestart);
    }

    private void ConfigureMusicMix(
        MusicTrack track,
        bool pauseOtherMusicIfMusic2)
    {
        switch (track)
        {
            case MusicTrack.Music1:
                StopSnapshot(Snapshot.combatMusic);
                StopSnapshot(Snapshot.bossMusic);
                StopSnapshot(Snapshot.allMusic);
                PauseResumeTrack(MusicTrack.Music2, true);
                break;

            case MusicTrack.Music2:
                if (pauseOtherMusicIfMusic2)
                    PauseResumeTrack(MusicTrack.Music1, true);
                break;

            case MusicTrack.CombatMusic:
                StartSnapshot(Snapshot.combatMusic);
                break;

            case MusicTrack.BossMusic:
                StopSnapshot(Snapshot.allMusic);
                StartSnapshot(Snapshot.bossMusic);
                break;

            case MusicTrack.Ambience:
                // Conserva el comportamiento de tu manager original.
                StopSnapshot(Snapshot.combatMusic);
                StopSnapshot(Snapshot.bossMusic);
                StopSnapshot(Snapshot.allMusic);
                break;
        }
    }

    public EventInstance GetTrackInstance(MusicTrack track)
    {
        switch (track)
        {
            case MusicTrack.Music1: return currentMusic;
            case MusicTrack.Music2: return currentMusic2;
            case MusicTrack.CombatMusic: return currentCombatMusic;
            case MusicTrack.BossMusic: return currentBossMusic;
            case MusicTrack.Ambience: return currentAmbience;
            default: return default;
        }
    }

    private void SetTrackInstance(
        MusicTrack track,
        EventInstance instance)
    {
        switch (track)
        {
            case MusicTrack.Music1:
                currentMusic = instance;
                break;
            case MusicTrack.Music2:
                currentMusic2 = instance;
                break;
            case MusicTrack.CombatMusic:
                currentCombatMusic = instance;
                break;
            case MusicTrack.BossMusic:
                currentBossMusic = instance;
                break;
            case MusicTrack.Ambience:
                currentAmbience = instance;
                break;
        }
    }

    public void PauseResumeTrack(MusicTrack track, bool pause)
    {
        EventInstance instance = GetTrackInstance(track);

        if (instance.isValid())
            instance.setPaused(pause);
    }

    public void StopTrack(MusicTrack track, bool allowFadeOut = true)
    {
        EventInstance instance = GetTrackInstance(track);
        bool hadInstance = instance.isValid();

        StopAndReleaseInstance(instance, allowFadeOut);
        SetTrackInstance(track, default);

        if (track == MusicTrack.Music1)
            currentMusicString = string.Empty;

        if (track == MusicTrack.Ambience)
            currentAmbienceString = string.Empty;

        if (track == MusicTrack.CombatMusic)
            StopSnapshotInstance(combatSnapshot, !allowFadeOut);

        if (track == MusicTrack.BossMusic)
            StopSnapshotInstance(bossSnapshot, !allowFadeOut);

        if (track == MusicTrack.Music2 && hadInstance)
            PauseResumeTrack(MusicTrack.Music1, false);
    }

    public void StopAllMusic(bool instantly = true)
    {
        StopTrack(MusicTrack.Music1, !instantly);
        StopTrack(MusicTrack.Music2, !instantly);
        StopTrack(MusicTrack.CombatMusic, !instantly);
        StopTrack(MusicTrack.BossMusic, !instantly);
        StopTrack(MusicTrack.Ambience, !instantly);

        StopSnapshot(Snapshot.allMusic, instantly);
        StopSnapshot(Snapshot.normalMusic, instantly);
        StopSnapshot(Snapshot.ambienceMusic, instantly);
    }

    // Mantiene el nombre de tu método original.
    public bool CheckIfMusickTrackIsPlaying(MusicTrack track)
    {
        EventInstance instance = GetTrackInstance(track);

        if (!instance.isValid())
            return false;

        instance.getPlaybackState(out PLAYBACK_STATE state);
        instance.getPaused(out bool paused);

        return !paused &&
            (state == PLAYBACK_STATE.PLAYING ||
             state == PLAYBACK_STATE.STARTING ||
             state == PLAYBACK_STATE.SUSTAINING);
    }

    private bool IsSameEventPlaying(
        EventInstance instance,
        EventReference reference)
    {
        if (!instance.isValid())
            return false;

        instance.getPlaybackState(out PLAYBACK_STATE state);

        if (state != PLAYBACK_STATE.PLAYING &&
            state != PLAYBACK_STATE.STARTING &&
            state != PLAYBACK_STATE.SUSTAINING)
        {
            return false;
        }

        instance.getDescription(out EventDescription description);

        if (!description.isValid())
            return false;

        description.getID(out FMOD.GUID guid);
        return guid == reference.Guid;
    }

    private string GetEventPath(EventInstance instance)
    {
        if (!instance.isValid())
            return string.Empty;

        instance.getDescription(out EventDescription description);

        if (!description.isValid())
            return string.Empty;

        description.getPath(out string path);
        return path ?? string.Empty;
    }

    // ---------------------------------------------------------
    // SNAPSHOTS
    // ---------------------------------------------------------

    public void StartSnapshot(
        Snapshot snapshotToStart,
        bool use5Seconds = false)
    {
        EnsureInitialized();

        EventInstance instance = default;

        switch (snapshotToStart)
        {
            case Snapshot.allMusic:
                // Evita dejar activos los dos fades a la vez.
                StopSnapshotInstance(
                    use5Seconds ? fadeMusicSnapshot : fadeMusic5Snapshot,
                    true);

                instance = use5Seconds
                    ? fadeMusic5Snapshot
                    : fadeMusicSnapshot;
                break;

            case Snapshot.combatMusic:
                instance = combatSnapshot;
                break;

            case Snapshot.bossMusic:
                instance = bossSnapshot;
                break;

            case Snapshot.normalMusic:
                instance = fadeNormalSnapshot;
                break;

            case Snapshot.ambienceMusic:
                instance = ambienceSnapshot;
                break;
        }

        StartSnapshotInstance(instance);
    }

    public void StopSnapshot(
        Snapshot snapshotToStop,
        bool instantly = false)
    {
        switch (snapshotToStop)
        {
            case Snapshot.allMusic:
                StopSnapshotInstance(fadeMusicSnapshot, instantly);
                StopSnapshotInstance(fadeMusic5Snapshot, instantly);
                break;

            case Snapshot.combatMusic:
                StopTrack(MusicTrack.CombatMusic, !instantly);
                break;

            case Snapshot.bossMusic:
                StopTrack(MusicTrack.BossMusic, !instantly);
                break;

            case Snapshot.normalMusic:
                StopSnapshotInstance(fadeNormalSnapshot, instantly);
                break;

            case Snapshot.ambienceMusic:
                StopSnapshotInstance(ambienceSnapshot, instantly);
                break;
        }
    }

    public void ToggleSnapshot(
        Snapshot snapshot,
        bool enable,
        bool use5Seconds = false,
        bool instantlyWhenDisabling = false)
    {
        if (enable)
            StartSnapshot(snapshot, use5Seconds);
        else
            StopSnapshot(snapshot, instantlyWhenDisabling);
    }

    public void SetPauseSnapshot(bool paused)
    {
        EnsureInitialized();

        if (paused)
            StartSnapshotInstance(pauseSnapshot);
        else
            StopSnapshotInstance(pauseSnapshot, false);
    }

    private void StartSnapshotInstance(EventInstance instance)
    {
        if (!instance.isValid())
            return;

        instance.getPlaybackState(out PLAYBACK_STATE state);

        if (state == PLAYBACK_STATE.STOPPED ||
            state == PLAYBACK_STATE.STOPPING)
        {
            instance.start();
        }
    }

    private void StopSnapshotInstance(
        EventInstance instance,
        bool instantly)
    {
        if (!instance.isValid())
            return;

        instance.stop(
            instantly
                ? FMOD.Studio.STOP_MODE.IMMEDIATE
                : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    private void ReleaseSnapshot(ref EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
        }

        instance = default;
    }

    // ---------------------------------------------------------
    // LIMPIEZA
    // ---------------------------------------------------------

    // Puedes llamarlo desde el sistema de muerte del nuevo juego.
    public void CleanUpOnDeath()
    {
        for (int i = activeLoopInstances.Count - 1; i >= 0; i--)
        {
            StopAndReleaseInstance(activeLoopInstances[i], false);
        }
    }

    public void CleanUpAllInstances()
    {
        StopAllCoroutines();
        StopAllMusic(true);

        StopSnapshotInstance(pauseSnapshot, true);

        // Incluye bucles y otras instancias creadas por el manager.
        for (int i = eventInstances.Count - 1; i >= 0; i--)
        {
            EventInstance instance = eventInstances[i];

            if (instance.isValid())
            {
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
            }
        }

        eventInstances.Clear();
        activeLoopInstances.Clear();

        foreach (StudioEventEmitter emitter in studioEventEmitters)
        {
            if (emitter != null)
                emitter.Stop();
        }

        studioEventEmitters.Clear();

        currentMusicString = string.Empty;
        currentAmbienceString = string.Empty;
    }

    // ---------------------------------------------------------
    // VOLÚMENES
    // ---------------------------------------------------------

    private void ApplyVolumes()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sFXVolume = Mathf.Clamp01(sFXVolume);
        textSoundVolume = Mathf.Clamp01(textSoundVolume);

        if (masterBus.isValid())
            masterBus.setVolume(masterVolume);

        if (musicBus.isValid())
            musicBus.setVolume(musicVolume);

        if (sfxBus.isValid())
            sfxBus.setVolume(sFXVolume);

        if (textSoundBus.isValid())
            textSoundBus.setVolume(textSoundVolume);
    }

    public void SetSlidersVolume()
    {
        EnsureInitialized();
        ApplyVolumes();
        UpdateSliderDisplays();

        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sFXVolume);
        PlayerPrefs.SetFloat("TextVolume", textSoundVolume);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        SetSlidersVolume();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SetSlidersVolume();
    }

    public void SetSFXVolume(float value)
    {
        sFXVolume = Mathf.Clamp01(value);
        SetSlidersVolume();
    }

    public void SetTextVolume(float value)
    {
        textSoundVolume = Mathf.Clamp01(value);
        SetSlidersVolume();
    }

    public void SaveAudioSettings()
    {
        SetSlidersVolume();
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (Instance == this && paused)
            PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        if (Instance == this)
            PlayerPrefs.Save();
    }

    // ---------------------------------------------------------
    // SLIDERS DEL MENÚ
    // ---------------------------------------------------------

    private void FindSliders()
    {
        DisconnectSliders();

        Slider[] sliders = FindObjectsOfType<Slider>(true);

        foreach (Slider slider in sliders)
        {
            switch (slider.gameObject.name)
            {
                case "MasterSlider":
                    masterSlider = slider;
                    break;
                case "MusicSlider":
                    musicSlider = slider;
                    break;
                case "SFXSlider":
                    sfxSlider = slider;
                    break;
                case "TextSlider":
                    textSlider = slider;
                    break;
            }
        }

        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

        if (textSlider != null)
            textSlider.onValueChanged.AddListener(OnTextSliderChanged);

        UpdateSliderDisplays();
    }

    private void DisconnectSliders()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);

        if (textSlider != null)
            textSlider.onValueChanged.RemoveListener(OnTextSliderChanged);
    }

    private void UpdateSliderDisplays()
    {
        SetSliderDisplay(masterSlider, masterVolume);
        SetSliderDisplay(musicSlider, musicVolume);
        SetSliderDisplay(sfxSlider, sFXVolume);
        SetSliderDisplay(textSlider, textSoundVolume);
    }

    private void SetSliderDisplay(Slider slider, float volume)
    {
        if (slider == null)
            return;

        float value = Mathf.Lerp(
            slider.minValue,
            slider.maxValue,
            volume);

        slider.SetValueWithoutNotify(value);
    }

    private void OnMasterSliderChanged(float value)
    {
        SetMasterVolume(masterSlider.normalizedValue);
    }

    private void OnMusicSliderChanged(float value)
    {
        SetMusicVolume(musicSlider.normalizedValue);
    }

    private void OnSFXSliderChanged(float value)
    {
        SetSFXVolume(sfxSlider.normalizedValue);
    }

    private void OnTextSliderChanged(float value)
    {
        SetTextVolume(textSlider.normalizedValue);
    }
}