using UnityEngine;

public class EditorSTTProvider : INativeSTTProvider
{
    public void Initialize(string gameObjectName, string language)
    {
        Debug.LogWarning("[NativeSTT] Editor mode — build to Android/iOS for real STT.");
    }

    public void StartListening(int silenceTimeout, int possibleSilenceTimeout, int minSpeechLength, int maxResults)
    {
        Debug.LogWarning("[NativeSTT] Editor mode — StartListening has no effect.");
    }

    public void StopListening() { }
    public void Destroy() { }
    public bool IsAvailable() => false;
}
