namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// The voice used to speak the utterance.
    /// </summary>
    public interface ISpeechSynthesisVoice
    {
        /// <summary>
        /// The unique identifier for a voice object.
        /// </summary>
        string Identifier { get; }
        /// <summary>
        /// The name for a voice object.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// A BCP 47 code identifying the voice’s language and locale.
        /// The locale of a voice reflects regional variations in pronunciation or accent; for example, a voice with code en-US speaks English text with a North American accent, and a voice with code en-AU speaks English text with an Australian accent.
        /// </summary>
        string Language { get; }
        /// <summary>
        /// The speech quality for a voice object.
        /// Default - he lower quality version of a voice that is usually installed on the device by default.
        /// Enhanced - The higher quality version of a voice that is usually downloaded by the user.
        /// </summary>
        VoiceQuality Quality { get; }
    }
}