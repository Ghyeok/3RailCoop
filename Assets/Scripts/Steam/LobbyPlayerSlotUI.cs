using System.Collections.Generic;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>로비 플레이어의 표시와 준비 버튼만 담당합니다. 접속 로직은 변경하지 않습니다.</summary>
public sealed class LobbyPlayerSlotUI : MonoBehaviour
{
    [SerializeField, Range(0, 1)] private int slotIndex;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private GameObject avatarPlaceholder;
    [SerializeField] private Image statusIndicator;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Color waitingColor = new Color(0.58f, 0.66f, 0.72f);
    [SerializeField] private Color readyColor = new Color(0.35f, 0.90f, 0.74f);

    private readonly List<LobbyPlayer> players = new List<LobbyPlayer>(2);
    private LobbyPlayer boundPlayer;
    private Texture2D ownedAvatar;
    private ulong avatarSteamId;
    private Callback<AvatarImageLoaded_t> avatarLoaded;

    private void OnEnable()
    {
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
        InvokeRepeating(nameof(RefreshBinding), 0f, 0.5f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshBinding));
        if (readyButton != null) readyButton.onClick.RemoveListener(OnReadyClicked);
        Bind(null);
        avatarLoaded?.Dispose();
        avatarLoaded = null;
    }

    private void RefreshBinding()
    {
        if (SteamBootstrap.IsSteamInitialized && avatarLoaded == null)
            avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);

        players.Clear();
        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening && manager.SpawnManager != null)
        {
            foreach (var networkObject in manager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject != null && networkObject.IsPlayerObject &&
                    networkObject.TryGetComponent<LobbyPlayer>(out var player))
                    players.Add(player);
            }
        }

        // 호스트(클라이언트 ID 0)가 첫 번째 자리를 사용하도록 모든 클라이언트에서 정렬합니다.
        players.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
        Bind(slotIndex < players.Count ? players[slotIndex] : null);
    }

    public void Bind(LobbyPlayer player)
    {
        if (boundPlayer != player)
        {
            if (boundPlayer != null)
            {
                boundPlayer.isReady.OnValueChanged -= OnReadyChanged;
                boundPlayer.steamId.OnValueChanged -= OnSteamIdChanged;
            }

            ReleaseAvatar();
            boundPlayer = player;
            if (boundPlayer != null)
            {
                boundPlayer.isReady.OnValueChanged += OnReadyChanged;
                boundPlayer.steamId.OnValueChanged += OnSteamIdChanged;
            }
        }

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        bool occupied = boundPlayer != null;
        bool ready = occupied && boundPlayer.isReady.Value;
        bool local = occupied && boundPlayer.IsSpawned && boundPlayer.IsOwner;

        if (playerNameText != null)
        {
            playerNameText.richText = false;
            playerNameText.text = occupied ? $"플레이어 {boundPlayer.OwnerClientId + 1:00}" :
                (slotIndex == 0 ? "플레이어 01" : "동료를 기다리는 중");
        }
        if (roleText != null)
            roleText.text = occupied ? (boundPlayer.OwnerClientId == 0 ? "HOST" : "CREW") +
                (local ? "  /  나" : "") : (slotIndex == 0 ? "첫 번째 승무원" : "두 번째 승무원");
        if (statusText != null)
        {
            statusText.text = !occupied ? "접속 대기" : (ready ? "준비 완료" : "준비 전");
            statusText.color = ready ? readyColor : waitingColor;
        }
        if (statusIndicator != null) statusIndicator.color = ready ? readyColor : waitingColor;
        if (readyButton != null) readyButton.interactable = local;
        if (readyButtonText != null)
        {
            readyButtonText.text = !occupied ? "준비하기" :
                (local ? (ready ? "준비 취소" : "준비하기") : (ready ? "준비 완료" : "준비 대기"));
            readyButtonText.color = local ? Color.white : waitingColor;
        }

        ulong steamId = occupied ? boundPlayer.steamId.Value : 0;
        if (steamId != 0 && SteamBootstrap.IsSteamInitialized)
        {
            if (playerNameText != null)
                playerNameText.text = SteamFriends.GetFriendPersonaName(new CSteamID(steamId));
            if (avatarSteamId != steamId)
            {
                ReleaseAvatar();
                avatarSteamId = steamId;
                LoadAvatar();
            }
        }
        else if (avatarSteamId != 0)
        {
            ReleaseAvatar();
        }
    }

    private void OnReadyClicked()
    {
        if (boundPlayer != null && boundPlayer.IsSpawned && boundPlayer.IsOwner)
            boundPlayer.ToggleReady();
    }

    private void OnReadyChanged(bool previous, bool current) => RefreshDisplay();
    private void OnSteamIdChanged(ulong previous, ulong current) => RefreshDisplay();

    private void OnAvatarLoaded(AvatarImageLoaded_t message)
    {
        if (avatarSteamId != 0 && message.m_steamID.m_SteamID == avatarSteamId)
            LoadAvatar();
    }

    private void LoadAvatar()
    {
        if (avatarImage == null || !SteamBootstrap.IsSteamInitialized) return;
        var texture = SteamAvatarUtility.GetSteamAvatar(new CSteamID(avatarSteamId));
        if (texture == null) return;
        if (ownedAvatar != null) Destroy(ownedAvatar);
        ownedAvatar = texture;
        avatarImage.texture = texture;
        avatarImage.enabled = true;
        if (avatarPlaceholder != null) avatarPlaceholder.SetActive(false);
    }

    private void ReleaseAvatar()
    {
        if (avatarImage != null)
        {
            avatarImage.texture = null;
            avatarImage.enabled = false;
        }
        if (ownedAvatar != null) Destroy(ownedAvatar);
        ownedAvatar = null;
        avatarSteamId = 0;
        if (avatarPlaceholder != null) avatarPlaceholder.SetActive(true);
    }
}
