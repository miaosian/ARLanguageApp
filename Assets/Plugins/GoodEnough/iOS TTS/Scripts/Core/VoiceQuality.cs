namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// The speech quality for a voice object.
    /// Default - he lower quality version of a voice that is usually installed on the device by default.
    /// Enhanced - The higher quality version of a voice that is usually downloaded by the user.
    /// </summary>
    public enum VoiceQuality
    {
        Null = 0,
        /// <summary>
        /// The lower quality version of a voice that is usually installed on the device by default.
        /// </summary>
        Default = 1,
        /// <summary>
        /// The higher quality version of a voice that is usually downloaded by the user.
        /// </summary>
        Enhanced = 2
    }
}