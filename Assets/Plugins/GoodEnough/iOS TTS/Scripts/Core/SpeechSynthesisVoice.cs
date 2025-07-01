namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// The voice object used to speak the utterance.
    /// </summary>
    internal class SpeechSynthesisVoice : ISpeechSynthesisVoice
    {
        private readonly string _identifier;
        private readonly string _name;
        private readonly string _language;
        private readonly VoiceQuality _quality;

        public SpeechSynthesisVoice(string identifier, string name, string language, VoiceQuality quality)
        {
            _identifier = identifier;
            _name = name;
            _language = language;
            _quality = quality;
        }

        /// <summary>
        /// The unique identifier for a voice object.
        /// </summary>
        public string Identifier
        {
            get { return _identifier; }
        }
        /// <summary>
        /// The name for a voice object.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
        /// <summary>
        /// A BCP 47 code identifying the voice’s language and locale.
        /// The locale of a voice reflects regional variations in pronunciation or accent; for example, a voice with code en-US speaks English text with a North American accent, and a voice with code en-AU speaks English text with an Australian accent.
        /// </summary>
        public string Language
        {
            get { return _language; }
        }
        /// <summary>
        /// The speech quality for a voice object.
        /// Default - he lower quality version of a voice that is usually installed on the device by default.
        /// Enhanced - The higher quality version of a voice that is usually downloaded by the user.
        /// </summary>
        public VoiceQuality Quality
        {
            get { return _quality; }
        }
    }
}