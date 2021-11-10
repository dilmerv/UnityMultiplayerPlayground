using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField]
    private Vector2 defaultPositionRange = new Vector2(-4, 4);

    private bool playerInitialized;

    private void Update()
    {
        if(!playerInitialized)
        {
            var localPlayerGameObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            if (localPlayerGameObject != null)
            {
                var localPlayerOverlay = localPlayerGameObject.gameObject.GetComponentInChildren<TextMeshProUGUI>();
                localPlayerOverlay.text =$"ClientId: {NetworkManager.Singleton.LocalClientId}";
                localPlayerGameObject.transform.position = new Vector3(Random.Range(defaultPositionRange.x, defaultPositionRange.y), 0, 
                    Random.Range(defaultPositionRange.x, defaultPositionRange.y));
                playerInitialized = true;
            }
        }
    }
}
