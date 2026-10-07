using Steamworks;
using System;
using Unity.Netcode;
using UnityEngine;

public class LobbyPlayer : NetworkBehaviour
{
    public static event Action<LobbyPlayer> OnLobbyPlayerSpawned;
    public static event Action<LobbyPlayer> OnLobbyPlayerDespawned;

    /// <summary>
    /// 준비 상태를 모든 플레이어에게 실시간으로 동기화하는 변수
    /// </summary>
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// 각 플레이어의 고유 Steam ID를 동기화하기 위한 변수
    /// </summary>
    public NetworkVariable<ulong> steamId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        isReady.OnValueChanged -= OnReadyChanged;
        isReady.OnValueChanged += OnReadyChanged;
        OnLobbyPlayerSpawned?.Invoke(this);

        if (IsOwner)
        {
            ulong mySteamId = SteamUser.GetSteamID().m_SteamID;
            SetSteamIdServerRPC(mySteamId );
        }
    }

    [ServerRpc]
    private void SetSteamIdServerRPC(ulong id)
    {
        steamId.Value = id;
    }

    /// <summary>
    /// UI의 [준비 완료] 버튼을 눌렀을 때 호출할 함수
    /// </summary>
    public void ToggleReady()
    {
        if (IsOwner)
        {
            SetReadyServerRPC(!isReady.Value);
        }
    }

    [ServerRpc]
    private void SetReadyServerRPC(bool newReadyState)
    {
        isReady.Value = newReadyState;
    }

    private void OnReadyChanged(bool prevValue, bool newValue)
    {
        Debug.Log($"준비 상태 변경 : {prevValue} -> {newValue}");
        // TODO: UI 텍스트나 체크박스 갱신
    }

    public override void OnNetworkDespawn()
    {
        OnLobbyPlayerDespawned?.Invoke(this);
        isReady.OnValueChanged -= OnReadyChanged;
    }
}
