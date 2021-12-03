using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerWithRaycastControl : NetworkBehaviour
{
    [SerializeField]
    private float walkSpeed = 3.5f;

    [SerializeField]
    private float runSpeedOffset = 2.0f;

    [SerializeField]
    private float rotationSpeed = 3.5f;

    [SerializeField]
    private Vector2 defaultInitialPositionOnPlane = new Vector2(-4, 4);

    [SerializeField]
    private NetworkVariable<Vector3> networkPositionDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<Vector3> networkRotationDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();


    [SerializeField]
    private NetworkVariable<float> networkPlayerHealth = new NetworkVariable<float>(1000);

    [SerializeField]
    private NetworkVariable<float> networkPlayerPunchBlend = new NetworkVariable<float>();

    [SerializeField]
    private GameObject leftHand;

    [SerializeField]
    private GameObject rightHand;

    [SerializeField]
    private float minPunchDistance = 1.0f;

    private CharacterController characterController;

    // client caches positions
    private Vector3 oldInputPosition = Vector3.zero;
    private Vector3 oldInputRotation = Vector3.zero;
    private PlayerState oldPlayerState = PlayerState.Idle;

    private Animator animator;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (IsClient && IsOwner)
        {
            transform.position = new Vector3(Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y), 0,
                   Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y));

            PlayerCameraFollow.Instance.FollowPlayer(transform.Find("PlayerCameraRoot"));
        }
    }

    void Update()
    {
        if (IsClient && IsOwner)
        {
            ClientInput();
        }

        ClientMoveAndRotate();
        ClientVisuals();
    }

    private void FixedUpdate()
    {
        if (IsClient && IsOwner)
        {
            if (networkPlayerState.Value == PlayerState.Punch && ActivePunchActionKey())
            {
                CheckPunch(leftHand.transform, Vector3.up);
                CheckPunch(rightHand.transform, Vector3.down);
            }
        }
    }

    private void CheckPunch(Transform hand, Vector3 aimDirection)
    {
        RaycastHit hit;

        int layerMask = LayerMask.GetMask("Player");

        if (Physics.Raycast(hand.position, hand.transform.TransformDirection(aimDirection), out hit, minPunchDistance, layerMask))
        {
            Debug.DrawRay(hand.position, hand.transform.TransformDirection(aimDirection) * minPunchDistance, Color.yellow);

            var playerHit = hit.transform.GetComponent<NetworkObject>();
            if (playerHit != null)
            { 
                UpdateHealthServerRpc(1, playerHit.OwnerClientId);
            }
        }
        else
        {
            Debug.DrawRay(hand.position, hand.transform.TransformDirection(aimDirection) * minPunchDistance, Color.red);
        }
    }


    private void ClientMoveAndRotate()
    {
        if (networkPositionDirection.Value != Vector3.zero)
        {
            characterController.SimpleMove(networkPositionDirection.Value);
        }
        if (networkRotationDirection.Value != Vector3.zero)
        {
            transform.Rotate(networkRotationDirection.Value, Space.World);
        }
    }

    private void ClientVisuals()
    {
        if (oldPlayerState != networkPlayerState.Value)
        {
            oldPlayerState = networkPlayerState.Value;
            animator.SetTrigger($"{networkPlayerState.Value}");
            if (networkPlayerState.Value == PlayerState.Punch)
            {
                animator.SetFloat($"{networkPlayerState.Value}Blend", networkPlayerPunchBlend.Value);
            }
        }
    }

    private void ClientInput()
    {
        // left & right rotation
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);

        // forward & backward direction
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        Vector3 inputPosition = direction * forwardInput;

        // change fighting states
        if (ActivePunchActionKey() && forwardInput == 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Punch);
            return;
        }

        // change motion states
        if (forwardInput == 0)
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        else if (!ActiveRunningActionKey() && forwardInput > 0 && forwardInput <= 1)
            UpdatePlayerStateServerRpc(PlayerState.Walk);
        else if (ActiveRunningActionKey() && forwardInput > 0 && forwardInput <= 1)
        {
            inputPosition = direction * runSpeedOffset;
            UpdatePlayerStateServerRpc(PlayerState.Run);
        }
        else if (forwardInput < 0)
            UpdatePlayerStateServerRpc(PlayerState.ReverseWalk);

        // let server know about position and rotation client changes
        if (oldInputPosition != inputPosition ||
            oldInputRotation != inputRotation)
        {
            oldInputPosition = inputPosition;
            oldInputRotation = inputRotation;
            UpdateClientPositionAndRotationServerRpc(inputPosition * walkSpeed, inputRotation * rotationSpeed);
        }
    }

    private static bool ActiveRunningActionKey()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private static bool ActivePunchActionKey()
    {
        return Input.GetKey(KeyCode.Space);
    }

    [ServerRpc]
    public void UpdateClientPositionAndRotationServerRpc(Vector3 newPosition, Vector3 newRotation)
    {
        networkPositionDirection.Value = newPosition;
        networkRotationDirection.Value = newRotation;
    }

    [ServerRpc]
    public void UpdateHealthServerRpc(int takeAwayPoint, ulong clientId)
    {
        var clientWithDamaged = NetworkManager.Singleton.ConnectedClients[clientId]
            .PlayerObject.GetComponent<PlayerWithRaycastControl>();

        if (clientWithDamaged != null && clientWithDamaged.networkPlayerHealth.Value > 0)
        {
            clientWithDamaged.networkPlayerHealth.Value -= takeAwayPoint;
        }

        // execute method on a client getting punch
        NotifyHealthChangedClientRpc(takeAwayPoint, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }

    [ClientRpc]
    public void NotifyHealthChangedClientRpc(int takeAwayPoint, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;

        Logger.Instance.LogInfo($"Client got punch {takeAwayPoint}");
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        networkPlayerState.Value = state;
        if (state == PlayerState.Punch)
        {
            networkPlayerPunchBlend.Value = Random.Range(0.0f, 1.0f);
        }
    }
}
