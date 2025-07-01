using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GoodEnough.TextToSpeech
{
    internal static class TTS_iOS
    {
        #region Import DLL

        #if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void _SetupCallbacks(SpeechUtteranceCallback speechUtteranceCancelled,
            SpeechUtteranceCallback speechUtteranceContinued, SpeechUtteranceCallback speechUtteranceFinished,
            SpeechUtteranceCallback speechUtterancePaused, SpeechUtteranceCallback speechUtteranceStarted,
            StringSpeechUtteranceCallback willSpeakPartOfString);

        [DllImport("__Internal")]
        private static extern void _Speak(string textToSpeak, float pitch, double preUtteranceDelay,
            double postUtteranceDelay, float speechRate, string voiceIdentifier, float volume);

        [DllImport("__Internal")]
        private static extern bool _PauseSpeakingEndOfWord();

        [DllImport("__Internal")]
        private static extern bool _PauseSpeakingImmediate();

        [DllImport("__Internal")]
        private static extern bool _ContinueSpeaking();

        [DllImport("__Internal")]
        private static extern bool _StopSpeakingEndOfWord();

        [DllImport("__Internal")]
        private static extern bool _StopSpeakingImmediate();

        [DllImport("__Internal")]
        private static extern bool _isPaused();

        [DllImport("__Internal")]
        private static extern bool _isSpeaking();

        [DllImport("__Internal")]
        private static extern float _UtteranceMinimumSpeechRate();

        [DllImport("__Internal")]
        private static extern float _UtteranceMaximumSpeechRate();

        [DllImport("__Internal")]
        private static extern float _UtteranceDefaultSpeechRate();

        [DllImport("__Internal")]
        private static extern long _GetNumberOfAvailableVoices();

        [DllImport("__Internal")]
        private static extern string _GetVoiceIdentifier(int index);

        [DllImport("__Internal")]
        private static extern string _GetVoiceIdentifierFromLanguageCode(string languageCode);

        [DllImport("__Internal")]
        private static extern string _GetVoiceName(int index);

        [DllImport("__Internal")]
        private static extern string _GetVoiceLanguage(int index);

        [DllImport("__Internal")]
        private static extern int _GetVoiceQuality(int index);

        [DllImport("__Internal")]
        private static extern string _CurrentLanguageCode();
        #endif

        #endregion

        #region Calls to Native

        public static void SetupCallbacks(SpeechUtteranceCallback speechUtteranceCancelled,
            SpeechUtteranceCallback speechUtteranceContinued, SpeechUtteranceCallback speechUtteranceFinished,
            SpeechUtteranceCallback speechUtterancePaused, SpeechUtteranceCallback speechUtteranceStarted,
            StringSpeechUtteranceCallback willSpeakPartOfString)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                Try(()=> _SetupCallbacks(speechUtteranceCancelled, speechUtteranceContinued, speechUtteranceFinished,
                    speechUtterancePaused, speechUtteranceStarted, willSpeakPartOfString),
                    "Error setting up callbacks");
#endif
        }

        public static void Speak(string textToSpeak, float pitchMultiplier = 1f, double preUtteranceDelay = 0d,
            double postUtteranceDelay = 0d, float speechRate = 1f, string voiceIdentifier = null, float volume = 1f)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
            {
                var rate = Mathf.Clamp(speechRate, UtteranceMinimumSpeechRate, UtteranceMaximumSpeechRate);
                var pitch = Mathf.Clamp(pitchMultiplier, 0.5f, 2f);
                var clampedVolume = Mathf.Clamp(volume, 0f, 1f);

                Try(() => _Speak(textToSpeak, pitch, preUtteranceDelay, postUtteranceDelay, rate, voiceIdentifier,
                    clampedVolume),
                    "Error synthesizing speech");
            }
#endif
        }

        public static bool PauseSpeaking(SpeechBoundary speechBoundary = SpeechBoundary.Word)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                switch (speechBoundary)
                {
                    case SpeechBoundary.Immediate:
                        return Try(_PauseSpeakingImmediate, false, "Error Pausing Speech Immediate");
                    case SpeechBoundary.Word:
                        return Try(_PauseSpeakingEndOfWord, false, "Error Pausing Speech End Of Word");
                    default:
                        throw new ArgumentOutOfRangeException("speechBoundary", speechBoundary, "SpeechBoundary not supported");
                }
