# Mobile Native STT — Native Platform Speech-to-Text for Unity

Offline speech-to-text using native platform APIs:
- **Android**: Google's `SpeechRecognizer` (same engine as Gboard/Assistant)
- **iOS**: Apple's `SFSpeechRecognizer` (same engine as Siri)

## Installation

In Unity: **Window → Package Manager → + → Add package from git URL**

```
https://github.com/YOUR_USERNAME/MobileNativeSTT.git
```

## Quick Start

1. Add the **NativeSTT** component to a GameObject in your scene
2. Subscribe to events and call `Initialize()` + `StartListening()`

```csharp
var stt = GetComponent<NativeSTT>();
stt.OnResult += (result) => Debug.Log(result.Text);
stt.OnPartialResult += (partial) => Debug.Log("..." + partial);
stt.Initialize("en-US");
stt.StartListening();
```

Or check `AutoStart` in the Inspector to start automatically.

## Inspector Settings

| Setting | Description |
|---------|-------------|
| **Language** | BCP-47 language code (e.g., `en-US`, `en-IN`, `hi-IN`) |
| **Continuous Listening** | Auto-restart after each utterance |
| **Auto Start** | Initialize and start on Awake |

## Events

| Event | Data | Description |
|-------|------|-------------|
| `OnResult` | `STTResult` | Final transcription result |
| `OnPartialResult` | `string` | Live partial text while speaking |
| `OnError` | `string` | Error message |
| `OnReadyForSpeech` | — | Recognizer is listening |
| `OnSpeechEnd` | — | User stopped speaking |

## Setup Requirements

### Android
- **Permission**: `RECORD_AUDIO` (requested at runtime in demo)
- **Offline model**: User must download the language pack:
  Settings → Google → Languages → Offline Speech Recognition
- Minimum API level: 21 (Android 5.0)

### iOS
- **Info.plist** entries (added automatically by Unity or manually):
  - `NSSpeechRecognitionUsageDescription` — why you need speech recognition
  - `NSMicrophoneUsageDescription` — why you need mic access
- On-device recognition: iOS 13+ (`requiresOnDeviceRecognition`)
- User must download the language: Settings → General → Keyboard → Dictation → Languages

## Package Structure

```
MobileNativeSTT/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
├── Runtime/
│   ├── MobileNativeSTT.asmdef
│   ├── Scripts/
│   │   ├── NativeSTT.cs
│   │   ├── STTResult.cs
│   │   ├── INativeSTTProvider.cs
│   │   └── Providers/
│   │       ├── AndroidSTTProvider.cs
│   │       ├── IOSSTTProvider.cs
│   │       └── EditorSTTProvider.cs
│   └── Plugins/
│       ├── Android/
│       │   └── NativeSTTPlugin.java
│       └── iOS/
│           ├── NativeSTTPlugin.h
│           └── NativeSTTPlugin.mm
└── Samples~/
    └── Demo/
        ├── MobileNativeSTT.Samples.asmdef
        ├── STTDemo.cs
        └── NativeSTTDemo.unity
```

## Samples

Import the **Demo** sample from the Package Manager window to get a ready-made scene with tap-to-toggle and hold-to-speak modes.
