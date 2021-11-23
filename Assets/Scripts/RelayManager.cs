using DilmerGames.Core.Singletons;
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : NetworkSingleton<RelayManager>
{
    private const string ENVIRONMENT = "production";

    [SerializeField]
    private int maxNumberOfConnections = 10;

    public bool IsRelayAvailable { get; private set; }

    public string JoinCode { get; private set; }

    async void Awake()
    {
        StartCoroutine(CheckForRelayAvailability());

        try
        {
            Logger.Instance.LogInfo($"Setting Up Relay Server | Connections: {maxNumberOfConnections}");

            var relayHostData = await SetupRelayServer(maxNumberOfConnections);

            // make code available for clients to join
            JoinCode = relayHostData.JoinCode;

            UnityTransport transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                relayHostData.IPv4Address,
                relayHostData.Port,
                relayHostData.AllocationIDBytes,
                relayHostData.Key,
                relayHostData.ConnectionData);

            IsRelayAvailable = true;

            JoinGame();
        }
        catch(Exception e)
        {
            Logger.Instance.LogError(e.Message);
        }
    }

    public async void JoinGame()
    {
        if (!IsRelayAvailable || string.IsNullOrEmpty(JoinCode))
        {
            Logger.Instance.LogWarning("Relay server is not available, unable to join...");
            return;
        }

        await JoinRelayServer(JoinCode);
    }

    private IEnumerator CheckForRelayAvailability()
    {
        while (!IsRelayAvailable)
        {
            yield return new WaitForSeconds(1.0f);
            Logger.Instance.LogInfo("Checking relay availability...");

        }
        Logger.Instance.LogInfo("Relay is now available...");
    }

    public static async Task<RelayHostData> SetupRelayServer(int maxConnections = 2)
    {
        InitializationOptions options = new InitializationOptions()
            .SetEnvironmentName(ENVIRONMENT);

        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Logger.Instance.LogInfo("SetupRelayServer not signed in...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);

        RelayHostData relayHostData = new RelayHostData
        {
            Key = allocation.Key,
            Port = (ushort) allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            IPv4Address = allocation.RelayServer.IpV4,
            ConnectionData = allocation.ConnectionData
        };

        relayHostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(relayHostData.AllocationID);

        Logger.Instance.LogInfo($"SetupRelayServer join code {relayHostData.JoinCode}...");

        return relayHostData;
    }

    public static async Task<RelayJoinData> JoinRelayServer(string joinCode)
    {
        InitializationOptions options = new InitializationOptions()
            .SetEnvironmentName(ENVIRONMENT);

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Logger.Instance.LogInfo("JoinRelayServer not signed in...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

        RelayJoinData relayJoinData = new RelayJoinData
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            HostConnectionData = allocation.HostConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };

        Logger.Instance.LogInfo($"SetupRelayServer allocation key {relayJoinData.Key}...");

        return relayJoinData;
    }
}
