using System.Runtime.InteropServices;

public class IOSSTTProvider : INativeSTTProvider
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void NativeSTT_Initialize(string gameObjectName, string language);
    [DllImport("__Internal")] private static extern void NativeSTT_StartListening();
    [DllImport("__Internal")] private static extern void NativeSTT_StopListening();
    [DllImport("__Internal")] private static extern void NativeSTT_Destroy();
    [DllImport("__Internal")] private static extern bool NativeSTT_IsAvailable();
#endif

    public void Initialize(string gameObjectName, string language)
    {
#if UNITY_IOS && !UNITY_EDITOR
        NativeSTT_Initialize(gameObjectName, language);
#endif
    }

    public void StartListening(int silenceTimeout, int possibleSilenceTimeout, int minSpeechLength, int maxResults)
    {
#if UNITY_IOS && !UNITY_EDITOR
        NativeSTT_StartListening();
#endif
    }

    public void StopListening()
    {
#if UNITY_IOS && !UNITY_EDITOR
        NativeSTT_StopListening();
#endif
    }

    public void Destroy()
    {
#if UNITY_IOS && !UNITY_EDITOR
        NativeSTT_Destroy();
#endif
    }

    public bool IsAvailable()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return NativeSTT_IsAvailable();
#else
        return false;
#endif
    }
}
