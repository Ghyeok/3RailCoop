using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class UI_LobbyCanvas : UIBase
{
    [SerializeField] private SteamLobby steamLobby;

    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject waitingRoomPanel;

    [FormerlySerializedAs("playerSlot_host")]
    [SerializeField] private UI_PlayerSlot playerSlotHost;
    [FormerlySerializedAs("playerSlot_client")]
    [SerializeField] private UI_PlayerSlot playerSlotClient;

    private void Start()
    {
        createRoomPanel.SetActive(true);
        waitingRoomPanel.SetActive(false);
    }

    private void OnEnable()
    {
        LobbyPlayer.OnLobbyPlayerSpawned -= OnLobbyPlayerSpawned;
        LobbyPlayer.OnLobbyPlayerSpawned += OnLobbyPlayerSpawned;

        LobbyPlayer.OnLobbyPlayerDespawned -= OnLobbyPlayerDespawned;
        LobbyPlayer.OnLobbyPlayerDespawned += OnLobbyPlayerDespawned;

        steamLobby.OnLobbyReady -= ActiveWaitingRoomPanel;
        steamLobby.OnLobbyReady += ActiveWaitingRoomPanel;

        steamLobby.OnLobbyClosed -= ReturnToCreateRoomPanel;
        steamLobby.OnLobbyClosed += ReturnToCreateRoomPanel;
    }

    private void OnDisable()
    {
        steamLobby.OnLobbyReady -= ActiveWaitingRoomPanel;
        steamLobby.OnLobbyClosed -= ReturnToCreateRoomPanel;

        LobbyPlayer.OnLobbyPlayerSpawned -= OnLobbyPlayerSpawned;
        LobbyPlayer.OnLobbyPlayerDespawned -= OnLobbyPlayerDespawned;
    }

    /// <summary>
    /// 대기방 패널을 활성화 시킨다.
    /// </summary>
    private void ActiveWaitingRoomPanel()
    {
        createRoomPanel.SetActive(false);
        waitingRoomPanel.SetActive(true);

        // 호스트 플레이어가 이미 생성된 경우도 처리
        foreach(var p in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if(p.TryGetComponent(out LobbyPlayer player))
                OnLobbyPlayerSpawned(player);
        }
    }

    private void ReturnToCreateRoomPanel()
    {
        createRoomPanel.SetActive(true);
        waitingRoomPanel.SetActive(false);
    }

    public void OnReadyClicked()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.SpawnManager == null) return;

        var localPlayer = manager.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null &&
            localPlayer.TryGetComponent(out LobbyPlayer player))
        {
            player.ToggleReady();
        }
    }

    public void OnLeaveClicked()
    {
        steamLobby.LeaveLobby();
    }

    private void OnLobbyPlayerSpawned(LobbyPlayer player)
    {
        if (!waitingRoomPanel.activeInHierarchy) return;

        UI_PlayerSlot slot =
            player.OwnerClientId == NetworkManager.ServerClientId
            ? playerSlotHost
            : playerSlotClient;

        slot.Bind(player);
    }

    private void OnLobbyPlayerDespawned(LobbyPlayer player)
    {
        UI_PlayerSlot slot =
            player.OwnerClientId == NetworkManager.ServerClientId
            ? playerSlotHost
            : playerSlotClient;

        if (slot.IsBoundTo(player))
            slot.UnBind();
    }
}
