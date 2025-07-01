using UnityEngine;
using System.IO;
using System.Text;

public static class WaveEncoder
{
  public static byte[] FromAudioClip(AudioClip clip)
  {
    var samples = new float[clip.samples * clip.channels];
    clip.GetData(samples, 0);
    var pcm = new byte[samples.Length * 2];
    int idx = 0;
    foreach (var f in samples)
    {
      short s = (short)(Mathf.Clamp(f, -1f, 1f) * short.MaxValue);
      pcm[idx++] = (byte)(s & 0xff);
      pcm[idx++] = (byte)((s >> 8) & 0xff);
    }
    using var ms = new MemoryStream();
    using var w  = new BinaryWriter(ms);
    // RIFF header
    w.Write(Encoding.ASCII.GetBytes("RIFF"));
    w.Write(36 + pcm.Length);
    w.Write(Encoding.ASCII.GetBytes("WAVE"));
    // fmt chunk
    w.Write(Encoding.ASCII.GetBytes("fmt "));
    w.Write(16);
    w.Write((short)1);
    w.Write((short)clip.channels);
    w.Write(clip.frequency);
    w.Write(clip.frequency * clip.channels * 2);
    w.Write((short)(clip.channels * 2));
    w.Write((short)16);
    // data chunk
    w.Write(Encoding.ASCII.GetBytes("data"));
    w.Write(pcm.Length);
    w.Write(pcm);
    return ms.ToArray();
  }
}
