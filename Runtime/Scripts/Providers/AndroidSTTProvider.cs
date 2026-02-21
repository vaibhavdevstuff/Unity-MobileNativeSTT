using UnityEngine;

public class AndroidSTTProvider : INativeSTTProvider
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _plugin;
#endif

    public void Initialize(string gameObjectName, string language)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var pluginClass = new AndroidJavaClass("com.gdev.nativestt.NativeSTTPlugin"))
        {
            _plugin = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
        }
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _plugin.Call("initialize", activity, gameObjectName, language);
        }
#endif
    }

    public void StartListening(int silenceTimeout, int possibleSilenceTimeout, int minSpeechLength, int maxResults)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_plugin != null)
            _plugin.Call("startListening", silenceTimeout, possibleSilenceTimeout, minSpeechLength, maxResults);
#endif
    }

    public void StopListening()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_plugin != null) _plugin.Call("stopListening");
#endif
    }

    public void Destroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_plugin != null)
        {
            _plugin.Call("destroy");
            _plugin.Dispose();
            _plugin = null;
        }
#endif
    }

    public bool IsAvailable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_plugin != null) return _plugin.Call<bool>("isAvailable");
#endif
        return false;
    }
}
