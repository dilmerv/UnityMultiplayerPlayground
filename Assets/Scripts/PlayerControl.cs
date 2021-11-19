using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerControl : NetworkBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walking
    }

    [SerializeField]
    private float speed = 3.5f;

    [SerializeField]
    private float rotationSpeed = 3.5f;

    [SerializeField]
    private Vector2 defaultInitialPositionOnPlane = new Vector2(-4, 4);

    [SerializeField]
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<Vector3> networkRotation = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();

    private CharacterController characterController;

    // client caches positions
    private Vector3 oldInputPosition = Vector3.zero;
    private Vector3 oldInputRotation = Vector3.zero;

    private Animator animator;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        transform.position = new Vector3(Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y), 0,
               Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y));
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

    private void ClientMoveAndRotate()
    {
        if (networkPosition.Value != Vector3.zero)
        {
            characterController.SimpleMove(networkPosition.Value);
        }
        if(networkRotation.Value != Vector3.zero)
        {
            transform.Rotate(networkRotation.Value);
        }
    }

    private void ClientVisuals()
    {
        if (networkPlayerState.Value == PlayerState.Walking)
        {
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }
    }

    private void ClientInput()
    {
        // y axis client rotation
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);

        // forward & backward direction
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        Vector3 inputPosition = direction * forwardInput;

        if(oldInputPosition != inputPosition || 
            oldInputRotation != inputRotation)
        {
            oldInputPosition = inputPosition;
            UpdateClientPositionAndRotationServerRpc(inputPosition * speed, inputRotation * rotationSpeed);
        }

        if (forwardInput > 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Walking);
        }
        else
        {
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        }
    }

    [ServerRpc]
    public void UpdateClientPositionAndRotationServerRpc(Vector3 newPosition, Vector3 newRotation)
    {
        networkPosition.Value = newPosition;
        networkRotation.Value = newRotation;
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        networkPlayerState.Value = state;
    }
}
