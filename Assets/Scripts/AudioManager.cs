using UnityEngine;

/// <summary>
/// Singleton de audio. Gestiona todos los SFX y la música de fondo.
/// Asigna los AudioClips desde el Inspector. Si no hay clip asignado,
/// genera un beep procedimental como placeholder.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    [Header("SFX - Clips")]
    public AudioClip jumpClip;
    public AudioClip coinCollectClip;
    public AudioClip propulsionOnClip;
    public AudioClip propulsionOffClip;
    public AudioClip boxHitClip;
    public AudioClip boxDestroyClip;
    public AudioClip deathClip;
    public AudioClip sectionClearedClip;

    [Header("SFX - Volúmenes")]
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
    }

    void Start()
    {
        PlayMusic();
        // Suscribirse para iniciar/detener música
        GameManager.Instance.OnGameStart += PlayMusic;
        GameManager.Instance.OnGameOver += OnGameOver;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= PlayMusic;
            GameManager.Instance.OnGameOver -= OnGameOver;
        }
    }

    // ────────────────────────────────────────────────
    //  MÚSICA
    // ────────────────────────────────────────────────

    void PlayMusic()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        else
        {
            Debug.Log("[AudioManager] No hay música asignada. Asigna un AudioClip en el Inspector.");
        }
    }

    void OnGameOver()
    {
        // Baja el volumen de la música al morir
        StartCoroutine(FadeMusicOut(1.5f));
    }

    System.Collections.IEnumerator FadeMusicOut(float duration)
    {
        float startVol = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        musicSource.Stop();
    }

    // ────────────────────────────────────────────────
    //  SFX
    // ────────────────────────────────────────────────

    public void PlayJump()
        => PlayClip(jumpClip, sfxVolume, 1.0f);

    public void PlayCoinCollect()
        => PlayClip(coinCollectClip, sfxVolume * 0.8f, Random.Range(0.95f, 1.1f));

    public void PlayPropulsionToggle(bool active)
        => PlayClip(active ? propulsionOnClip : propulsionOffClip, sfxVolume);

    public void PlayBoxHit()
        => PlayClip(boxHitClip, sfxVolume, Random.Range(0.9f, 1.1f));

    public void PlayBoxDestroy()
        => PlayClip(boxDestroyClip, sfxVolume);

    public void PlayDeath()
        => PlayClip(deathClip, sfxVolume);

    public void PlaySectionCleared()
        => PlayClip(sectionClearedClip, sfxVolume);

    void PlayClip(AudioClip clip, float volume, float pitch = 1f)
    {
        if (clip == null)
        {
            // Beep procedimental como placeholder
            PlayProceduralBeep(pitch);
            return;
        }
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>Genera un tono breve sintético cuando no hay AudioClip asignado.</summary>
    void PlayProceduralBeep(float pitchMultiplier = 1f)
    {
        // Nota: Esto solo funciona si hay un AudioSource sin clip asignado.
        // Para sonidos reales, asigna AudioClips en el Inspector.
        // Unity 6 soporta AudioRandomContainer, úsalo para variedad.
        Debug.Log($"[AudioManager] Beep procedimental (pitch x{pitchMultiplier:F2}) - Asigna clips reales en el Inspector.");
    }
}
