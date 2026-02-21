#import "NativeSTTPlugin.h"
#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>

// Unity extern for sending messages back
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

@interface NativeSTTPlugin () <SFSpeechRecognizerDelegate>

@property (nonatomic, strong) SFSpeechRecognizer *speechRecognizer;
@property (nonatomic, strong) SFSpeechAudioBufferRecognitionRequest *recognitionRequest;
@property (nonatomic, strong) SFSpeechRecognitionTask *recognitionTask;
@property (nonatomic, strong) AVAudioEngine *audioEngine;
@property (nonatomic, copy) NSString *gameObjectName;
@property (nonatomic, copy) NSString *language;
@property (nonatomic, assign) BOOL isListening;

@end

@implementation NativeSTTPlugin

+ (instancetype)sharedInstance {
    static NativeSTTPlugin *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[NativeSTTPlugin alloc] init];
    });
    return instance;
}

- (void)initializeWithGameObject:(NSString *)gameObjectName language:(NSString *)language {
    self.gameObjectName = gameObjectName;
    self.language = language;
    self.audioEngine = [[AVAudioEngine alloc] init];
    
    NSLocale *locale = [[NSLocale alloc] initWithLocaleIdentifier:language];
    self.speechRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:locale];
    self.speechRecognizer.delegate = self;
    
    // Request speech recognition authorization
    [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
        switch (status) {
            case SFSpeechRecognizerAuthorizationStatusAuthorized:
                NSLog(@"[NativeSTT] Speech recognition authorized");
                break;
            case SFSpeechRecognizerAuthorizationStatusDenied:
                [self sendError:@"Speech recognition permission denied"];
                break;
            case SFSpeechRecognizerAuthorizationStatusRestricted:
                [self sendError:@"Speech recognition restricted on this device"];
                break;
            case SFSpeechRecognizerAuthorizationStatusNotDetermined:
                [self sendError:@"Speech recognition not determined"];
                break;
        }
    }];
}

- (void)startListening {
    if (self.recognitionTask) {
        [self.recognitionTask cancel];
        self.recognitionTask = nil;
    }
    
    // Configure audio session
    NSError *error = nil;
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    [audioSession setCategory:AVAudioSessionCategoryRecord
                         mode:AVAudioSessionModeMeasurement
                      options:AVAudioSessionCategoryOptionDuckOthers
                        error:&error];
    if (error) {
        [self sendError:[NSString stringWithFormat:@"Audio session error: %@", error.localizedDescription]];
        return;
    }
    
    [audioSession setActive:YES withOptions:AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation error:&error];
    if (error) {
        [self sendError:[NSString stringWithFormat:@"Audio session activation error: %@", error.localizedDescription]];
        return;
    }
    
    self.recognitionRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
    self.recognitionRequest.shouldReportPartialResults = YES;
    
    // Force on-device recognition (iOS 13+)
    if (@available(iOS 13.0, *)) {
        self.recognitionRequest.requiresOnDeviceRecognition = YES;
    }
    
    AVAudioInputNode *inputNode = self.audioEngine.inputNode;
    
    __weak typeof(self) weakSelf = self;
    self.recognitionTask = [self.speechRecognizer recognitionTaskWithRequest:self.recognitionRequest
                                                              resultHandler:^(SFSpeechRecognitionResult * _Nullable result, NSError * _Nullable error) {
        __strong typeof(weakSelf) strongSelf = weakSelf;
        if (!strongSelf) return;
        
        if (result) {
            NSString *text = result.bestTranscription.formattedString;
            float confidence = 0.0f;
            
            // Get confidence from first segment
            NSArray<SFTranscriptionSegment *> *segments = result.bestTranscription.segments;
            if (segments.count > 0) {
                float totalConf = 0.0f;
                for (SFTranscriptionSegment *segment in segments) {
                    totalConf += segment.confidence;
                }
                confidence = totalConf / segments.count;
            }
            
            if (result.isFinal) {
                [strongSelf sendResultWithText:text confidence:confidence];
                [strongSelf sendMessageToUnity:@"OnNativeSpeechEnd" message:@""];
            } else {
                [strongSelf sendMessageToUnity:@"OnNativePartialResult" message:text];
            }
        }
        
        if (error) {
            [strongSelf stopListeningInternal];
            
            // Don't send error for cancellation (normal stop)
            if (error.code != 216 && error.code != 1) {
                [strongSelf sendError:error.localizedDescription];
            }
        }
    }];
    
    // Install audio tap
    AVAudioFormat *recordingFormat = [inputNode outputFormatForBus:0];
    [inputNode installTapOnBus:0 bufferSize:1024 format:recordingFormat block:^(AVAudioPCMBuffer * _Nonnull buffer, AVAudioTime * _Nonnull when) {
        __strong typeof(weakSelf) strongSelf = weakSelf;
        if (strongSelf && strongSelf.recognitionRequest) {
            [strongSelf.recognitionRequest appendAudioPCMBuffer:buffer];
        }
    }];
    
    [self.audioEngine prepare];
    [self.audioEngine startAndReturnError:&error];
    if (error) {
        [self sendError:[NSString stringWithFormat:@"Audio engine error: %@", error.localizedDescription]];
        return;
    }
    
    self.isListening = YES;
    [self sendMessageToUnity:@"OnNativeReady" message:@""];
}

