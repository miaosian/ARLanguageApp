    namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// Constraints describing when speech may be paused or stopped.
    /// Immediate - Indicates that speech should pause or stop immediately.
    /// Word - Indicates that speech should pause or stop after the word currently being spoken.
    /// </summary>
    public enum SpeechBoundary
    {
        /// <summary>
        /// Indicates that speech should pause or stop immediately.
        /// </summary>
        Immediate = 0,

        /// <summary>
        /// Indicates that speech should pause or stop after the word currently being spoken.
        /// </summary>
        Word = 1
    }
}
