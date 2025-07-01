namespace GoodEnough.TextToSpeech
{
    /// <summary>
    /// void Method with no parameters
    /// </summary>
    public delegate void SpeechUtteranceCallback();

    /// <summary>
    /// void Method with 3 parameters: int startIndex, int stringLength, string utteranceSpeechString
    /// </summary> 
    /// <param name="startIndex">The start index of the spoken part of the utterance string.</param>
    /// <param name="stringLength">The number of characters in the spoken part of the utterance string.</param>
    /// <param name="utteranceSpeechString">The utterance currently being spoken.</param>
    public delegate void StringSpeechUtteranceCallback(int startIndex, int stringLength, string utteranceSpeechString);
}