using Steamworks;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class UI_Popup_FriendInvite : MonoBehaviour
{
    [SerializeField] private GameObject steamFriendSlot;
    [SerializeField] private Transform contentTransform;

    private Action<CSteamID> onInvite;

    public void Init(Action<CSteamID> action)
    {
        onInvite = action;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int friendsCnt = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        if(friendsCnt == -1) // Steam 로그인 상태 문제
        {
            // 재로그인?
            Debug.LogError("스팀 클라이언트에 로그인 해주세요.");
            return;
        }
        else if (friendsCnt == 0) // 친구 수 0
        {
            // 아무것도 안 띄움
            Debug.Log("친구 목록이 비었습니다.");
            return;
        }
        else
        {
            for (int i = 0; i < friendsCnt; i++)
            {
                CSteamID id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                string name = SteamFriends.GetFriendPersonaName(id);

                var slot = Instantiate(steamFriendSlot, contentTransform, false);
                SteamFriendSlot friendSlot = slot.GetComponent<SteamFriendSlot>();
                friendSlot.Bind(id, onInvite);

                Debug.Log(name);
            }
        }
    }
}
