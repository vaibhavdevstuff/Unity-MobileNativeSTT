using System;
using UnityEngine;

/// <summary>
/// Main speech-to-text MonoBehaviour.
/// Attach to a GameObject, call Initialize(), then StartListening().
/// </summary>
public class NativeSTT : MonoBehaviour
{
    [Header("Language")]
    [Tooltip("BCP-47 language code (e.g., en-US, hi-IN). Leave empty for device default.")]
    public string Language = "";

    [Header("Mode")]
    [Tooltip("Hold to speak, release to get result. When false, uses auto-detect mode.")]
    public bool PushToTalk = false;

    [Tooltip("Initialize and start listening on Awake.")]
    public bool AutoStart = false;

    [Header("Timing (Auto-Detect Mode)")]
    [Tooltip("Seconds of complete silence before ending recognition.")]
    [Range(0.5f, 10f)]
    public float SilenceTimeout = 3f;

    [Tooltip("Seconds of possible silence before ending recognition.")]
    [Range(0.5f, 10f)]
    public float PossibleSilenceTimeout = 3f;

    [Tooltip("Minimum seconds of speech before allowing end.")]
    [Range(1f, 30f)]
    public float MinSpeechLength = 5f;

    [Header("Recognition")]
    [Tooltip("Maximum number of alternative results.")]
    [Range(1, 5)]
    public int MaxResults = 1;

    [Header("Debug")]
    [Tooltip("Enable verbose debug logging.")]
    public bool DebugMode = false;

    // ---- Events ----

    /// <summary>Called when a final transcription result is available.</summary>
    public event Action<STTResult> OnResult;

    /// <summary>Called with partial/interim transcription text while user is speaking.</summary>
    public event Action<string> OnPartialResult;

    /// <summary>Called when an error occurs.</summary>
    public event Action<string> OnError;

    /// <summary>Called when the recognizer is ready and listening for speech.</summary>
    public event Action OnReadyForSpeech;

    /// <summary>Called when the recognizer detects the user has stopped speaking.</summary>
    public event Action OnSpeechEnd;

    // ---- State ----

    private INativeSTTProvider _provider;
    private bool _initialized;
    private bool _isListening;

    public bool IsListening => _isListening;
    public bool IsInitialized => _initialized;

    void Awake()
    {
        if (AutoStart)
        {
            Initialize();
            StartListening();
        }
    }

    /// <summary>
    /// Initialize the native speech recognizer.
    /// </summary>
    public void Initialize(string language = null)
    {
        if (_initialized)
        {
            Log("Already initialized.", true);
            return;
        }

        if (!string.IsNullOrEmpty(language))
            Language = language;

        _provider = CreateProvider();
        _provider.Initialize(gameObject.name, Language);
        _initialized = true;

        Log($"Initialized. Language: '{Language}', PushToTalk: {PushToTalk}, " +
            $"Silence: {SilenceTimeout}s, MinSpeech: {MinSpeechLength}s");
    }

    /// <summary>
    /// Start listening for speech input.
    /// </summary>
    public void StartListening()
    {
        if (!_initialized)
        {
            Log("Not initialized. Call Initialize() first.", true);
            return;
        }

        int silenceMs = Mathf.RoundToInt(SilenceTimeout * 1000);
        int possibleMs = Mathf.RoundToInt(PossibleSilenceTimeout * 1000);
        int minMs = Mathf.RoundToInt(MinSpeechLength * 1000);

        Log($"StartListening — silence: {silenceMs}ms, possibleSilence: {possibleMs}ms, " +
            $"minSpeech: {minMs}ms, maxResults: {MaxResults}");

        _provider.StartListening(silenceMs, possibleMs, minMs, MaxResults);
        _isListening = true;
    }

    /// <summary>
    /// Stop listening for speech input.
    /// </summary>
    public void StopListening()
    {
        if (!_initialized) return;
        _isListening = false;
        _provider.StopListening();
        Log("StopListening called.");
    }

    /// <summary>
    /// Check if speech recognition is available.
    /// </summary>
    public bool IsAvailable()
    {
        return _provider != null && _provider.IsAvailable();
    }

    void OnDestroy()
    {
        if (_provider != null)
        {
            _provider.Destroy();
            _provider = null;
            _initialized = false;
            _isListening = false;
        }
    }

    // ---- UnitySendMessage Callbacks ----

    private void OnNativeResult(string json)
    {
        _isListening = false;
        Log($"OnNativeResult raw JSON: {json}");

        try
        {
            var result = STTResult.FromJson(json);
            result.IsFinal = true;

            Log($"Parsed result — text: '{result.Text}', confidence: {result.Confidence:F2}");

            if (!string.IsNullOrEmpty(result.Text))
            {
                OnResult?.Invoke(result);
            }
            else
            {
                Log("Result text was empty, OnResult not invoked.", true);
            }
        }
        catch (Exception e)
        {
            Log($"Error parsing result: {e.Message}\nRaw: {json}", true);
        }
    }

    private void OnNativePartialResult(string text)
    {
        Log($"Partial: '{text}'");
        OnPartialResult?.Invoke(text);
    }

    private void OnNativeError(string error)
    {
        _isListening = false;
        Log($"Error from native: {error}", true);
        OnError?.Invoke(error);
    }

    private void OnNativeReady(string unused)
    {
        Log("Recognizer ready.");
        OnReadyForSpeech?.Invoke();
    }

    private void OnNativeSpeechEnd(string unused)
    {
        Log("Speech ended (silence detected).");
        OnSpeechEnd?.Invoke();
    }

    // ---- Debug Helper ----

    /// <summary>
    /// Log a message if DebugMode is enabled.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="isWarning">If true, logs as warning/error even when debug is off.</param>
    private void Log(string message, bool isWarning = false)
    {
        if (isWarning)
        {
            Debug.LogWarning($"[NativeSTT] {message}");
        }
        else if (DebugMode)
        {
            Debug.Log($"[NativeSTT] {message}");
        }
    }

    private INativeSTTProvider CreateProvider()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return new AndroidSTTProvider();
#elif UNITY_IOS && !UNITY_EDITOR
        return new IOSSTTProvider();
#else
        return new EditorSTTProvider();
#endif
    }
}
