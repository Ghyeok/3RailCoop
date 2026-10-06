using Steamworks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobby : MonoBehaviour
{
    /// <summary>
    /// 로비 생성 결과를 받을 콜백 변수
    /// </summary>
    protected Callback<LobbyCreated_t> lobbyCreated;

    /// <summary>
    /// 스팀 아바타 이미지를 모두 내려받으면
    /// </summary>
    protected Callback<AvatarImageLoaded_t> avatarLoaded;

    /// <summary>
    /// 호스트의 주소의 Key로 사용할 문자열
    /// </summary>
    private const string HostAddressKey = "HostAddress";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (!SteamBootstrap.IsSteamInitialized) return;
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
    }

    /// <summary>
    /// UI 버튼에 연결할 방 만들기 함수
    /// </summary>
    public void CreateLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 2);
    }

    /// <summary>
    /// 스팀 서버가 로비를 다 만들고 나서 호출해주는 함수
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if(callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("로비 생성 실패!");
            return;
        }

        Debug.Log("스팀 로비 생성 성공!");

        // 호스트 본인의 Steam ID를 가져와서 문자열로 변환
        string hostSteamId = SteamUser.GetSteamID().ToString();

        // 방금 만들어진 로비(m_ulSteamIDLobby)에 방장 주소 기록
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, hostSteamId);       

        // 방장이 서버 겸 클라이언트로 네트워크 통신 시작
        NetworkManager.Singleton.StartHost();
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // 호스트가 적어둔 주소를 HostAddress 라는 Key로 꺼내옴
        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);

        Debug.Log($"로비 입장 성공! 호스트 주소는 : {hostAddress}");

        // NGO 클라이언트로 접속 시작
        NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// 연결된 모든 플레이어가 Ready인지?
    /// </summary>
    /// <returns></returns>
    private bool CheckAllPlayerReady()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<LobbyPlayer>();

            if (player == null || !player.isReady.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// UI 버튼에 연결할 게임 시작 함수
    /// </summary>
    private void LoadInGameSceneNetwork()
    {
        if (NetworkManager.Singleton.IsServer && CheckAllPlayerReady())
        {
            NetworkManager.Singleton.SceneManager.LoadScene("InGameScene", LoadSceneMode.Single);
        }
    }
}
