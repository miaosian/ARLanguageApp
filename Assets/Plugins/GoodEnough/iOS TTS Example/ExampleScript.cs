using System.Collections.Generic;
using System.Linq;
using GoodEnough.TextToSpeech;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.GoodEnough.Example
{
    public class ExampleScript : MonoBehaviour
    {
        public InputField InputField;
        public Text IsPausedText;
        public Text IsSpeakingText;

        public Slider PitchSlider;
        public Slider RateSlider;
        public Text CurrentPitch;
        public Text CurrentRate;
        public Slider VolumeSlider;
        public Text CurrentVolume;

        public Text VoiceTitle;
        public Text VoiceId;
        public Text VoiceName;
        public Text VoiceLanguage;
        public Text VoiceQuality;

        private SpeechBoundary _speechBoundary;
        private int _currentVoiceIndex;
        private string _lastSubmittedText;

        private List<ISpeechSynthesisVoice> _voices;

        private void Awake()
        {
            CacheAllVoices();
            SetVoiceToCurrentUserLanguageCode();
            UpdateVoiceDetails();
            TTS.OnWillSpeak = UpdateTextProgress;
            TTS.OnSpeechCancelled = ClearTextProgress;
            TTS.OnSpeechFinished = ClearTextProgress;
            PitchSlider.value = TTS.DefaultParameters.PitchMultiplier;
            UpdatePitch(PitchSlider.value);
            RateSlider.minValue = TTS.UtteranceMinimumSpeechRate;
            RateSlider.maxValue = TTS.UtteranceMaximumSpeechRate;
            RateSlider.value = TTS.DefaultParameters.SpeechRate;
            UpdateSpeechRate(RateSlider.value);
            VolumeSlider.value = TTS.DefaultParameters.Volume;
            UpdateVolume(VolumeSlider.value);
            _lastSubmittedText = InputField.text;
            InputField.onEndEdit.AddListener(text => _lastSubmittedText = text);
        }

        private void CacheAllVoices()
        {
            _voices = TTS.AllAvailableVoices.ToList();
            _voices.Add(null);
        }

        private void SetVoiceToCurrentUserLanguageCode()
        {
            var languageCode = TTS.CurrentLanguageCode;
            var allVoices = _voices;
            var voice = TTS.GetVoiceForLanguage(languageCode);
            if (voice != null)
                _currentVoiceIndex = allVoices.IndexOf(voice);
        }

        //Called from Slider
        public void UpdatePitch(float value)
        {
            TTS.DefaultParameters.PitchMultiplier = value;
            CurrentPitch.text = string.Format("{0:0.0}", value);
        }

        //Called from Slider
        public void UpdateSpeechRate(float value)
        {
            TTS.DefaultParameters.SpeechRate = value;
            CurrentRate.text = string.Format("{0:0.0}", RateSlider.minValue + value * (RateSlider.maxValue - RateSlider.minValue));
        }

        //Called from Slider
        public void UpdateVolume(float value)
        {
            TTS.DefaultParameters.Volume = value;
            CurrentVolume.text = string.Format("{0:0.0}", value);
        }

        private void UpdateVoiceDetails()
        {
            var voice = _voices[_currentVoiceIndex];
            TTS.DefaultParameters.Voice = voice;

            if (voice == null)
            {
                VoiceTitle.text = string.Format("Voice ({0}/{1})", _currentVoiceIndex + 1,
                    _voices.Count);
                VoiceId.text = "Id: Default";
                VoiceName.text = "Default";
                VoiceLanguage.text = "Language: Default";
                VoiceQuality.text = "Quality: Default";
                return;
            }

            VoiceTitle.text = string.Format("Voice ({0}/{1})", _currentVoiceIndex + 1,
                _voices.Count);
            VoiceId.text = string.Format("Id: {0}", voice.Identifier);
            VoiceName.text = string.Format("{0}", voice.Name);
            VoiceLanguage.text = string.Format("Language: {0}", voice.Language);
            VoiceQuality.text = string.Format("Quality: {0}", voice.Quality);
        }

        private void Update()
        {
            IsSpeakingText.text = string.Format("Is Speaking: {0}", TTS.IsSpeaking);
            IsPausedText.text = string.Format("Is Paused: {0}", TTS.IsPaused);
            InputField.interactable = !TTS.IsSpeaking;

        }

        private void UpdateTextProgress(int startIndex, int stringLength, string utteranceSpeechString)
        {
            const string boldTag = "<b>";
            var newString = utteranceSpeechString.Insert(startIndex, boldTag);
            newString = newString.Insert(startIndex + stringLength + boldTag.Length, "</b>");
            InputField.text = newString;
        }

        private void ClearTextProgress()
        {
            InputField.text = _lastSubmittedText;
        }

        //Called from Button
        public void Speak()
        {
            TTS.Speak(_lastSubmittedText);
        }

        //Called from Button
        public void Continue()
        {
            TTS.Continue();
        }

        //Called from Button
        public void Pause()
        {
            TTS.Pause(_speechBoundary);
        }

        //Called from Button
        public void Stop()
        {
            TTS.Stop(_speechBoundary);
        }

        //Called from Button
        public void SetSpeechBoundary(int speechBoundary)
        {
            _speechBoundary = (SpeechBoundary)speechBoundary;
        }

        //Called from Button
        public void NextVoice()
        {
            _currentVoiceIndex = (_currentVoiceIndex + 1) % _voices.Count;
            UpdateVoiceDetails();
        }

        //Called from Button
        public void PreviousVoice()
        {
            _currentVoiceIndex--;
            if (_currentVoiceIndex < 0)
                _currentVoiceIndex = _voices.Count - 1;
            UpdateVoiceDetails();
        }
    }
}
