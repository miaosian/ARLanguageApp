using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Utility to convert Unity AudioClip to WAV byte array (PCM 16-bit little endian).
/// </summary>
public static class WavUtility
{
    const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null)
            throw new ArgumentNullException("clip");

        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert float samples to 16-bit PCM
        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        float rescaleFactor = 32767; // to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            var byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        using (var memStream = new MemoryStream())
        {
            // RIFF header
            memStream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
            memStream.Write(BitConverter.GetBytes(memStream.Length + bytesData.Length - 8), 0, 4);
            memStream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
            
            // fmt subchunk
            memStream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
            memStream.Write(BitConverter.GetBytes(16), 0, 4); // Subchunk1Size (16 for PCM)
            memStream.Write(BitConverter.GetBytes((short)1), 0, 2); // AudioFormat PCM = 1
            memStream.Write(BitConverter.GetBytes((short)clip.channels), 0, 2);
            memStream.Write(BitConverter.GetBytes(clip.frequency), 0, 4);
            int byteRate = clip.frequency * clip.channels * 2;
            memStream.Write(BitConverter.GetBytes(byteRate), 0, 4);
            short blockAlign = (short)(clip.channels * 2);
            memStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);
            memStream.Write(BitConverter.GetBytes((short)16), 0, 2); // BitsPerSample

            // data subchunk
            memStream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
            memStream.Write(BitConverter.GetBytes(bytesData.Length), 0, 4);
            memStream.Write(bytesData, 0, bytesData.Length);

            return memStream.ToArray();
        }
    }

/// <summary>
/// Convert a WAV byte array (16-bit PCM, little-endian) into a Unity AudioClip.
/// </summary>
public static AudioClip ToAudioClip(byte[] wavFile, string clipName)
{
    const int HEADER_SIZE = 44;
    // channels (Int16 at offset 22)
    int channels = BitConverter.ToInt16(wavFile, 22);
    // sample rate (Int32 at offset 24)
    int sampleRate = BitConverter.ToInt32(wavFile, 24);
    // total data bytes (Int32 at offset 40)
    int dataSize = BitConverter.ToInt32(wavFile, 40);
    // 16-bit = 2 bytes per sample
    int totalSamples = dataSize / 2;
    // samples per channel
    int samplesPerChannel = totalSamples / channels;

    var data = new float[totalSamples];
    int offset = HEADER_SIZE;
    for (int i = 0; i < totalSamples; i++)
    {
        short sample = BitConverter.ToInt16(wavFile, offset);
        data[i] = sample / 32768f;
        offset += 2;
    }

    // Create the clip: lengthSamples is per-channel
    AudioClip clip = AudioClip.Create(clipName, samplesPerChannel, channels, sampleRate, false);
    clip.SetData(data, 0);
    return clip;
}

}
