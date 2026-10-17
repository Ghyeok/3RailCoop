using Steamworks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobby : MonoBehaviour
{
    [SerializeField] private UI_Popup friendInvitePopup;

    private const int MIN_PLAYER = 2;
    private const int MAX_PLAYER = 2;

    private CSteamID currentLobbyId = CSteamID.Nil;
    private CSteamID requestedLobbyId = CSteamID.Nil;
    private bool isCreatingLobby;

    public event Action OnLobbyReady;

    /// <summary>
    /// 로비 생성 결과를 받을 콜백 변수
    /// </summary>
    protected Callback<LobbyCreated_t> lobbyCreated;

    /// <summary>
    /// 로비 참가 요청을 받을 콜백 변수
    /// </summary>
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;

    /// <summary>
    /// 로비 참가 요청을 받을 콜백 변수
    /// </summary>
    protected Callback<LobbyEnter_t> lobbyEntered;

    /// <summary>
    /// 호스트의 주소의 Key로 사용할 문자열
    /// </summary>
    private const string HostSteamIdKey = "HostSteamId";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (!SteamBootstrap.IsSteamInitialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    private void OnDestroy()
    {
        lobbyCreated?.Dispose();
        gameLobbyJoinRequested?.Dispose();
        lobbyEntered?.Dispose();
    }

    /// <summary>
    /// UI 버튼에 연결할 방 만들기 함수
    /// </summary>
    public void CreateLobby()
    {
        if (!SteamBootstrap.IsSteamInitialized)
        {
            Debug.LogWarning("Steam이 초기화되지 않았습니다.");
            return;
        }

        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            Debug.LogError("NetworkManager를 찾을 수 없습니다.");
            return;
        }

        if (isCreatingLobby || currentLobbyId.IsValid() || manager.IsListening || manager.ShutdownInProgress)
        {
            Debug.LogWarning("이미 로비를 생성 중이거나 네트워크가 실행 중입니다.");
            return;
        }

        isCreatingLobby = true;
        if (SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MAX_PLAYER) == SteamAPICall_t.Invalid)
        {
            isCreatingLobby = false;
            Debug.LogError("Steam 로비 생성 요청에 실패했습니다.");
        }
    }

    /// <summary>
    /// 스팀 서버가 로비를 다 만들고 나서 호출해주는 콜백
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        isCreatingLobby = false;

        if(callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("로비 생성 실패!");
            return;
        }

        var createdLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.IsListening || manager.ShutdownInProgress)
        {
            SteamMatchmaking.LeaveLobby(createdLobbyId);
            Debug.LogError("네트워크를 시작할 수 없어 생성된 Steam 로비를 떠납니다.");
            return;
        }

        // 호스트 본인의 Steam ID를 가져와서 문자열로 변환
        string hostSteamId = SteamUser.GetSteamID().ToString();

        // 방금 만들어진 로비(m_ulSteamIDLobby)에 방장 주소 기록
        SteamMatchmaking.SetLobbyData(createdLobbyId, HostSteamIdKey, hostSteamId);

        // 방장이 서버 겸 클라이언트로 네트워크 통신 시작
        if (!manager.StartHost())
        {
            SteamMatchmaking.LeaveLobby(createdLobbyId);
            Debug.LogError("호스트 시작에 실패해 생성된 Steam 로비를 떠납니다.");
            return;
        }

        currentLobbyId = createdLobbyId;
        Debug.Log("스팀 로비 생성 성공!");

        // 로비가 완료되면 UI 갱신
        OnLobbyReady?.Invoke();
    }

    /// <summary>
    /// 로비에 참여하는 클라이언트 대상 콜백 함수
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // 입장 성공 여부 판별
        if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError("입장 실패!");
            return;
        }

        // 로비의 소유자 아이디와 접속하려는 유저 아이디가 같으면 호스트로 판정
        var lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        bool isMyLobby = SteamMatchmaking.GetLobbyOwner(lobbyId) == SteamUser.GetSteamID();
        if (isMyLobby)
        {
            Debug.Log("호스트는 StartClient() 실행 불가");
            return;
        }

        // 호스트가 적어둔 주소를 HostAddress 라는 Key로 꺼내옴
        string hostSteamId = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostSteamIdKey);

        Debug.Log($"로비 입장 성공! 호스트 주소는 : {hostSteamId}");

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

    /// <summary>
    /// UI 버튼에 연결할 사용자가 로비를 떠날 때 호출되는 함수
    /// </summary>
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
            // TODO : 실제 게임 시작 로직
        }
        else
        {
            Debug.LogError("모든 플레이어가 준비되지 않았습니다.");
        }
    }

    /// <summary>
    /// UI 버튼에 연결할 친구 초대 함수.
    /// 만약 Steam Overlay가 비활성화 되어 있다면 인게임 팝업을 띄워 초대
    /// </summary>
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

        bool isOverlayEnabled = SteamUtils.IsOverlayEnabled();
        Debug.Log($"Overlay enabled: {isOverlayEnabled}");

        // 오버레이 권한이 활성화 되어 있므면 오버레이, 아니면 인게임 팝업
        if (isOverlayEnabled)
        {
            SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyId);
        }
        else
        {
            var popup = UIManager.Instance.OpenPopup(friendInvitePopup);
            UI_Popup_FriendInvite invitePopup = popup.GetComponent<UI_Popup_FriendInvite>();
            invitePopup.Init(SendInvite);
        }
    }

    /// <summary>
    /// 친구에게 초대를 보내는 메서드
    /// </summary>
    /// <param name="friendId"></param>
    private void SendInvite(CSteamID friendId)
    {
        if (!currentLobbyId.IsValid()) return;

        bool sent = SteamMatchmaking.InviteUserToLobby(currentLobbyId, friendId);
    }

    /// <summary>
    /// 초대 수락, 로비 참가 요청 콜백
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        requestedLobbyId = callback.m_steamIDLobby; // 참가 하고 싶은 로비 ID
        SteamMatchmaking.JoinLobby(requestedLobbyId); // 해당 로비에 비동기 참가 요청
    }
}
