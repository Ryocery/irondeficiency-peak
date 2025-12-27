using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;

namespace IronDeficiency;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin {
    internal static ManualLogSource Log { get; private set; } = null!;
    
    // Tripping
    internal static ConfigEntry<bool> EnableRandomTrips { get; private set; } = null!;
    internal static ConfigEntry<float> TripChancePerSecond { get; private set; } = null!;
    internal static ConfigEntry<float> MinMovementSpeed { get; private set; } = null!;
    
    // Fainting
    internal static ConfigEntry<bool> EnableRandomFaints { get; private set; } = null!;
    internal static ConfigEntry<float> FaintChancePerSecond { get; private set; } = null!;
    internal static ConfigEntry<bool> CanFaintWhileClimbing { get; private set; } = null!;
    
    private void Awake() {
        Log = Logger;
        Log.LogInfo($"Plugin {Name} is loaded!");
        
        EnableRandomTrips = Config.Bind("Tripping", "EnableRandomTrips", true, "Enable random tripping.");
        TripChancePerSecond = Config.Bind("Tripping", "TripChancePerSecond", 0.02f, "Chance per second to trip (0.01 = 1% per second)");
        MinMovementSpeed = Config.Bind("Tripping", "MinMovementSpeed", 1.5f, "Minimum movement speed to be able to trip.");
        EnableRandomFaints = Config.Bind("Fainting", "EnableRandomFaints", true, "Enable random fainting.");
        FaintChancePerSecond = Config.Bind("Fainting", "FaintChancePerSecond", 0.006f, "Chance per second to faint (0.005 = 0.5% per second)");
        CanFaintWhileClimbing = Config.Bind("Fainting", "CanFaintWhileClimbing", true, "Whether you can faint while climbing.");
        
        Harmony harmony = new("com.github.RandomTripMod");
        
        try {
            harmony.PatchAll();
        } catch (Exception ex) {
            Log.LogError($"Mod failed to load: {ex}");
        }
    }
}

public class IronDeficiencyRPCHandler : MonoBehaviourPun {
    private Character? _character;

    private void Start() {
        _character = GetComponent<Character>();
    }
    
    [PunRPC]
    public void PlayTripSound() {
        AudioClip? sound = IronDeficiencyPatch.GetTripSound();
        if (!sound) return;
        if (_character) AudioSource.PlayClipAtPoint(sound, _character.Center, 0.7f);
    }
    
    [PunRPC]
    public void PlayFaintSound() {
        AudioClip? sound = IronDeficiencyPatch.GetFaintSound();
        
        if (!sound) return;
        if (_character) AudioSource.PlayClipAtPoint(sound, _character.Center, 0.7f);
    }
}

