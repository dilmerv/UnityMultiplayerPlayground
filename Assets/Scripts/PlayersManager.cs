using DilmerGames.Core.Singletons;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayersManager : Singleton<PlayersManager>
{
    NetworkVariable<int> playersInGame = new NetworkVariable<int>();

    public int PlayersInGame
    {
        get
        {
            return playersInGame.Value;
        }
    }

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            playersInGame.Value++;
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            playersInGame.Value--;
        };
    }

    //public override void OnNetworkSpawn()
    //{
    //    Logger.Instance.LogInfo("OnNetworkSpawn...");
    //    var localPlayerGameObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
    //    var localPlayerOverlay = localPlayerGameObject.gameObject.GetComponentInChildren<TextMeshProUGUI>();
    //    localPlayerOverlay.text = $"ClientId: {NetworkManager.Singleton.LocalClientId}";
    //    localPlayerGameObject.transform.position = new Vector3(Random.Range(-4, 4), 0, Random.Range(-4, 4));
    //}
}
