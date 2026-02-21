using UnityEngine;

/// <summary>
/// Represents a speech-to-text recognition result.
/// Field names MUST be lowercase to match JSON from native plugins.
/// </summary>
[System.Serializable]
public class STTResult
{
    // Lowercase to match JSON from native plugins (JsonUtility is case-sensitive)
    public string text = "";
    public float confidence;
    public bool isFinal;

    // Public properties for clean access
    public string Text => text;
    public float Confidence => confidence;
    public bool IsFinal
    {
        get => isFinal;
        set => isFinal = value;
    }

    public STTResult() { }

    public STTResult(string text, float confidence, bool isFinal)
    {
        this.text = text;
        this.confidence = confidence;
        this.isFinal = isFinal;
    }

    public static STTResult FromJson(string json)
    {
        return JsonUtility.FromJson<STTResult>(json);
    }
}
