using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerSlot : MonoBehaviour
{
    [SerializeField] private RawImage avatar;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hostBadgeText;
    [SerializeField] private TextMeshProUGUI readyStateText;

    private LobbyPlayer boundPlayer;
    private Texture2D loadedAvatar;
    private Callback<AvatarImageLoaded_t> avatarLoaded;
    private Callback<PersonaStateChange_t> personaChanged;

    private void OnEnable()
    {
        if(!SteamBootstrap.IsSteamInitialized) return;

        avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);
        personaChanged = Callback<PersonaStateChange_t>.Create(OnPersonaChanged);
    }

    private void OnDisable()
    {
        UnBind();
        avatarLoaded?.Dispose();
        personaChanged?.Dispose();
        avatarLoaded = null;
        personaChanged = null;
    }

    /// <summary>
    /// LobbyPlayer 정보를 바탕으로 Slot 데이터와 연결한다.
    /// </summary>
    /// <param name="player"></param>
    public void Bind(LobbyPlayer player)
    {
        UnBind();
        if (player == null) return;

        boundPlayer = player;
        player.steamId.OnValueChanged += OnSteamIdChanged;
        player.isReady.OnValueChanged += OnReadyChanged;

        // 호스트 배지 업데이트
        hostBadgeText.gameObject.SetActive(
            player.OwnerClientId == NetworkManager.ServerClientId);

        UpdateReady(player.isReady.Value);
        UpdateProfile(player.steamId.Value);
    }

    /// <summary>
    /// 연결되어 있는 데이터를 해제한다.
    /// </summary>
    public void UnBind()
    {
        if(boundPlayer != null)
        {
            boundPlayer.steamId.OnValueChanged -= OnSteamIdChanged;
            boundPlayer.isReady.OnValueChanged -= OnReadyChanged;
            boundPlayer = null;
        }

        if(loadedAvatar != null) Destroy(loadedAvatar);
        loadedAvatar = null;
        avatar.texture = null;
        nameText.text = string.Empty;
        readyStateText.text = string.Empty;
        hostBadgeText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 아바타 이미지의 다운로드가 끝난 시점에 호출된다
    /// </summary>
    /// <param name="result"></param>
    private void OnAvatarLoaded(AvatarImageLoaded_t result)
    {
        if (boundPlayer != null && result.m_steamID.m_SteamID == boundPlayer.steamId.Value)
            UpdateProfile(boundPlayer.steamId.Value);
    }

    /// <summary>
    /// Steam 유저 상태가 변경되는 시점에 호출된다.
    /// </summary>
    /// <param name="result"></param>
    private void OnPersonaChanged(PersonaStateChange_t result)
    {
        if (boundPlayer != null && result.m_ulSteamID == boundPlayer.steamId.Value)
            UpdateProfile(boundPlayer.steamId.Value);
    }

    public void OnSteamIdChanged(ulong prev, ulong next) => UpdateProfile(next);

    public void OnReadyChanged(bool prev, bool next) => UpdateReady(next);

    private void UpdateReady(bool ready) => readyStateText.text = ready? "Ready" : string.Empty;
    private void UpdateProfile(ulong id)
    {
        if(loadedAvatar != null) Destroy(loadedAvatar);
        loadedAvatar = null;
        avatar.texture = null;

        if (id == 0)
        {
            nameText.text = "Waiting...";
            return;
        }

        var steamId = new CSteamID(id);
        bool isLocal = id == SteamUser.GetSteamID().m_SteamID;

        if (!isLocal) SteamFriends.RequestUserInformation(steamId, false);

        // 본인이면 본인 이름, 아니면 친구 이름
        nameText.text = isLocal 
            ? SteamFriends.GetPersonaName() 
            : SteamFriends.GetFriendPersonaName(steamId);

        loadedAvatar = SteamAvatarUtility.GetSteamAvatar(steamId);
        avatar.texture = loadedAvatar;

    }

    public bool IsBoundTo(LobbyPlayer player) => boundPlayer == player;
}
