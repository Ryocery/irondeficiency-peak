using Photon.Pun;
using UnityEngine;

namespace IronDeficiency.Handlers;

public class NetworkHandler : MonoBehaviourPun {
    private Character? _character;

    private void Start() {
        _character = GetComponent<Character>();
    }

    [PunRPC]
    public void PlayTripSound() {
        AudioClip? sound = AssetHandler.GetTripSound();
        if (!sound) return;
        if (_character) AudioSource.PlayClipAtPoint(sound, _character.Center, 0.7f);
    }

    [PunRPC]
    public void PlayFaintSound() {
        AudioClip? sound = AssetHandler.GetFaintSound();
        if (!sound) return;
        if (_character) AudioSource.PlayClipAtPoint(sound, _character.Center, 0.7f);
    }
}