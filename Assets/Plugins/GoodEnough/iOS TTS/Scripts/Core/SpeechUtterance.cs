namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// A chunk of text to be spoken, along with parameters that affect its speech.
    /// </summary>
    public class SpeechUtterance
    {
        /// <summary>
        /// The text to be spoken in the utterance.
        /// An utterance’s text cannot be changed once it is created. To speak different text, create a new utterance.
        /// </summary>
        public readonly string SpeechString;
        private readonly SpeechUtteranceParameters _parameters;

        /// <summary>
        /// A chunk of text to be spoken, created with standard parameters
        /// </summary>
        /// <param name="speechString">The text to be spoken in the utterance.</param>
        public SpeechUtterance(string speechString)
        {
            SpeechString = speechString;
            _parameters = new SpeechUtteranceParameters();
        }

        /// <summary>
        /// A chunk of text to be spoken, along with parameters that affect its speech.
        /// </summary>
        /// <param name="speechString">The text to be spoken in the utterance.</param>
        /// <param name="parameters">Parameters that affect the speech</param>
        public SpeechUtterance(string speechString, SpeechUtteranceParameters parameters)
        {
            SpeechString = speechString;
            _parameters = parameters;
        }

        /// <summary>
        /// The baseline pitch at which the utterance will be spoken.
        /// The default pitch is 1.0. Allowed values are in the range from 0.5 (for lower pitch) to 2.0 (for higher pitch).
        /// </summary>
        public float PitchMultiplier
        {
            get { return _parameters.PitchMultiplier; }
            set { _parameters.PitchMultiplier = value; }
        }

        /// <summary>
        /// The amount of time in seconds a speech synthesizer will wait before actually speaking the utterance upon beginning to handle it.
        /// When two or more utterances are spoken, the time between periods when either is audible will be at least the sum of the first utterance’s postUtteranceDelay and the second utterance’s preUtteranceDelay.
        /// </summary>
        public float PreUtteranceDelay
        {
            get { return _parameters.PreUtteranceDelay; }
            set { _parameters.PreUtteranceDelay = value; }
        }

        /// <summary>
        /// The amount of time in seconds a speech synthesizer will wait after the utterance is spoken before handling the next queued utterance.
        /// When two or more utterances are spoken, the time between periods when either is audible will be at least the sum of the first utterance’s postUtteranceDelay and the second utterance’s preUtteranceDelay.
        /// </summary>
        public float PostUtteranceDelay
        {
            get { return _parameters.PostUtteranceDelay; }
            set { _parameters.PostUtteranceDelay = value; }
        }

        /// <summary>
        /// The rate at which the utterance will be spoken.
        /// Speech rates are values in the range between UtteranceMinimumSpeechRate and UtteranceMaximumSpeechRate. Lower values correspond to slower speech, and vice versa. The default value is UtteranceDefaultSpeechRate.
        /// </summary>
        public float SpeechRate
        {
            get { return _parameters.SpeechRate; }
            set { _parameters.SpeechRate = value; }
        }

        /// <summary>
        /// The voice used to speak the utterance.
        /// The default value is null, which causes the utterance to be spoken in the default voice.
        /// </summary>
        public ISpeechSynthesisVoice Voice
        {
            get { return _parameters.Voice; }
            set { _parameters.Voice = value; }
        }

        /// <summary>
        /// The volume used when speaking the utterance.
        /// Allowed values are in the range from 0.0 (silent) to 1.0 (loudest). The default volume is 1.0.
        /// </summary>
        public float Volume
        {
            get { return _parameters.Volume; }
            set { _parameters.Volume = value; }
        }
    }
}