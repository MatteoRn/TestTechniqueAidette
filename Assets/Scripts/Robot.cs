using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Robot : MonoBehaviour
{
    [Header("Command's Variables :")]
    [SerializeField] private string _CommandSuccesMessage = " effectué avec succès";

    [Header("Movement's Variables :")]
    [SerializeField] private float _Speed = 2.0f;
    [SerializeField] private float _JumpForce = 5.0f;
    private Vector3 _Velocity;
    private const float GRAVITY = -9.81f;

    // ---------------------------------------------------------------------

    [Header("Communication's Variables :")]
    [SerializeField] private float _CommunicationCooldown = 20f;
    [SerializeField] private readonly string _CommunicationNormalMessage = "Alive";
    private string _CommunicationMessage = "Alive";
    private bool _CanCommunicate = true;

    // ---------------------------------------------------------------------

    private Coroutine _CommunicateCoroutine, _CommandCoroutine = null;
    private InputMap _InputMap;
    private CharacterController _CharacterController;

    // ---------------------------------------------------------------------

    // DATAS

    private List<CommandDecryptor.TextInfos> _TextInfosQueue = new List<CommandDecryptor.TextInfos>();

    private Dictionary<CommandDecryptor.CommandAction, Action> _ActionsEvents;

    // ---------------------------------------------------------------------

    private void Awake()
    {
        _CharacterController = GetComponent<CharacterController>();
    }
    void Start()
    {
        // Connect methods to input
        _InputMap = new InputMap();

        _InputMap.Main.Movements.started += MovementInput;
        _InputMap.Main.Movements.performed += MovementInput;
        _InputMap.Main.Movements.canceled += MovementInput;
        _InputMap.Main.Jump.started += JumpInput;

        _InputMap.Enable();

        // ----------------------------------------------

        _CommunicateCoroutine = StartCoroutine(CommunicationTimer());
        CommandDecryptor.textDecryptEvent += GetCommand;
        CommandDecryptor.onCommandIsWriting += OnCommandIsWriting;
        SetActionDictionary();
    }

    /// <summary>
    /// Init "_ActionsEvents" dictionary to manage what action does
    /// </summary>
    private void SetActionDictionary()
    {
        _ActionsEvents = new Dictionary<CommandDecryptor.CommandAction, Action>()
        {
            { CommandDecryptor.CommandAction.Jump, Jump},
            { CommandDecryptor.CommandAction.Move_Fwd, () => SetVelocity(transform.forward)},
            { CommandDecryptor.CommandAction.Move_Left, () => SetVelocity(-transform.right)},
            { CommandDecryptor.CommandAction.Move_Right, () => SetVelocity(transform.right)},
            { CommandDecryptor.CommandAction.Move_Back, () => SetVelocity(-transform.forward)}
        };
    }

    void Update()
    {
        if (!_CharacterController.isGrounded) _Velocity.y += GRAVITY * Time.deltaTime;
        
        _CharacterController.Move(_Velocity * Time.deltaTime);
    }

    #region Command

    /// <summary>
    /// Get the latest command and place it in the command queue
    /// </summary>
    /// <param name="pTextInfos"></param>
    private void GetCommand(CommandDecryptor.TextInfos pTextInfos)
    {
        if (pTextInfos.time < 1 && _TextInfosQueue.Count > 1) _TextInfosQueue.Insert(1, pTextInfos);
        else _TextInfosQueue.Add(pTextInfos);

        _CommandCoroutine ??= StartCoroutine(ExecuteCommand());
    }

    private IEnumerator ExecuteCommand()
    {
        // Wait the expected time 
        yield return new WaitForSeconds((float)_TextInfosQueue[0].time);
        _CommunicationMessage = _TextInfosQueue[0].commandAction.ToString();

        // Call Event for the Action
        _ActionsEvents[_TextInfosQueue[0].commandAction].Invoke();
        print(_TextInfosQueue[0].commandAction + _CommandSuccesMessage);

        // Reset velocity
        yield return new WaitForSeconds(.2f);
        SetVelocity(Vector3.zero);

        // Update commands list
        _CommunicationMessage = _CommunicationNormalMessage;
        _TextInfosQueue.RemoveAt(0);
        StopCoroutine(_CommandCoroutine);

        _CommandCoroutine = _TextInfosQueue.Count > 0 ? StartCoroutine(ExecuteCommand()) : null;
    }

    private void OnCommandIsWriting(bool pIsWriting) 
    {
        if (pIsWriting) _InputMap.Disable();
        else _InputMap.Enable();
    } 

    #endregion

    #region Inputs & Movements
    private void MovementInput(InputAction.CallbackContext pContext)
    {
        SetVelocity((transform.forward * pContext.ReadValue<Vector2>().y) + (transform.right * pContext.ReadValue<Vector2>().x));
    }
    private void SetVelocity(Vector3 pDirection) => _Velocity = new Vector3(pDirection.x, _Velocity.y, pDirection.z);
    private void JumpInput(InputAction.CallbackContext pContext) => Jump();
    private void Jump()
    {
        if (!_CharacterController.isGrounded) return;
        _Velocity.y = _JumpForce;
    }

    #endregion

    #region Communication
    private void Communicate() => print(_CommunicationMessage);
    private IEnumerator CommunicationTimer()
    {
        while (_CanCommunicate)
        {
            yield return new WaitForSeconds(_CommunicationCooldown);
            Communicate();
        }

        StopCoroutine(CommunicationTimer());
        _CommunicateCoroutine = null;
    }

    #endregion

    private void OnDestroy()
    {
        _InputMap.Main.Movements.started -= MovementInput;
        _InputMap.Main.Movements.performed -= MovementInput;
        _InputMap.Main.Movements.canceled -= MovementInput;
        _InputMap.Main.Jump.started -= JumpInput;

        CommandDecryptor.textDecryptEvent -= GetCommand;
        CommandDecryptor.onCommandIsWriting -= OnCommandIsWriting;

        StopAllCoroutines();
        if (_CommunicateCoroutine != null)
        {
            _CommunicateCoroutine = null;
        }
    }
}
