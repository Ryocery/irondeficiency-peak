using System;
using System.Reflection;
using HarmonyLib;
using IronDeficiency.Handlers;
using UnityEngine;

namespace IronDeficiency.Patches;

[HarmonyPatch(typeof(Character))]
public static class CharacterPatches {
    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    public static void AwakePostfix(Character __instance) {
        __instance.gameObject.AddComponent<NetworkHandler>();
    }

    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    public static void UpdatePostfix(Character __instance) {
        if (!__instance.IsLocal) return;

        if (Config.Instance.EnableRandomTrips.Value) {
            ProcessRandomTrips(__instance);
        }

        if (Config.Instance.EnableRandomFaints.Value) {
            ProcessRandomFaints(__instance);
        }
    }

    private static void ProcessRandomTrips(Character character) {
        if (!character.data.isGrounded ||
            character.data.avarageVelocity.magnitude < Config.Instance.MinMovementSpeed.Value ||
            character.data.dead ||
            character.data.fullyPassedOut ||
            character.data.isClimbing ||
            character.data.isRopeClimbing ||
            character.data.isVineClimbing) return;

        float chanceThisFrame = Config.Instance.TripChancePerSecond.Value * Time.deltaTime;
        if (UnityEngine.Random.Range(0f, 1f) < chanceThisFrame) {
            TriggerTrip(character);
        }
    }

    private static void ProcessRandomFaints(Character character) {
        if (character.data.dead || character.data.fullyPassedOut || character.data.passedOut) return;

        if (!Config.Instance.CanFaintWhileClimbing.Value &&
            (character.data.isClimbing || character.data.isRopeClimbing || character.data.isVineClimbing)) return;

        float chanceThisFrame = Config.Instance.FaintChancePerSecond.Value * Time.deltaTime;
        if (UnityEngine.Random.Range(0f, 1f) < chanceThisFrame) {
            TriggerFaint(character);
        }
    }

    private static void TriggerTrip(Character character) {
        try {
            Plugin.Logger.LogInfo($"Random trip triggered for {character.characterName}!");

            NetworkHandler rpcHandler = character.GetComponent<NetworkHandler>();
            if (rpcHandler) {
                rpcHandler.photonView.RPC("PlayTripSound", Photon.Pun.RpcTarget.All);
            } else {
                AudioClip? sound = AssetHandler.GetTripSound();
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
            Plugin.Logger.LogError($"Exception in random trip: {ex}");
        }
    }

    private static void TriggerFaint(Character character) {
        try {
            Plugin.Logger.LogInfo($"Random faint triggered for {character.characterName}!");

            NetworkHandler rpcHandler = character.GetComponent<NetworkHandler>();
            if (rpcHandler) {
                rpcHandler.photonView.RPC("PlayFaintSound", Photon.Pun.RpcTarget.All);
            } else {
                AudioClip? sound = AssetHandler.GetFaintSound();
                if (sound) {
                    AudioSource.PlayClipAtPoint(sound, character.Center, 0.7f);
                }
            }

            character.PassOutInstantly();
        } catch (Exception ex) {
            Plugin.Logger.LogError($"Exception in random faint: {ex}");
        }
    }
}