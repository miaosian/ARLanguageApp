namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// Parameters that affect the speech.
    /// </summary>
    public class SpeechUtteranceParameters
    {
        /// <summary>
        /// The baseline pitch at which the utterance will be spoken.
        /// The default pitch is 1.0. Allowed values are in the range from 0.5 (for lower pitch) to 2.0 (for higher pitch).
        /// </summary>
        public float PitchMultiplier = 1f;

        /// <summary>
        /// The amount of time in seconds a speech synthesizer will wait before actually speaking the utterance upon beginning to handle it.
        /// When two or more utterances are spoken, the time between periods when either is audible will be at least the sum of the first utterance’s postUtteranceDelay and the second utterance’s preUtteranceDelay.
        /// </summary>
        public float PreUtteranceDelay = 0f;

        /// <summary>
        /// The amount of time in seconds a speech synthesizer will wait after the utterance is spoken before handling the next queued utterance.
        /// When two or more utterances are spoken, the time between periods when either is audible will be at least the sum of the first utterance’s postUtteranceDelay and the second utterance’s preUtteranceDelay.
        /// </summary>
        public float PostUtteranceDelay = 0f;

        /// <summary>
        /// The rate at which the utterance will be spoken.
        /// Speech rates are values in the range between UtteranceMinimumSpeechRate and UtteranceMaximumSpeechRate. Lower values correspond to slower speech, and vice versa. The default value is UtteranceDefaultSpeechRate.
        /// </summary>
        public float SpeechRate;

        /// <summary>
        /// The default value is null, which causes the utterance to be spoken in the default voice.
        /// </summary>
        public ISpeechSynthesisVoice Voice = null;

        /// <summary>
        /// The volume used when speaking the utterance.
        /// Allowed values are in the range from 0.0 (silent) to 1.0 (loudest). The default volume is 1.0.
        /// </summary>
        public float Volume = 1f;

        /// <summary>
        /// Parameters that affect the speech
        /// </summary>
        public SpeechUtteranceParameters()
        {
            SpeechRate = TTS.UtteranceDefaultSpeechRate;
        }
    }
}