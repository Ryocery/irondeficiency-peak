using System;
using UnityEngine;

namespace IronDeficiency.Handlers;

public static class AudioHandler {
    public static AudioClip? ToAudioClip(byte[] fileBytes, string name = "wav") {
        try {
            int frequency = BitConverter.ToInt32(fileBytes, 24);
            int channels = BitConverter.ToInt16(fileBytes, 22);
            int bitsPerSample = BitConverter.ToInt16(fileBytes, 34);

            const int headerSize = 44;
            int bytesPerSample = bitsPerSample / 8;
            int totalSamples = (fileBytes.Length - headerSize) / bytesPerSample;
            int samplesPerChannel = totalSamples / channels;

            float[] audioData = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++) {
                short sample = (short)((fileBytes[headerSize + i * 2 + 1] << 8) | fileBytes[headerSize + i * 2]);
                audioData[i] = sample / 32768f;
            }

            AudioClip? clip = AudioClip.Create(name, samplesPerChannel, channels, frequency, false);
            clip.SetData(audioData, 0);
            return clip;
        } catch (Exception ex) {
            Plugin.Logger.LogError($"Failed to convert WAV data: {ex}");
            return null;
        }
    }
}