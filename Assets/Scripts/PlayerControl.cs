using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerControl : NetworkBehaviour
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
    private NetworkVariable<float> punchBlend = new NetworkVariable<float>();

    [SerializeField]
    private NetworkVariable<int> health = new NetworkVariable<int>(1000);

    private CharacterController characterController;

    // client caches positions
    private Vector3 oldInputPosition = Vector3.zero;
    private Vector3 oldInputRotation = Vector3.zero;

    private PlayerState oldPlayerState = PlayerState.Idle;

    private Animator animator;

    [SerializeField]
    private GameObject leftHand;

    [SerializeField]
    private GameObject rightHand;

    [SerializeField]
    private float minHitDistance = 1.0f;

    private bool blockPunch;

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
            if (networkPlayerState.Value == PlayerState.Punch && ActivePunchingKey())
            {
                CheckPunch(leftHand.transform, Vector2.up);
                CheckPunch(rightHand.transform, Vector2.down);
            }
        }
    }

    private void CheckPunch(Transform transform, Vector3 aimDirecton)
    {
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Player");

        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.TransformDirection(aimDirecton), out hit, minHitDistance, layerMask))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(aimDirecton) * hit.distance, Color.yellow);
            //Logger.Instance.LogInfo("Did Hit: " + hit.transform.name);
            var n = hit.transform.GetComponent<NetworkObject>();
            if (n != null)
            {
                //Logger.Instance.LogInfo("Did Hit NetworkObj: " + n.OwnerClientId);
                UpdateHealthServerRpc(1, n.OwnerClientId);
            }
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(aimDirecton) * minHitDistance, Color.white);
        }

        //blockPunch = true;

        // only allows to raycast every other second
        //yield return new WaitForSeconds(1.0f);

        //blockPunch = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Logger.Instance.LogInfo($"OnCollisionEnter with name: {collision.gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Logger.Instance.LogInfo($"OnTriggerEnter with name: {other.gameObject.name}");
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
                animator.SetFloat($"{PlayerState.Punch}Blend", punchBlend.Value);
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

        // change punching state
        if (ActivePunchingKey() && forwardInput == 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Punch);
            return;
        }

        // change character movement
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

    private static bool ActivePunchingKey()
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
    public void UpdateHealthServerRpc(int takeAwayLife, ulong clientId)
    {
        var clientWithDamaged = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerControl>();
        if(clientWithDamaged != null && clientWithDamaged.health.Value > 0)
            clientWithDamaged.health.Value -= takeAwayLife;

        NotifyHealthChangedClientRpc(takeAwayLife, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }

    [ClientRpc]
    public void NotifyHealthChangedClientRpc(int takeAwayLife, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;

        Logger.Instance.LogInfo($"Client is losing {takeAwayLife} live(s)");
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        networkPlayerState.Value = state;
        if (state == PlayerState.Punch)
        {
            punchBlend.Value = Random.Range(0.0f, 1.0f);
        }
    }
}
