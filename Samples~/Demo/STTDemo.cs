using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Demo: supports both tap-to-toggle and hold-to-speak modes.
/// Mode is determined by NativeSTT.PushToTalk setting.
/// </summary>
[RequireComponent(typeof(NativeSTT))]
public class STTDemo : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text ResultText;
    public TMP_Text PartialText;
    public TMP_Text StatusText;
    public Button ListenButton;

    [Header("Push-to-Talk Settings")]
    [Tooltip("Delay in seconds after releasing before stopping recognizer. Gives it time to process final words.")]
    public float ReleaseDelay = 0.5f;

    private NativeSTT _stt;

    void Start()
    {
        _stt = GetComponent<NativeSTT>();

        _stt.OnResult += OnResult;
        _stt.OnPartialResult += OnPartialResult;
        _stt.OnError += OnError;
        _stt.OnReadyForSpeech += () => SetStatus("Listening...");
        _stt.OnSpeechEnd += () => SetStatus("Processing...");

        if (ListenButton != null)
        {
            if (_stt.PushToTalk)
            {
                var trigger = ListenButton.gameObject.AddComponent<EventTrigger>();

                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener(_ => OnPushDown());
                trigger.triggers.Add(down);

                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener(_ => OnPushUp());
                trigger.triggers.Add(up);

                SetButtonText("Hold to Speak");
            }
            else
            {
                ListenButton.onClick.AddListener(OnToggle);
                SetButtonText("Listen");
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(
                UnityEngine.Android.Permission.Microphone);
        }
#endif

        _stt.Initialize();
        SetStatus("Ready.");
    }

    // ---- Push-to-Talk ----

    void OnPushDown()
    {
        if (PartialText != null) PartialText.text = "";
        _stt.StartListening();
        SetStatus("Listening...");
        SetButtonText("Speaking...");
    }

    void OnPushUp()
    {
        SetStatus("Processing...");
        SetButtonText("Hold to Speak");
        // Delay stop so recognizer can finish processing last words
        StartCoroutine(DelayedStop());
    }

    IEnumerator DelayedStop()
    {
        yield return new WaitForSeconds(ReleaseDelay);
        _stt.StopListening();
    }

    // ---- Toggle Mode ----

    void OnToggle()
    {
        if (_stt.IsListening)
        {
            _stt.StopListening();
            SetStatus("Stopped.");
            SetButtonText("Listen");
        }
        else
        {
            if (PartialText != null) PartialText.text = "";
            _stt.StartListening();
            SetButtonText("Stop");
        }
    }

    // ---- Events ----

    void OnResult(STTResult result)
    {
        Debug.Log($"[STTDemo] Result: {result.Text} (confidence: {result.Confidence:F2})");
        if (ResultText != null) ResultText.text = result.Text;
        if (PartialText != null) PartialText.text = "";
        SetStatus("Done.");
        SetButtonText(_stt.PushToTalk ? "Hold to Speak" : "Listen");
    }

    void OnPartialResult(string partial)
    {
        Debug.Log($"[STTDemo] Partial: {partial}");
        if (PartialText != null) PartialText.text = partial;
    }

    void OnError(string error)
    {
        Debug.LogError($"[STTDemo] Error: {error}");
        SetStatus("Error: " + error);
        SetButtonText(_stt.PushToTalk ? "Hold to Speak" : "Listen");
    }

    void SetStatus(string s)
    {
        if (StatusText != null) StatusText.text = s;
    }

    void SetButtonText(string t)
    {
        if (ListenButton != null)
        {
            var label = ListenButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = t;
        }
    }

    void OnDestroy()
    {
        if (_stt != null)
        {
            _stt.OnResult -= OnResult;
            _stt.OnPartialResult -= OnPartialResult;
            _stt.OnError -= OnError;
        }
    }
}
