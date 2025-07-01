#import "TextToSpeech.h"
#import "cstdlib"
@implementation TextToSpeech
static TextToSpeech *_instance;
+(TextToSpeech*) instance
{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        _instance = [[TextToSpeech alloc] init];
    });
    return _instance;
}
-(id)init
{
    self = [super init];
    [synthesizer setDelegate:(id<AVSpeechSynthesizerDelegate>)self];
    return self;
}
static AVSpeechSynthesizer* synthesizer = NULL;
-(void)Speak: (NSString*) textToSpeak
       pitch: (float) pitch
        preUtteranceDelay: (double) preUtteranceDelay
        postUtteranceDelay: (double) postUtteranceDelay
        rate: (float) rate
        voiceIdentifier: (NSString*) voiceIdentifier
        volume: (float) volume
{
    if(synthesizer == NULL)
		synthesizer = [[AVSpeechSynthesizer alloc] init];
	
	[synthesizer setDelegate:(id<AVSpeechSynthesizerDelegate>)self];
	
    AVSpeechUtterance *speechUtterance = CreateSpeechUtterance(textToSpeak, pitch, preUtteranceDelay, postUtteranceDelay, rate, voiceIdentifier, volume);
    [synthesizer speakUtterance:speechUtterance];
}
-(bool)PauseSpeakingEndOfWord
{
    return [synthesizer pauseSpeakingAtBoundary:AVSpeechBoundaryWord];
}
-(bool)PauseSpeakingImmediate
{
    return [synthesizer pauseSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}
-(bool)ContinueSpeaking
{
    return [synthesizer continueSpeaking];
}
-(bool)StopSpeakingEndOfWord
{
    return [synthesizer stopSpeakingAtBoundary:AVSpeechBoundaryWord];
}
-(bool)StopSpeakingImmediate
{
    return [synthesizer stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}
-(bool)isPaused
{
    return [synthesizer isPaused];
}
-(bool)isSpeaking
{
    return [synthesizer isSpeaking];
}
-(float)UtteranceMinimumSpeechRate
{
    return AVSpeechUtteranceMinimumSpeechRate;
}
-(float)UtteranceMaximumSpeechRate
{
    return AVSpeechUtteranceMaximumSpeechRate;
}
-(float)UtteranceDefaultSpeechRate
{
    return AVSpeechUtteranceDefaultSpeechRate;
}
-(NSString*) CurrentLanguageCode
{
    return AVSpeechSynthesisVoice.currentLanguageCode;
}
AVSpeechUtterance* CreateSpeechUtterance (NSString* textToSpeak, float pitch = 1, double preUtteranceDelay = 0, double postUtteranceDelay = 0, float rate = 1, NSString* voiceIdentifier = nil, float volume = 1.0)
{
    AVSpeechUtterance *speechUtterance = [AVSpeechUtterance speechUtteranceWithString:textToSpeak];
    speechUtterance.pitchMultiplier = pitch;
    speechUtterance.preUtteranceDelay = preUtteranceDelay;
    speechUtterance.postUtteranceDelay = postUtteranceDelay;
    speechUtterance.rate = rate;
    speechUtterance.voice = GetVoice(voiceIdentifier);
    speechUtterance.volume = volume;
    return speechUtterance;
}
-(long)GetNumberOfAvailableVoices
{
    return GetAllAvailableVoices().count;
}
-(NSString*) GetVoiceIdentifier: (int) voiceIndex
{
    return [GetAllAvailableVoices() objectAtIndex:voiceIndex].identifier;
}
-(NSString*) GetVoiceIdentifierFromLanguageCode: (NSString*) languageCode
{
    AVSpeechSynthesisVoice* voice = GetVoiceFromLanguageCode(languageCode);
    if(voice != nil)
        return GetVoiceFromLanguageCode(languageCode).identifier;
    else
        return [[NSString alloc] init];
}
-(NSString*) GetVoiceName: (int) voiceIndex
{
    return [GetAllAvailableVoices() objectAtIndex:voiceIndex].name;
}
-(NSString*) GetVoiceLanguage: (int) voiceIndex
{
    return [GetAllAvailableVoices() objectAtIndex:voiceIndex].language;
}
-(int) GetVoiceQuality: (int) voiceIndex
{
    return [GetAllAvailableVoices() objectAtIndex:voiceIndex].quality;
}
NSArray<AVSpeechSynthesisVoice*>* GetAllAvailableVoices()
{
    return AVSpeechSynthesisVoice.speechVoices;
}
AVSpeechSynthesisVoice* GetVoice (NSString* identifier)
{
    return [AVSpeechSynthesisVoice voiceWithIdentifier:identifier];
}
AVSpeechSynthesisVoice* GetVoiceFromLanguageCode (NSString* language)
{
    return [AVSpeechSynthesisVoice voiceWithLanguage:language];
}
//Delegates
//Tells the delegate when the synthesizer has canceled speaking an utterance.
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
 didCancelSpeechUtterance:(AVSpeechUtterance *)utterance
{
    if (_speechUtteranceCancelled != NULL)
    {
        _speechUtteranceCancelled();
    }
}
//Tells the delegate when the synthesizer has resumed speaking an utterance after being paused.
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
didContinueSpeechUtterance:(AVSpeechUtterance *)utterance
{
    if (_speechUtteranceContinued != NULL)
    {
        _speechUtteranceContinued();
    }
}
//Tells the delegate when the synthesizer has finished speaking an utterance.
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
 didFinishSpeechUtterance:(AVSpeechUtterance *)utterance
{
    if (_speechUtteranceFinished != NULL)
    {
        _speechUtteranceFinished();
    }
}
//Tells the delegate when the synthesizer has paused while speaking an utterance.
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
  didPauseSpeechUtterance:(AVSpeechUtterance *)utterance
{
    if (_speechUtterancePaused != NULL)
    {
        _speechUtterancePaused();
    }
}
//Tells the delegate when the synthesizer has begun speaking an utterance.
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
  didStartSpeechUtterance:(AVSpeechUtterance *)utterance
{
    if (_speechUtteranceStarted != NULL)
    {
        _speechUtteranceStarted();
    }
}
//Tells the delegate when the synthesizer is about to speak a portion of an utterance's text.
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
willSpeakRangeOfSpeechString:(NSRange)characterRange
                utterance:(AVSpeechUtterance *)utterance
{
    if (_willSpeakPartOfString != NULL)
    {
        _willSpeakPartOfString(characterRange.location, characterRange.length, ToChar(utterance.speechString));
    }
}
// Converts C style string to NSString
NSString* CreateNSString (const char* string)
{
    if (string)
        return [NSString stringWithUTF8String: string];
    else
        return [NSString stringWithUTF8String: ""];
}
char* ToChar(NSString* nsstring)
{
    const char* string = [nsstring UTF8String];
    if (string == NULL)
        return NULL;
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

@end
extern "C"
{
    void _SetupCallbacks(speechUtteranceCallback speechUtteranceCancelled, speechUtteranceCallback speechUtteranceContinued, speechUtteranceCallback speechUtteranceFinished, speechUtteranceCallback speechUtterancePaused, speechUtteranceCallback speechUtteranceStarted, speechStringUtteranceCallback willSpeakPartOfString)
    {
        _speechUtteranceCancelled = speechUtteranceCancelled;
        _speechUtteranceContinued = speechUtteranceContinued;
        _speechUtteranceFinished = speechUtteranceFinished;
        _speechUtterancePaused = speechUtterancePaused;
        _speechUtteranceStarted = speechUtteranceStarted;
        _willSpeakPartOfString = willSpeakPartOfString;
    }
    void _Speak(const char* textToSpeak, float pitch = 1, double preUtteranceDelay = 0, double postUtteranceDelay = 0, float rate = 1, const char* voiceIdentifier = nil, float volume = 1.0)
    {
        [[TextToSpeech instance] Speak: CreateNSString(textToSpeak) pitch: pitch preUtteranceDelay: preUtteranceDelay postUtteranceDelay: postUtteranceDelay rate: rate voiceIdentifier: CreateNSString(voiceIdentifier)  volume: volume];
    }
    bool _PauseSpeakingEndOfWord()
    {
        return [[TextToSpeech instance] PauseSpeakingEndOfWord];
    }
    bool _PauseSpeakingImmediate()
    {
        return [[TextToSpeech instance] PauseSpeakingImmediate];
    }
    bool _ContinueSpeaking()
    {
        return [[TextToSpeech instance] ContinueSpeaking];
    }
    bool _StopSpeakingEndOfWord()
    {
        return [[TextToSpeech instance] StopSpeakingEndOfWord];
    }
    bool _StopSpeakingImmediate()
    {
        return [[TextToSpeech instance] StopSpeakingImmediate];
    }
    bool _isPaused()
    {
        return [[TextToSpeech instance] isPaused];
    }
    bool _isSpeaking()
    {
        return [[TextToSpeech instance] isSpeaking];
    }
    float _UtteranceMinimumSpeechRate()
    {
        return [[TextToSpeech instance] UtteranceMinimumSpeechRate];
    }
    float _UtteranceMaximumSpeechRate()
    {
        return [[TextToSpeech instance] UtteranceMaximumSpeechRate];
    }
    float _UtteranceDefaultSpeechRate()
    {
        return [[TextToSpeech instance] UtteranceDefaultSpeechRate];
    }
    long _GetNumberOfAvailableVoices()
    {
        return [[TextToSpeech instance] GetNumberOfAvailableVoices];
    }
    char* _GetVoiceIdentifier(int index)
    {
        return ToChar([[TextToSpeech instance] GetVoiceIdentifier: index]);
    }
    char* _GetVoiceIdentifierFromLanguageCode(const char* languageCode)
    {
        return ToChar([[TextToSpeech instance] GetVoiceIdentifierFromLanguageCode: CreateNSString(languageCode)]);
    }
    char* _GetVoiceName(int index)
    {
        NSString* name = [[TextToSpeech instance] GetVoiceName: index];
        return ToChar(name);
    }
    char* _GetVoiceLanguage(int index)
    {
        NSString* language = [[TextToSpeech instance] GetVoiceLanguage: index];
        return ToChar(language);
    }
    int _GetVoiceQuality(int index)
    {
        return [[TextToSpeech instance] GetVoiceQuality: index];
    }
    char* _CurrentLanguageCode()
    {
        return ToChar([[TextToSpeech instance] CurrentLanguageCode]);
    }
}