- (void)stopListening {
    self.isListening = NO;
    [self stopListeningInternal];
}

- (void)stopListeningInternal {
    if (self.audioEngine.isRunning) {
        [self.audioEngine stop];
        [self.audioEngine.inputNode removeTapOnBus:0];
    }
    
    if (self.recognitionRequest) {
        [self.recognitionRequest endAudio];
        self.recognitionRequest = nil;
    }
    
    if (self.recognitionTask) {
        [self.recognitionTask cancel];
        self.recognitionTask = nil;
    }
}

- (void)destroy {
    [self stopListening];
    self.speechRecognizer = nil;
    self.audioEngine = nil;
}

- (BOOL)isAvailable {
    return self.speechRecognizer != nil && self.speechRecognizer.isAvailable;
}

// ---- SFSpeechRecognizerDelegate ----

- (void)speechRecognizer:(SFSpeechRecognizer *)speechRecognizer availabilityDidChange:(BOOL)available {
    if (!available) {
        [self sendError:@"Speech recognition became unavailable"];
    }
}

// ---- Helpers ----

- (void)sendResultWithText:(NSString *)text confidence:(float)confidence {
    // Escape special characters for JSON
    NSString *escapedText = [text stringByReplacingOccurrencesOfString:@"\\" withString:@"\\\\"];
    escapedText = [escapedText stringByReplacingOccurrencesOfString:@"\"" withString:@"\\\""];
    escapedText = [escapedText stringByReplacingOccurrencesOfString:@"\n" withString:@"\\n"];
    
    NSString *json = [NSString stringWithFormat:
                      @"{\"text\":\"%@\",\"confidence\":%f,\"isFinal\":true,\"isOffline\":true}",
                      escapedText, confidence];
    [self sendMessageToUnity:@"OnNativeResult" message:json];
}

- (void)sendError:(NSString *)error {
    [self sendMessageToUnity:@"OnNativeError" message:error];
}

- (void)sendMessageToUnity:(NSString *)method message:(NSString *)message {
    UnitySendMessage(
        [self.gameObjectName UTF8String],
        [method UTF8String],
        [message UTF8String]
    );
}

@end

// ---- C Interface for Unity P/Invoke ----

extern "C" {

void NativeSTT_Initialize(const char* gameObjectName, const char* language) {
    NSString *goName = [NSString stringWithUTF8String:gameObjectName];
    NSString *lang = [NSString stringWithUTF8String:language];
    [[NativeSTTPlugin sharedInstance] initializeWithGameObject:goName language:lang];
}

void NativeSTT_StartListening() {
    [[NativeSTTPlugin sharedInstance] startListening];
}

void NativeSTT_StopListening() {
    [[NativeSTTPlugin sharedInstance] stopListening];
}

void NativeSTT_Destroy() {
    [[NativeSTTPlugin sharedInstance] destroy];
}

bool NativeSTT_IsAvailable() {
    return [[NativeSTTPlugin sharedInstance] isAvailable];
}

}
