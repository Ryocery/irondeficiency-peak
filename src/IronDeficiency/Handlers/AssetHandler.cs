using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IronDeficiency.Handlers;

public static class AssetHandler {
    private static AudioClip? _faintSound;
    private static AudioClip? _tripSound;
    private static string _pluginDir = "";

    public static void Init(string pluginDir) {
        _pluginDir = pluginDir;
    }

    public static AudioClip? GetFaintSound() {
        if (_faintSound) return _faintSound;

        if (string.IsNullOrEmpty(_pluginDir)) return null;
        string audioPath = Path.Combine(_pluginDir, "sounds", "faint_sound.wav");

        if (!File.Exists(audioPath)) return null;

        try {
            byte[] audioData = File.ReadAllBytes(audioPath);
            _faintSound = AudioHandler.ToAudioClip(audioData, "faint_sound");
            return _faintSound;
        } catch (Exception ex) {
            Plugin.Logger.LogError($"Failed to load custom sound: {ex}");
        }
        return null;
    }

    public static AudioClip? GetTripSound() {
        if (_tripSound) return _tripSound;

        AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        _tripSound = allClips.FirstOrDefault(clip => clip.name == "Au_Slip1");
        return _tripSound;
    }
}