#endif
            return false;
        }

        public static bool StopSpeaking(SpeechBoundary speechBoundary = SpeechBoundary.Immediate)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                switch (speechBoundary)
                {
                    case SpeechBoundary.Immediate:
                        Try(_StopSpeakingImmediate, false, "Error Stopping Speech Immediate");
                        break;
                    case SpeechBoundary.Word:
                        Try(_StopSpeakingEndOfWord, false, "Error Stopping Speech End Of Word");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("speechBoundary", speechBoundary, "SpeechBoundary not supported");
                }
#endif
            return false;
        }

        public static bool ContinueSpeaking()
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                return Try(_ContinueSpeaking, false, "Error Continuing Speech");
#endif
            return false;
        }

        public static bool IsPaused
        {
            get
            {
#if UNITY_IOS
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                    return Try(_isPaused, false, "Error getting IsPaused state");
#endif
                return false;
            }
        }

        public static bool IsSpeaking
        {
            get
            {
#if UNITY_IOS
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                    return Try(_isSpeaking, false, "Error getting IsSpeaking state");
#endif
                return false;
            }
        }

        /// <summary>
        /// The minimum allowed speech rate.
        /// </summary>
        public static float UtteranceMinimumSpeechRate
        {
            get
            {
#if UNITY_IOS
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                    return Try(_UtteranceMinimumSpeechRate, 0f, "Error getting UtteranceMinimumSpeechRate value");
#endif
                return 0f;
            }
        }

        /// <summary>
        /// The maximum allowed speech rate.
        /// </summary>
        public static float UtteranceMaximumSpeechRate
        {
            get
            {
#if UNITY_IOS
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                    return Try(_UtteranceMaximumSpeechRate, 0f, "Error getting UtteranceMaximumSpeechRate value");
#endif
                return 0f;
            }
        }

        /// <summary>
        /// The default rate at which an utterance is spoken unless its rate property is changed.
        /// </summary>
        public static float UtteranceDefaultSpeechRate
        {
            get
            {
#if UNITY_IOS
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                    return Try(_UtteranceDefaultSpeechRate, 0f, "Error getting UtteranceDefaultSpeechRate value");
#endif
                return 0f;
            }
        }

        public static long GetNumberOfAvailableVoices()
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
            {
                return Try(_GetNumberOfAvailableVoices, 0, "Error getting the number of available voices.");
            }
#endif
            return 0;
        }

        public static string GetVoiceIdentifier(int index)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                return Try(() => _GetVoiceIdentifier(index), string.Empty, "Error getting voice identifier for voice index " + index);
#endif
            return string.Empty;
        }

        public static string GetVoiceName(int index)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                return Try(() => _GetVoiceName(index), string.Empty, "Error getting voice name for voice index " + index);
#endif
            return string.Empty;
        }

        public static string GetVoiceLanguage(int index)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                return Try(() => _GetVoiceLanguage(index), string.Empty, "Error getting voice language for voice index " + index);
#endif
            return string.Empty;
        }

        public static int GetVoiceQuality(int index)
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                return Try(() => _GetVoiceQuality(index), 0, "Error getting voice quality for voice index " + index);
#endif
            return 0;
        }

        public static string GetVoiceIdentifierFromLanguageCode(string languageCode)
        { 
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                return Try(() => _GetVoiceIdentifierFromLanguageCode(languageCode), string.Empty, "Error getting voice identifier from Language Code " + languageCode);
#endif

            return string.Empty;
        }

        public static string CurrentLanguageCode
        {
            get
            {
#if UNITY_IOS
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
                    return Try(_CurrentLanguageCode, string.Empty, "Error getting current language code");
#endif
                return string.Empty;
            }
        }

        #endregion

        private static void Try(Action action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogError(errorMessage + " " + e.Message);
            }
        }

        private static T Try<T>(Func<T> action, T fallbackValue, string errorMessage)
        {
            T value;
            try
            {
                value = action();
            }
            catch (Exception e)
            {
                Debug.LogError(errorMessage + " " + e.Message);
                value = fallbackValue;
            }
            return value;
        }
    }
}