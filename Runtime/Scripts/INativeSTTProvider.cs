/// <summary>
/// Interface for platform-specific STT implementations.
/// </summary>
public interface INativeSTTProvider
{
    void Initialize(string gameObjectName, string language);
    void StartListening(int silenceTimeout, int possibleSilenceTimeout, int minSpeechLength, int maxResults);
    void StopListening();
    void Destroy();
    bool IsAvailable();
}
