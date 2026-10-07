using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UI_LobbyCanvas : MonoBehaviour
{
    [SerializeField] private SteamLobby steamLobby;

    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject waitingRoomPanel;

    [SerializeField] private UI_PlayerSlot playerSlot_host;
    [SerializeField] private UI_PlayerSlot playerSlot_client;

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
    }

    private void OnDisable()
    {
        steamLobby.OnLobbyReady -= ActiveWaitingRoomPanel;
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
        createRoomPanel.SetActive(true);
        waitingRoomPanel.SetActive(false);
    }

    private void OnLobbyPlayerSpawned(LobbyPlayer player)
    {
        if (!waitingRoomPanel.activeInHierarchy) return;

        UI_PlayerSlot slot =
            player.OwnerClientId == NetworkManager.ServerClientId
            ? playerSlot_host
            : playerSlot_client;

        slot.Bind(player);
    }

    private void OnLobbyPlayerDespawned(LobbyPlayer player)
    {
        UI_PlayerSlot slot =
            player.OwnerClientId == NetworkManager.ServerClientId
            ? playerSlot_host
            : playerSlot_client;

        if (slot.IsBoundTo(player))
            slot.UnBind();
    }
}
