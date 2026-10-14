using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Audio manager handling metronome ticks, nail placement, hammer strikes, and UI sounds.
/// Routes audio output to the Master.mixer SFX group (Mixers/Master/SFX).
/// Provides procedural sound synthesis as fallback if dedicated SFX clips are not yet provided.
/// </summary>
[DefaultExecutionOrder(-80)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Routing")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup _sfxGroup;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _metronomeTickClip;
    [SerializeField] private AudioClip _nailPlaceClip;
    [SerializeField] private AudioClip _hammerHitPerfectClip;
    [SerializeField] private AudioClip _hammerHitGoodClip;
    [SerializeField] private AudioClip _hammerHitBadClip;
    [SerializeField] private AudioClip _missClip;
    [SerializeField] private AudioClip _fanfareClip;

    [Header("Volume Controls")]
    [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1.0f;
    [Range(0f, 1f)] [SerializeField] private float _metronomeVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.9f;

    private AudioSource _metronomeSource;
    private AudioSource _sfxSource;

    // Procedurally generated fallback clips
    private AudioClip _procTickNormal;
    private AudioClip _procTickAccent;
    private AudioClip _procNailPlace;
    private AudioClip _procHammerPerfect;
    private AudioClip _procHammerGood;
    private AudioClip _procHammerBad;
    private AudioClip _procMiss;

    public AudioMixer AudioMixer => _audioMixer;
    public AudioMixerGroup SfxGroup => _sfxGroup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            var existing = FindAnyObjectByType<AudioManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var prefab = Resources.Load<GameObject>("AudioManager");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.name = "[AudioManager]";
                DontDestroyOnLoad(go);
            }
            else
            {
                var go = new GameObject("[AudioManager]");
                go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        EnsureMixerLoaded();
        LoadDefaultClips();
        GenerateProceduralClips();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeAudioSources()
    {
        if (_metronomeSource == null)
        {
            _metronomeSource = gameObject.AddComponent<AudioSource>();
            _metronomeSource.playOnAwake = false;
            _metronomeSource.spatialBlend = 0f; // 2D sound
        }

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f; // 2D sound
        }

        ApplyMixerRouting();
    }

    public void EnsureMixerLoaded()
    {
#if UNITY_EDITOR
        if (_audioMixer == null)
        {
            _audioMixer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Scenes/Master.mixer");
        }
        if (_audioMixer != null && _sfxGroup == null)
        {
            var groups = _audioMixer.FindMatchingGroups("Master/SFX");
            if (groups != null && groups.Length > 0)
            {
                _sfxGroup = groups[0];
            }
            else
            {
                var fallback = _audioMixer.FindMatchingGroups("SFX");
                if (fallback != null && fallback.Length > 0)
                {
                    _sfxGroup = fallback[0];
                }
            }
        }
#endif
        ApplyMixerRouting();
    }

    public void ApplyMixerRouting()
    {
        if (_sfxGroup != null)
        {
            if (_metronomeSource != null) _metronomeSource.outputAudioMixerGroup = _sfxGroup;
            if (_sfxSource != null) _sfxSource.outputAudioMixerGroup = _sfxGroup;
        }
    }

    private void LoadDefaultClips()
    {
#if UNITY_EDITOR
        if (_metronomeTickClip == null)
        {
            _metronomeTickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/metronome-tick.mp3");
        }
#endif
    }

    private void GenerateProceduralClips()
    {
        // 44100Hz audio generation
        int sampleRate = 44100;

        _procTickNormal = CreateToneClip("ProcTickNormal", sampleRate, 0.04f, 880f, 0.8f);
        _procTickAccent = CreateToneClip("ProcTickAccent", sampleRate, 0.06f, 1320f, 1.0f);
        _procNailPlace = CreateNoisePopClip("ProcNailPlace", sampleRate, 0.05f, 600f, 0.7f);
        _procHammerPerfect = CreateMetallicHitClip("ProcHammerPerfect", sampleRate, 0.12f, 1600f, 1.0f);
        _procHammerGood = CreateMetallicHitClip("ProcHammerGood", sampleRate, 0.10f, 1100f, 0.85f);
        _procHammerBad = CreateMetallicHitClip("ProcHammerBad", sampleRate, 0.10f, 650f, 0.7f);
        _procMiss = CreateDullThudClip("ProcMiss", sampleRate, 0.08f, 220f, 0.5f);
    }

    private static AudioClip CreateToneClip(string name, int sampleRate, float duration, float frequency, float volume)
    {
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 80f); // fast decay
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }

        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateNoisePopClip(string name, int sampleRate, float duration, float centerFreq, float volume)
    {
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 90f);
            float noise = (Random.value * 2f - 1f) * 0.4f;
            float tone = Mathf.Sin(2f * Mathf.PI * centerFreq * t) * 0.6f;
            data[i] = (tone + noise) * envelope * volume;
        }

        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateMetallicHitClip(string name, int sampleRate, float duration, float pitch, float volume)
    {
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 35f);
            float harmonic1 = Mathf.Sin(2f * Mathf.PI * pitch * t);
            float harmonic2 = Mathf.Sin(2f * Mathf.PI * (pitch * 1.62f) * t) * 0.6f;
            float harmonic3 = Mathf.Sin(2f * Mathf.PI * (pitch * 2.76f) * t) * 0.3f;
            float noise = (Random.value * 2f - 1f) * 0.2f * Mathf.Exp(-t * 120f);
            data[i] = (harmonic1 + harmonic2 + harmonic3 + noise) * envelope * volume * 0.5f;
        }

        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateDullThudClip(string name, int sampleRate, float duration, float freq, float volume)
    {
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 45f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
        }

        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// Plays metronome tick. Downbeats (beat 1) are pitched higher/accented.
    /// </summary>
    public void PlayTick(bool isAccent = false)
    {
        if (_metronomeSource == null) return;

        float vol = _masterVolume * _metronomeVolume;
        if (_metronomeTickClip != null)
        {
            _metronomeSource.pitch = isAccent ? 1.35f : 1.0f;
            _metronomeSource.PlayOneShot(_metronomeTickClip, vol);
        }
        else
        {
            AudioClip clip = isAccent ? _procTickAccent : _procTickNormal;
            _metronomeSource.pitch = 1.0f;
            _metronomeSource.PlayOneShot(clip, vol);
        }
    }

    /// <summary>
    /// Plays nail placement sound (called when attacker places a nail).
    /// </summary>
    public void PlayNailPlace()
    {
        if (_sfxSource == null) return;
        AudioClip clip = _nailPlaceClip != null ? _nailPlaceClip : _procNailPlace;
        _sfxSource.pitch = Random.Range(0.95f, 1.05f);
        _sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume);
    }

    /// <summary>
    /// Plays hammer hit sound depending on judgment tier (Perfect, Good, Bad).
    /// </summary>
    public void PlayHammerHit(HitTier tier)
    {
        if (_sfxSource == null) return;

        AudioClip clip = null;
        float pitch = 1.0f;

        switch (tier)
        {
            case HitTier.Perfect:
                clip = _hammerHitPerfectClip != null ? _hammerHitPerfectClip : _procHammerPerfect;
                pitch = 1.05f;
                break;
            case HitTier.Good:
                clip = _hammerHitGoodClip != null ? _hammerHitGoodClip : _procHammerGood;
                pitch = 1.0f;
                break;
            case HitTier.Bad:
                clip = _hammerHitBadClip != null ? _hammerHitBadClip : _procHammerBad;
                pitch = 0.9f;
                break;
            case HitTier.Miss:
                PlayMiss();
                return;
        }

        if (clip != null)
        {
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume);
        }
    }

    /// <summary>
    /// Plays miss sound (timeout or miss hit).
    /// </summary>
    public void PlayMiss()
    {
        if (_sfxSource == null) return;
        AudioClip clip = _missClip != null ? _missClip : _procMiss;
        _sfxSource.pitch = 0.95f;
        _sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume * 0.7f);
    }

    /// <summary>
    /// Plays fanfare or round swap sound.
    /// </summary>
    public void PlayFanfare()
    {
        if (_sfxSource == null) return;
        if (_fanfareClip != null)
        {
            _sfxSource.pitch = 1.0f;
            _sfxSource.PlayOneShot(_fanfareClip, _masterVolume * _sfxVolume);
        }
        else
        {
            _sfxSource.pitch = 1.5f;
            _sfxSource.PlayOneShot(_procHammerPerfect, _masterVolume * _sfxVolume);
        }
    }
}