[HarmonyPatch(typeof(Character))]
public class IronDeficiencyPatch {
    private static AudioClip? _faintSound;
    private static AudioClip? _tripSound;
    
    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    public static void AwakePostfix(Character __instance) {
        __instance.gameObject.AddComponent<IronDeficiencyRPCHandler>();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    public static void UpdatePostfix(Character __instance) {
        if (!__instance.IsLocal) return;
        
        if (Plugin.EnableRandomTrips.Value) {
            ProcessRandomTrips(__instance);
        }
        
        if (Plugin.EnableRandomFaints.Value) {
            ProcessRandomFaints(__instance);
        }
    }
    
    private static void ProcessRandomTrips(Character character) {
        if (!character.data.isGrounded ||
            character.data.avarageVelocity.magnitude < Plugin.MinMovementSpeed.Value ||
            character.data.dead || character.data.fullyPassedOut || character.data.isClimbing || 
            character.data.isRopeClimbing || character.data.isVineClimbing) return;
        
        float chanceThisFrame = Plugin.TripChancePerSecond.Value * Time.deltaTime;
        if (UnityEngine.Random.Range(0f, 1f) < chanceThisFrame) {
            TriggerTrip(character);
        }
    }
    
    private static void ProcessRandomFaints(Character character) {
        if (character.data.dead || character.data.fullyPassedOut || character.data.passedOut) return;
        if (!Plugin.CanFaintWhileClimbing.Value && 
            (character.data.isClimbing || character.data.isRopeClimbing || character.data.isVineClimbing)) return;
        
        float chanceThisFrame = Plugin.FaintChancePerSecond.Value * Time.deltaTime;
        if (UnityEngine.Random.Range(0f, 1f) < chanceThisFrame) {
            TriggerFaint(character);
        }
    }
    
    private static void TriggerTrip(Character character) {
        try {
            Plugin.Log.LogInfo($"Random trip triggered for {character.characterName}!");
            
            IronDeficiencyRPCHandler? rpcHandler = character.GetComponent<IronDeficiencyRPCHandler>();
            if (rpcHandler) {
                rpcHandler.photonView.RPC("PlayTripSound", RpcTarget.All);
            } else {
                AudioClip? sound = GetTripSound();
                if (sound) {
                    AudioSource.PlayClipAtPoint(sound, character.Center, 0.7f);
                }
            }
            
            MethodInfo? getBodypartRigMethod = typeof(Character).GetMethod("GetBodypartRig", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo? fallMethod = typeof(Character).GetMethod("Fall", BindingFlags.NonPublic | BindingFlags.Instance);

            if (getBodypartRigMethod == null || fallMethod == null) return;
            
            Rigidbody? footR = getBodypartRigMethod.Invoke(character, [BodypartType.Foot_R]) as Rigidbody;
            Rigidbody? footL = getBodypartRigMethod.Invoke(character, [BodypartType.Foot_L]) as Rigidbody;
            Rigidbody? hip = getBodypartRigMethod.Invoke(character, [BodypartType.Hip]) as Rigidbody;
            Rigidbody? head = getBodypartRigMethod.Invoke(character, [BodypartType.Head]) as Rigidbody;
            
            fallMethod.Invoke(character, [2f, 0f]);
            
            Vector3 forward = character.data.lookDirection_Flat;
            footR?.AddForce((forward + Vector3.up) * 200f, ForceMode.Impulse);
            footL?.AddForce((forward + Vector3.up) * 200f, ForceMode.Impulse);
            hip?.AddForce(Vector3.up * 1500f, ForceMode.Impulse);
            head?.AddForce(forward * -300f, ForceMode.Impulse);

        } catch (Exception ex) {
            Plugin.Log.LogError($"Exception in random trip: {ex}");
        }
    }
    
    private static void TriggerFaint(Character character) {
        try {
            Plugin.Log.LogInfo($"Random faint triggered for {character.characterName}!");
            
            IronDeficiencyRPCHandler? rpcHandler = character.GetComponent<IronDeficiencyRPCHandler>();
            if (rpcHandler) {
                rpcHandler.photonView.RPC("PlayFaintSound", RpcTarget.All);
            } else {
                AudioClip? sound = GetFaintSound();
                if (sound) {
                    AudioSource.PlayClipAtPoint(sound, character.Center, 0.7f);
                }
            }
            
            character.PassOutInstantly();
        } catch (Exception ex) {
            Plugin.Log.LogError($"Exception in random faint: {ex}");
        }
    }
    
    public static AudioClip? GetFaintSound() {
        if (_faintSound) return _faintSound;
        string? modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        if (modPath == null) return _faintSound;
        string audioPath = Path.Combine(modPath, "sounds", "faint_sound.wav");
        
        if (!File.Exists(audioPath)) return _faintSound;
        
        try {
            byte[] audioData = File.ReadAllBytes(audioPath);
            _faintSound = WavUtility.ToAudioClip(audioData, "faint_sound");
            if (_faintSound != null) {
                return _faintSound;
            }
        } catch (Exception ex) {
            Plugin.Log.LogError($"Failed to load custom sound: {ex}");
        }
        return _faintSound;
    }
    
    public static AudioClip? GetTripSound() {
        if (_tripSound) return _tripSound;
        
        AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        _tripSound = allClips.FirstOrDefault(clip => clip.name == "Au_Slip1");
        return _tripSound;
    }
}

public static class WavUtility {
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
            Plugin.Log.LogError($"Failed to convert WAV data: {ex}");
            return null;
        }
    }
}