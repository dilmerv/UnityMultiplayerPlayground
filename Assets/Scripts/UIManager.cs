using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Button startServerButton;

    [SerializeField]
    private Button startHostButton;

    [SerializeField]
    private Button startClientButton;

    private bool clientIdSet = false;

    private void Awake()
    {
        Cursor.visible = true;
    }

    void Start()
    {
        startServerButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartServer())
                Logger.Instance.LogInfo("Server started...");
            else
                Logger.Instance.LogInfo("Unable to start server...");
        });

        startHostButton.onClick.AddListener(() =>
        {
            if(NetworkManager.Singleton.StartHost())
                Logger.Instance.LogInfo("Host started...");
            else
                Logger.Instance.LogInfo("Unable to start host...");
        });

        startClientButton.onClick.AddListener(() =>
        {
            if(NetworkManager.Singleton.StartClient())
                Logger.Instance.LogInfo("Client started...");
            else
                Logger.Instance.LogInfo("Unable to start client...");
        });

        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Logger.Instance.LogInfo($"{id} just connected...");

            if (!clientIdSet)
            {
                var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()
                    .gameObject.GetComponentInChildren<TextMeshProUGUI>();
                localPlayer.text = $"ClientId: {NetworkManager.Singleton.LocalClientId}";
                clientIdSet = true;
            }
        };
    }
}
