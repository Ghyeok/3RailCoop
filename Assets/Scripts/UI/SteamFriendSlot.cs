using Steamworks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SteamFriendSlot : MonoBehaviour
{
    [SerializeField] RawImage friendAvatar;
    [SerializeField] TextMeshProUGUI friendText;
    [SerializeField] Button inviteButton;

    private Texture2D loadedAvatar;
    private CSteamID friendId;
    private Callback<AvatarImageLoaded_t> avatarImageLoaded;

    private void Start()
    {
        avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(ReloadAvatar);
    }

    /// <summary>
    /// 스팀 친구 UI 요소들과 데이터 연결
    /// </summary>
    /// <param name="friendId"></param>
    /// <param name="onInvite"></param>
    public void Bind(CSteamID friendId, Action<CSteamID> onInvite)
    {
        this.friendId = friendId;

        friendText.text = SteamFriends.GetFriendPersonaName(friendId);
        loadedAvatar = SteamAvatarUtility.GetSteamAvatar(friendId);
        friendAvatar.texture = loadedAvatar;

        inviteButton.onClick.AddListener(() => onInvite?.Invoke(friendId));
    }

    private void ReloadAvatar(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID != friendId) return;

        Texture2D texture = SteamAvatarUtility.GetSteamAvatar(friendId);
        if (texture == null) return;

        if (loadedAvatar != null) Destroy(loadedAvatar);
        loadedAvatar = texture;
        friendAvatar.texture = loadedAvatar;
    }

    private void OnDestroy()
    {
        avatarImageLoaded?.Dispose();
    }
}
