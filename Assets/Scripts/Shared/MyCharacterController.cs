
using LiteNetLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyCharacterController
{
    ServerWorldStates _worldStates;
    ClientBehaviour _connection;
    uint _inputSequenceNumber = 0;
    long _playerId = -1;
    float _cameraDistance = 10f;
    Camera _camera;
    Vector3 _mouseDelta;

    bool _cameraTrace = true;
    UserInput _userInput;

    public MyCharacterController(
        ServerWorldStates worldStates,
        ClientBehaviour connection)
    {
        this._worldStates = worldStates;
        this._connection = connection;
        this._camera = Camera.main;
    }


    public void Initialize()
    {
        _connection.RegisterCallBack<JoinPacketAnswer>(OnJoinPacketAnswerReceived);
    }

    public void Update()
    {
        if (_playerId == -1)
        {
            return;
        }

        UpdateCameraData();
        CreateUserInput();
        if (Input.GetKeyDown(KeyCode.U))
        {
            _cameraTrace = !_cameraTrace;
        }
    }

    void UpdateCameraData()
    {
        if (_playerId == 1)
        {
            _cameraDistance = Mathf.Clamp(_cameraDistance + Input.mouseScrollDelta.y * Time.deltaTime * -20, min: 2, max: 15);
            var mouseX = Mathf.Clamp(Input.mousePosition.x, 0, Screen.width);
            var mouseY = Mathf.Clamp(Input.mousePosition.y, 0, Screen.height);

            _mouseDelta = new Vector3(mouseX / Screen.width - 0.5f, 0, mouseY / Screen.height - 0.5f);
        }
    }

    void CreateUserInput()
    {
        if (!ServerWorldStates.currentWorld.Players.TryGetValue(_playerId, out var player) ||
            !ServerWorldStates.currentWorld.Actors.TryGetValue(player.ControlActorId, out var actor) ||
            actor is not CharacterBase character)
        {
            return;
        }

        Vector2 input = Vector2.zero;
        if (_playerId == 1)
        {
            if (Input.GetKey(KeyCode.W))
            {
                input.y += 1;
            }
            if (Input.GetKey(KeyCode.S))
            {
                input.y -= 1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                input.x += 1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                input.x -= 1;
            }

        }
        else if (_playerId == 2)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                input.y += 1;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                input.y -= 1;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                input.x += 1;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                input.x -= 1;
            }
        }
        input.Normalize();

        _userInput = new UserInput
        {
            InputSequenceNumber = _inputSequenceNumber++,
            MoveInput = input,
            DeltaTime = Time.deltaTime,
            PlayerId = _playerId
        };

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, 1 << LayerMask.NameToLayer(LayerName.MouseRayTarget)))
        {
            _userInput.LookPoint = hit.point;
        }

        if (Input.GetMouseButtonDown(0))
        {
            _userInput.ActionCodes.Add(CharacterActionCode.A);
        }
    }

    public long GetControlActorId()
    {
        if (_worldStates.Players.TryGetValue(_playerId, out var player))
        {
            return player.ControlActorId;
        }
        return -1;
    }

    public void UpdateControlTarget()
    {
        if (_worldStates.Players.TryGetValue(_playerId, out var player))
        {
            if (_worldStates.Actors.TryGetValue(player.ControlActorId, out var actor) && actor is CharacterBase character)
            {
                character.DeletePredicted(player.LastUserInputSequenceNumber);
                character.PredicateUserInput(_userInput);
                character.PredictUpdate();

                if (_playerId == 1 && _cameraTrace)
                {
                    _camera.transform.position = character.CharacterData.WorldPosition + _camera.transform.forward * -_cameraDistance + _mouseDelta * 4f;
                }
            }
        }
    }

    void OnJoinPacketAnswerReceived(JoinPacketAnswer packet, NetPeer peer)
    {
        _playerId = packet.PlayerId;
    }
}