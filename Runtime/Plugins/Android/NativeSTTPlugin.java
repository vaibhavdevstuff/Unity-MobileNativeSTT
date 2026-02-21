package com.gdev.nativestt;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;
import java.util.Locale;

/**
 * Android native plugin wrapping SpeechRecognizer for Unity.
 */
public class NativeSTTPlugin implements RecognitionListener {

    private static NativeSTTPlugin instance;

    private Activity activity;
    private SpeechRecognizer speechRecognizer;
    private String gameObjectName;
    private String language;
    private boolean isListening;
    private String lastPartialResult = "";

    public static NativeSTTPlugin getInstance() {
        if (instance == null) {
            instance = new NativeSTTPlugin();
        }
        return instance;
    }

    public void initialize(final Activity activity, final String gameObjectName, final String language) {
        this.activity = activity;
        this.gameObjectName = gameObjectName;
        this.language = language;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null) {
                    speechRecognizer.destroy();
                }
                speechRecognizer = SpeechRecognizer.createSpeechRecognizer(activity);
                speechRecognizer.setRecognitionListener(NativeSTTPlugin.this);
            }
        });
    }

    /**
     * Start listening with configurable timing parameters.
     * @param silenceMs         Complete silence before ending (ms)
     * @param possibleSilenceMs Possible silence before ending (ms)
     * @param minSpeechMs       Minimum speech length (ms)
     * @param maxResults        Max alternative results
     */
    public void startListening(final int silenceMs, final int possibleSilenceMs,
                               final int minSpeechMs, final int maxResults) {
        if (activity == null || speechRecognizer == null) return;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    speechRecognizer.cancel();
                } catch (Exception e) { }

                Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL,
                        RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);

                if (language != null && !language.isEmpty()) {
                    intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, language);
                }

                intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
                intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, maxResults);

                // Configurable silence/timing detection
                intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, (long) silenceMs);
                intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, (long) possibleSilenceMs);
                intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_MINIMUM_LENGTH_MILLIS, (long) minSpeechMs);

                try {
                    lastPartialResult = "";
                    speechRecognizer.startListening(intent);
                    isListening = true;
                } catch (Exception e) {
                    sendError("Failed to start listening: " + e.getMessage());
                }
            }
        });
    }

    public void stopListening() {
        if (activity == null || speechRecognizer == null) return;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                isListening = false;
                try {
                    speechRecognizer.stopListening();
                } catch (Exception e) { }
            }
        });
    }

    public void destroy() {
        if (activity == null) return;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                isListening = false;
                if (speechRecognizer != null) {
                    speechRecognizer.destroy();
                    speechRecognizer = null;
                }
            }
        });
    }

    public boolean isAvailable() {
        if (activity == null) return false;
        return SpeechRecognizer.isRecognitionAvailable(activity);
    }

    // ---- RecognitionListener ----

    @Override
    public void onReadyForSpeech(Bundle params) {
        UnityPlayer.UnitySendMessage(gameObjectName, "OnNativeReady", "");
    }

    @Override public void onBeginningOfSpeech() { }
    @Override public void onRmsChanged(float rmsdB) { }
    @Override public void onBufferReceived(byte[] buffer) { }

    @Override
    public void onEndOfSpeech() {
        UnityPlayer.UnitySendMessage(gameObjectName, "OnNativeSpeechEnd", "");
    }

    @Override
    public void onError(int error) {
        switch (error) {
            case SpeechRecognizer.ERROR_NO_MATCH:
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT:
            case SpeechRecognizer.ERROR_CLIENT:
                // Non-fatal — send whatever partial we have
                sendResult(lastPartialResult, 0f);
                return;
            case SpeechRecognizer.ERROR_RECOGNIZER_BUSY:
                return;
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS:
                sendError("Insufficient permissions. Grant RECORD_AUDIO permission.");
                return;
            default:
                sendError("Error code: " + error);
                return;
        }
    }

    @Override
    public void onResults(Bundle results) {
        ArrayList<String> matches = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        float[] confidences = results.getFloatArray(SpeechRecognizer.CONFIDENCE_SCORES);

        if (matches != null && !matches.isEmpty() && !matches.get(0).isEmpty()) {
            String text = matches.get(0);
            float confidence = (confidences != null && confidences.length > 0) ? confidences[0] : 0f;
            sendResult(text, confidence);
        } else {
            sendResult(lastPartialResult, 0f);
        }
    }

    @Override
    public void onPartialResults(Bundle partialResults) {
        ArrayList<String> matches = partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches != null && !matches.isEmpty()) {
            String text = matches.get(0);
            lastPartialResult = text;
            UnityPlayer.UnitySendMessage(gameObjectName, "OnNativePartialResult", text);
        }
    }

    @Override public void onEvent(int eventType, Bundle params) { }

    // ---- Helpers ----

    private void sendResult(String text, float confidence) {
        String finalText = (text != null && !text.isEmpty()) ? text : lastPartialResult;
        String json = String.format(Locale.US,
                "{\"text\":\"%s\",\"confidence\":%f,\"isFinal\":true}",
                escapeJson(finalText), confidence);
        lastPartialResult = "";
        isListening = false;
        UnityPlayer.UnitySendMessage(gameObjectName, "OnNativeResult", json);
    }

    private void sendError(String message) {
        isListening = false;
        UnityPlayer.UnitySendMessage(gameObjectName, "OnNativeError", message);
    }

    private String escapeJson(String text) {
        if (text == null) return "";
        return text.replace("\\", "\\\\")
                   .replace("\"", "\\\"")
                   .replace("\n", "\\n")
                   .replace("\r", "\\r")
                   .replace("\t", "\\t");
    }
}
