using System.Collections.ObjectModel;
using System.Linq;
using AOT;

namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// The Speech Synthesis framework manages voices and speech synthesis for iOS, tvOS, and watchOS. 
    /// </summary>
    public class TTS
    {
        private readonly SpeechUtteranceParameters _defaultParameters;

        private static TTS _instance;
        private static TTS Instance
        {
            get { return _instance ?? (_instance = new TTS()); }
        }

        private TTS()
        {
            _allAvailableVoices = InitializeVoices();
            _defaultParameters = new SpeechUtteranceParameters();
            SetupCallbacks();
        }

        private readonly ReadOnlyCollection<ISpeechSynthesisVoice> _allAvailableVoices;

        #region Setup

        private ReadOnlyCollection<ISpeechSynthesisVoice> InitializeVoices()
        {
            var availableVoicesCount = TTS_iOS.GetNumberOfAvailableVoices();
            var availableVoices = new ISpeechSynthesisVoice[availableVoicesCount];

            for (var voiceIndex = 0; voiceIndex < availableVoicesCount; voiceIndex++)
            {
                var identifier = TTS_iOS.GetVoiceIdentifier(voiceIndex);
                var name = TTS_iOS.GetVoiceName(voiceIndex);
                var language = TTS_iOS.GetVoiceLanguage(voiceIndex);
                var quality = TTS_iOS.GetVoiceQuality(voiceIndex);
                availableVoices[voiceIndex] =
                    new SpeechSynthesisVoice(identifier, name, language, (VoiceQuality) quality);
            }

            return new ReadOnlyCollection<ISpeechSynthesisVoice>(availableVoices);
        }

        private void SetupCallbacks()
        {
            TTS_iOS.SetupCallbacks(OnSpeechCancelledCallback, OnSpeechContinuedCallback,
                OnSpeechFinishedCallback, OnSpeechPausedCallback, OnSpeechStartedCallback, OnWillSpeakCallback);
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Called when the synthesizer has canceled speaking an utterance.
        /// Can assign a method with no parameters
        /// OnSpeechCancelled += ()=> Debug.Log("Utterance Cancelled");
        /// 
        /// </summary>
        public static SpeechUtteranceCallback OnSpeechCancelled;

        /// <summary>
        /// Called when the synthesizer has resumed speaking an utterance after being paused.
        /// Can assign a method with no parameters
        /// OnSpeechContinued += ()=> Debug.Log("Utterance Continued");
        /// </summary>
        public static SpeechUtteranceCallback OnSpeechContinued;

        /// <summary>
        ///Called when the synthesizer has finished speaking an utterance.
        /// Can assign a method with no parameters
        /// OnSpeechFinished += ()=> Debug.Log("Utterance Finished");
        /// </summary>
        public static SpeechUtteranceCallback OnSpeechFinished;

        /// <summary>
        /// Called when the synthesizer has paused while speaking an utterance.
        /// Can assign a method with no parameters
        /// OnSpeechPaused += ()=> Debug.Log("Utterance Paused");
        /// </summary>
        public static SpeechUtteranceCallback OnSpeechPaused;

        /// <summary>
        /// Called when the synthesizer has begun speaking an utterance.
        /// Can assign a method with no parameters
        /// OnSpeechStarted += ()=> Debug.Log("Utterance Started");
        /// </summary>
        public static SpeechUtteranceCallback OnSpeechStarted;

        /// <summary>
        /// Called when the synthesizer is about to speak a portion of an utterance’s speechString.
        /// Can assign a method with 3 parameters (int startIndex, int stringLength, string utteranceSpeechString)
        /// </summary>
        public static StringSpeechUtteranceCallback OnWillSpeak;

        [MonoPInvokeCallback(typeof(SpeechUtteranceCallback))]
        private static void OnSpeechCancelledCallback()
        {
            if (OnSpeechCancelled != null)
                OnSpeechCancelled();
        }

        [MonoPInvokeCallback(typeof(SpeechUtteranceCallback))]
        private static void OnSpeechContinuedCallback()
        {
            if (OnSpeechContinued != null)
                OnSpeechContinued();
        }

        [MonoPInvokeCallback(typeof(SpeechUtteranceCallback))]
        private static void OnSpeechFinishedCallback()
        {
            if (OnSpeechFinished != null)
                OnSpeechFinished();
        }

        [MonoPInvokeCallback(typeof(SpeechUtteranceCallback))]
        private static void OnSpeechPausedCallback()
        {
            if (OnSpeechPaused != null)
                OnSpeechPaused();
        }

        [MonoPInvokeCallback(typeof(SpeechUtteranceCallback))]
        private static void OnSpeechStartedCallback()
        {
            if (OnSpeechStarted != null)
                OnSpeechStarted();
        }

        [MonoPInvokeCallback(typeof(SpeechUtteranceCallback))]
        private static void OnWillSpeakCallback(int startIndex, int stringLength, string utteranceSpeechString)
        {
            if (OnWillSpeak != null)
                OnWillSpeak(startIndex, stringLength, utteranceSpeechString);
        }

        #endregion

        #region API

        /// <summary>
        /// Enqueues an utterance to be spoken using DefaultParameters
        /// </summary>
        /// <param name="speechString">The text to be spoken in the utterance.</param>
        public static void Speak(string speechString)
        {
            Speak(speechString, DefaultParameters);
        }


        /// <summary>
        /// Enqueues an utterance to be spoken using a voice object for the specified language and locale.
        /// </summary>
        /// <param name="speechString">The text to be spoken in the utterance.</param>
        /// <param name="language">A BCP 47 code specifying language and locale for a voice.</param>
        public static void Speak(string speechString, string language)
        {
            var voice = GetVoiceForLanguage(language);
            Speak(speechString, voice);
        }

        /// <summary>
        /// Enqueues an utterance to be spoken using a specific voice object
        /// </summary>
        /// <param name="speechString">The text to be spoken in the utterance.</param>
        /// <param name="voice">The voice used to speak the utterance.</param>
        public static void Speak(string speechString, ISpeechSynthesisVoice voice)
        {
            var speechUtteranceSettings = new SpeechUtteranceParameters
            {
                PitchMultiplier = DefaultParameters.PitchMultiplier,
                PreUtteranceDelay = DefaultParameters.PreUtteranceDelay,
                PostUtteranceDelay = DefaultParameters.PostUtteranceDelay,
                SpeechRate = DefaultParameters.SpeechRate,
                Voice = voice,
                Volume = DefaultParameters.Volume
            };
            Speak(speechString, speechUtteranceSettings);
        }

        /// <summary>
        /// Enqueues an utterance to be spoken with default voice using specific parameters
        /// </summary>
        /// <param name="speechString">The text to be spoken in the utterance.</param>
        /// <param name="speechUtteranceParameters">Parameters that affect the speech</param>
        public static void Speak(string speechString, SpeechUtteranceParameters speechUtteranceParameters)
        {
            var speechUtterance = new SpeechUtterance(speechString, speechUtteranceParameters);
            Speak(speechUtterance);
        }

        /// <summary>
        /// Enqueues an utterance to be spoken with using specific parameters
        /// </summary>
        /// <param name="speechUtterance">A chunk of text to be spoken, along with parameters that affect its speech.</param>
        public static void Speak(SpeechUtterance speechUtterance)
        {
            var voiceIdentifier = speechUtterance.Voice == null ? string.Empty : speechUtterance.Voice.Identifier;
            TTS_iOS.Speak(speechUtterance.SpeechString, speechUtterance.PitchMultiplier,
                speechUtterance.PreUtteranceDelay, speechUtterance.PostUtteranceDelay, speechUtterance.SpeechRate,
                voiceIdentifier, speechUtterance.Volume);
        }

        /// <summary>
        /// Continues speech from the point at which it left off.
        /// </summary>
        /// <returns>true if speech has continued, or false otherwise.</returns>
        public static bool Continue()
        {
            return TTS_iOS.ContinueSpeaking();
        }

        /// <summary>
        ///  Pauses speech at default boundary constraints.
        /// </summary>
        /// <returns>true if speech has paused, or false otherwise.</returns>
        public static bool Pause()
        {
            return TTS_iOS.PauseSpeaking(DefaultSpeechBoundaryForPause);
        }

        /// <summary>
        /// Pauses speech at the specified boundary constraint.
        /// </summary>
        /// <param name="speechBoundary">A constant describing whether speech should pause immediately or only after finishing the word currently being spoken.</param>
        /// <returns>true if speech has paused, or false otherwise.</returns>
        public static bool Pause(SpeechBoundary speechBoundary)
        {
            return TTS_iOS.PauseSpeaking(speechBoundary);
        }

        /// <summary>
        ///  Stops all speech at default boundary constraints.
        /// </summary>
        /// <returns>true if speech has stopped, or false otherwise.</returns>
        public static bool Stop()
        {
            return TTS_iOS.StopSpeaking(DefaultSpeechBoundaryForStop);
        }

        /// <summary>
        /// Stops all speech at the specified boundary constraint.
        /// </summary>
        /// <param name="speechBoundary">A constant describing whether speech should stop immediately or only after finishing the word currently being spoken.</param>
        /// <returns>true if speech has stopped, or false otherwise.</returns>
        public static bool Stop(SpeechBoundary speechBoundary)
        {
            return TTS_iOS.StopSpeaking(speechBoundary);
        }

        /// <summary>
        /// Returns all available voices.
        /// </summary>
        public static ReadOnlyCollection<ISpeechSynthesisVoice> AllAvailableVoices
        {
            get { return Instance._allAvailableVoices; }
        }

        /// <summary>
        /// Returns a voice object for the specified language and locale.
        /// </summary>
        /// <param name="language">A BCP 47 code specifying language and locale for a voice.</param>
        /// <returns>Returns null if no voice available for the specified language</returns>
        public static ISpeechSynthesisVoice GetVoiceForLanguage(string language)
        {
            var voiceId = TTS_iOS.GetVoiceIdentifierFromLanguageCode(language);
            return AllAvailableVoices.FirstOrDefault(voice => voice.Identifier == voiceId);
        }

        /// <summary>
        /// Returns all available voice objects for the specified language and locale.
        /// </summary>
        /// <param name="language">A BCP 47 code specifying language and locale for a voice.</param>
        /// <returns>Returns an empty array if no voice available for the specified language</returns>
        public static ISpeechSynthesisVoice[] GetAllVoicesForLanguage(string language)
        {
            return AllAvailableVoices.Where(voice => voice.Language == language).ToArray();
        }

        /// <summary>
        /// Returns the code for the user’s current locale.
        /// A string containing BCP 47 language and locale code for the user’s current locale.
        /// </summary>
        public static string CurrentLanguageCode
        {
            get { return TTS_iOS.CurrentLanguageCode; }
        }

        /// <summary>
        /// Returns all language codes (A BCP 47) for which voices are available.
        /// </summary>
        /// <returns>Returns all language codes (A BCP 47) for which voices are available</returns>
        public static string[] GetAllAvailableLanguages()
        {
            return AllAvailableVoices.Select(voice => voice.Language).Distinct().OrderBy(language => language).ToArray();
        }

        /// <summary>
        /// Parameters used when calling Speak without custom SpeechUtteranceParameters
        /// </summary>
        public static SpeechUtteranceParameters DefaultParameters
        {
            get { return _instance._defaultParameters; }
        }

        /// Constraints describing when speech may be paused
        /// Immediate - Indicates that speech should pause or stop immediately.
        /// Word - Indicates that speech should pause or stop after the word currently being spoken.
        public static SpeechBoundary DefaultSpeechBoundaryForPause = SpeechBoundary.Immediate;

        /// Constraints describing when speech may be stopped.
        /// Immediate - Indicates that speech should pause or stop immediately.
        /// Word - Indicates that speech should pause or stop after the word currently being spoken.
        public static SpeechBoundary DefaultSpeechBoundaryForStop = SpeechBoundary.Immediate;

        /// <summary>
        /// A Boolean value that indicates whether the synthesizer is speaking.
        /// Returns true if the synthesizer is speaking or has utterances enqueued to speak, even if it is currently paused. Returns false if the synthesizer has finished speaking all utterances in its queue or if it has not yet been given an utterance to speak.
        /// </summary>
        public static bool IsSpeaking
        {
            get { return TTS_iOS.IsSpeaking; }
        }

        /// <summary>
        /// A Boolean value that indicates whether speech has been paused.
        /// Returns true if the synthesizer has begun speaking an utterance and was paused using Pause(); false otherwise.
        /// </summary>
        public static bool IsPaused
        {
            get { return TTS_iOS.IsPaused; }
        }

        /// <summary>
        /// The minimum allowed speech rate.
        /// </summary>
        public static float UtteranceMinimumSpeechRate
        {
            get { return TTS_iOS.UtteranceMinimumSpeechRate; }
        }

        /// <summary>
        /// The maximum allowed speech rate.
        /// </summary>
        public static float UtteranceMaximumSpeechRate
        {
            get { return TTS_iOS.UtteranceMaximumSpeechRate; }
        }

        /// <summary>
        /// The default rate at which an utterance is spoken unless its rate property is changed.
        /// </summary>
        public static float UtteranceDefaultSpeechRate
        {
            get { return TTS_iOS.UtteranceDefaultSpeechRate; }
        }

        #endregion
    }
}
