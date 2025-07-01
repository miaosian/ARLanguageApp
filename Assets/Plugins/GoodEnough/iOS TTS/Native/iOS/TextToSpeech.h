#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>

@interface TextToSpeech : NSObject
{
    
}

// AVSpeechSynthesizerDelegate delegate methods
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
 didCancelSpeechUtterance:(AVSpeechUtterance *)utterance;
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
didContinueSpeechUtterance:(AVSpeechUtterance *)utterance;
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
 didFinishSpeechUtterance:(AVSpeechUtterance *)utterance;
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
  didPauseSpeechUtterance:(AVSpeechUtterance *)utterance;
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
  didStartSpeechUtterance:(AVSpeechUtterance *)utterance;
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer
willSpeakRangeOfSpeechString:(NSRange)characterRange
                utterance:(AVSpeechUtterance *)utterance;
@end

extern "C"
{
    typedef void (*speechUtteranceCallback)();
    typedef void (*speechStringUtteranceCallback)(long startIndex, long stringLength, char* utteranceSpeechString);
}

static speechUtteranceCallback _speechUtteranceCancelled;
static speechUtteranceCallback _speechUtteranceContinued;
static speechUtteranceCallback _speechUtteranceFinished;
static speechUtteranceCallback _speechUtterancePaused;
static speechUtteranceCallback _speechUtteranceStarted;
static speechStringUtteranceCallback _willSpeakPartOfString;
