using Steamworks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobby : MonoBehaviour
{
    private const int MIN_PLAYER = 2;
    private const int MAX_PLAYER = 2;

    private CSteamID currentLobbyId = CSteamID.Nil;

    public event Action OnLobbyReady;

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
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MAX_PLAYER);
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

        // 현재 로비 아이디 캐싱
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // 호스트 본인의 Steam ID를 가져와서 문자열로 변환
        string hostSteamId = SteamUser.GetSteamID().ToString();

        // 방금 만들어진 로비(m_ulSteamIDLobby)에 방장 주소 기록
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, hostSteamId);       

        // 방장이 서버 겸 클라이언트로 네트워크 통신 시작
        NetworkManager.Singleton.StartHost();

        // 로비가 완료되면 UI 갱신
        OnLobbyReady?.Invoke();
    }

    /// <summary>
    /// 로비에 참여하는 클라이언트 대상 콜백 함수
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // 호스트가 적어둔 주소를 HostAddress 라는 Key로 꺼내옴
        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);

        Debug.Log($"로비 입장 성공! 호스트 주소는 : {hostAddress}");

        // 현재 로비 아이디 캐싱
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // NGO 클라이언트로 접속 시작
        NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// 연결된 모든 플레이어가 Ready인지?
    /// </summary>
    /// <returns></returns>
    private bool CheckAllPlayerReady()
    {
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        // 게임 플레이에 필요한 인원 수가 아니라면
        if (clients.Count < MIN_PLAYER)
            return false;

        foreach (var client in clients)
        {
            if (client.PlayerObject == null ||
                !client.PlayerObject.TryGetComponent<LobbyPlayer>(out var player) ||
                !player.isReady.Value)
                return false;
        }

        return true;
    }

    public void LeaveLobby()
    {
        if(SteamBootstrap.IsSteamInitialized && currentLobbyId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentLobbyId); ;
            currentLobbyId = CSteamID.Nil;
        }

        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening && !manager.ShutdownInProgress)
            manager.Shutdown();

        Debug.Log("로비를 나갑니다.");
    }

    /// <summary>
    /// UI 버튼에 연결할 게임 시작 함수
    /// </summary>
    public void StartGame()
    {
        if (NetworkManager.Singleton.IsServer && CheckAllPlayerReady())
        {
            NetworkManager.Singleton.SceneManager.LoadScene("InGameScene", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("모든 플레이어가 준비되지 않았습니다.");
        }
    }

    public void InviteFriends()
    {
        if (!SteamBootstrap.IsSteamInitialized)
        {
            Debug.LogWarning("Steam이 초기화되지 않았습니다.");
            return;
        }

        if (!currentLobbyId.IsValid())
        {
            Debug.LogWarning("아직 입장한 로비가 없습니다.");
            return;
        }

        Debug.Log($"Overlay enabled: {SteamUtils.IsOverlayEnabled()}");
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyId);
    }
}
