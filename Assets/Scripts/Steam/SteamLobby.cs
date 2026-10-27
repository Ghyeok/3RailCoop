using Netcode.Transports;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobby : MonoBehaviour
{
    [SerializeField] private UI_Popup friendInvitePopup;

    private const int MIN_PLAYER = 2;
    private const int MAX_PLAYER = 2;
    private const float LobbyJoinTimeoutSeconds = 20f;

    private sealed class LobbyJoinAttempt
    {
        public CSteamID LobbyId; // 요청한 로비 ID
        public CallResult<LobbyEnter_t> Result; // 요청의 비동기 결과
        public Coroutine Timeout; // 응답 대기 시간 관리
        public bool TimedOut; // 시간 초과 여부
    }

    private CSteamID currentLobbyId = CSteamID.Nil;
    private CSteamID requestedLobbyId = CSteamID.Nil;
    private List<ulong> connectedClientIds = new List<ulong>();
    private bool isCreatingLobby;
    private bool isJoiningLobby;
    private LobbyJoinAttempt activeJoinAttempt;
    private readonly List<LobbyJoinAttempt> joinAttempts = new List<LobbyJoinAttempt>();

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
    /// 호스트의 주소의 Key로 사용할 문자열
    /// </summary>
    private const string HostSteamIdKey = "HostSteamId";

    #region Lifecycle

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (!SteamBootstrap.IsSteamInitialized) return;

        // Steam 콜백 등록
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);

        // NGO 콜백 등록
        NetworkManager.Singleton.OnConnectionEvent -= OnLobbyConnected;
        NetworkManager.Singleton.OnConnectionEvent += OnLobbyConnected;
    }

    private void OnDestroy()
    {
        lobbyCreated?.Dispose();
        gameLobbyJoinRequested?.Dispose();
        foreach (var attempt in joinAttempts)
            attempt.Result?.Dispose();
        joinAttempts.Clear();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnConnectionEvent -= OnLobbyConnected;
    }

    #endregion

    #region Lobby and UI actions

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

        if (isCreatingLobby || isJoiningLobby || currentLobbyId.IsValid() || manager.IsListening || manager.ShutdownInProgress)
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

    public void LeaveLobby(CSteamID steamId)
    {
        if (SteamBootstrap.IsSteamInitialized && steamId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(steamId);
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

    #endregion

    #region Steam and NGO callbacks

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
        bool isValidateLobbyData = SteamMatchmaking.SetLobbyData(createdLobbyId, HostSteamIdKey, hostSteamId);
        if (!isValidateLobbyData)
        {
            LeaveLobby(createdLobbyId);
            Debug.LogError("호스트의 Steam ID를 읽을 수 없습니다!");
            return;
        }

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
        if (isJoiningLobby && (callback.m_ulSteamIDLobby == requestedLobbyId.m_SteamID ||
            callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess))
        {
            isJoiningLobby = false;
            requestedLobbyId = CSteamID.Nil;
        }

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
        if (!ulong.TryParse(hostSteamId, out ulong parsedHostId) || parsedHostId == 0)
        {
            Debug.LogError("호스트 Steam ID가 올바르지 않습니다!");
            LeaveLobby(lobbyId);
            return;
        }

        // Transport가 Steam 통신에 사용할 SteamNetworkingSocketsTransport인지 확인
        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            Debug.LogError("NetworkManager를 찾을 수 없습니다.");
            LeaveLobby(lobbyId);
            return;
        }

        var transport = manager.NetworkConfig.NetworkTransport as SteamNetworkingSocketsTransport;
        if (transport == null)
        {
            Debug.LogError("Transport가 Null 입니다!");
            LeaveLobby(lobbyId);
            return;
        }

        // Transport에 연결할 Host Id를 제공
        transport.ConnectToSteamID = parsedHostId;

        // 현재 로비 아이디 캐싱
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // 설정한 호스트 ID로 NGO 클라이언트로 접속 시작
        var isSuccess = manager.StartClient();
        if (!isSuccess)
        {
            LeaveLobby(lobbyId);
            return;
        }

        Debug.Log($"로비 입장 성공! 호스트 ID는 : {hostSteamId}");
    }

    /// <summary>
    /// 초대 수락, 로비 참가 요청 콜백
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        var manager = NetworkManager.Singleton;

        if (manager == null)
        {
            Debug.LogError("NetworkManager를 찾을 수 없습니다.");
            return;
        }

        // 로비 생성 중, NGO가 실행 혹은 종료 중일 때 새 참가 요청을 무시
        if (isCreatingLobby || isJoiningLobby || currentLobbyId.IsValid() || manager.IsListening || manager.ShutdownInProgress)
        {
            Debug.LogError("이미 로비를 사용 중이거나 네트워크가 종료 중입니다.");
            return;
        }

        requestedLobbyId = callback.m_steamIDLobby; // 참가 하고 싶은 로비 ID
        isJoiningLobby = true;

        var call = SteamMatchmaking.JoinLobby(requestedLobbyId);
        if (call == SteamAPICall_t.Invalid)
        {
            isJoiningLobby = false;
            requestedLobbyId = CSteamID.Nil;
            Debug.LogError("Steam 로비 참가 요청을 시작하지 못했습니다.");
            return;
        }

        var attempt = new LobbyJoinAttempt { LobbyId = requestedLobbyId };
        attempt.Result = CallResult<LobbyEnter_t>.Create((result, ioFailure) => OnLobbyJoinResult(attempt, result, ioFailure));
        joinAttempts.Add(attempt);
        activeJoinAttempt = attempt;
        attempt.Result.Set(call);
        attempt.Timeout = StartCoroutine(WaitForLobbyJoin(attempt));
    }

    private void OnLobbyJoinResult(LobbyJoinAttempt attempt, LobbyEnter_t result, bool ioFailure)
    {
        if (attempt.Timeout != null)
            StopCoroutine(attempt.Timeout);
        joinAttempts.Remove(attempt);
        attempt.Result.Dispose();

        bool entered = !ioFailure &&
            result.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess;
        var resultLobbyId = new CSteamID(result.m_ulSteamIDLobby);

        if (attempt.TimedOut)
        {
            // 같은 로비를 다시 사용 중이면 오래된 응답으로 현재 연결을 끊지 않는다.
            if (entered && resultLobbyId.IsValid() && resultLobbyId != currentLobbyId &&
                (!isJoiningLobby || resultLobbyId != requestedLobbyId) && SteamBootstrap.IsSteamInitialized)
                SteamMatchmaking.LeaveLobby(resultLobbyId);
            return;
        }

        activeJoinAttempt = null;
        if (ioFailure || resultLobbyId != attempt.LobbyId)
        {
            isJoiningLobby = false;
            requestedLobbyId = CSteamID.Nil;
            if (SteamBootstrap.IsSteamInitialized)
            {
                if (attempt.LobbyId.IsValid())
                    SteamMatchmaking.LeaveLobby(attempt.LobbyId);
                if (entered && resultLobbyId.IsValid() && resultLobbyId != attempt.LobbyId)
                    SteamMatchmaking.LeaveLobby(resultLobbyId);
            }
            Debug.LogError("Steam 로비 참가 응답을 확인하지 못했습니다.");
            return;
        }

        // 이전 요청의 늦은 성공으로 이미 들어간 동일 로비도 정리한다.
        if (!entered && SteamBootstrap.IsSteamInitialized && attempt.LobbyId.IsValid())
            SteamMatchmaking.LeaveLobby(attempt.LobbyId);

        OnLobbyEntered(result);
    }

    /// <summary>
    /// 로비 연결 콜백, Steam이 아닌 NGO 콜백을 사용한다.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="data"></param>
    private void OnLobbyConnected(NetworkManager manager, ConnectionEventData data)
    {
        Debug.Log(
            $"[NGO 연결 이벤트] 종류: {data.EventType}, " +
            $"ClientId: {data.ClientId}, " +
            $"IsServer: {manager.IsServer}, " +
            $"IsListening: {manager.IsListening}, " +
            $"ShutdownInProgress: {manager.ShutdownInProgress}"
         );


        switch (data.EventType)
        {
            // 내가 호스트에 접속했거나, 호스트에게 새 게스트가 접속함
            case ConnectionEvent.ClientConnected:
                AddClientId(data.ClientId);

                // 이미 호스트에 접속해있는 게스트들
                if (!manager.IsServer)
                {
                    foreach (ulong peerId in data.PeerClientIds)
                        AddClientId(peerId);
                }
                break;

            // 내가 들어온 뒤 다른 게스트가 들어옴
            case ConnectionEvent.PeerConnected:
                AddClientId(data.ClientId);
                break;

            // 호스트에서 게스트가 나가거나, 게스트 자신이 호스트와 끊김
            case ConnectionEvent.ClientDisconnected:
                if (manager.IsServer)
                    connectedClientIds.Remove(data.ClientId);
                else
                    connectedClientIds.Clear();
                break;

            // 다른 게스트가 나감
            case ConnectionEvent.PeerDisconnected:
                connectedClientIds.Remove(data.ClientId);
                break;
        }

        Debug.Log($"현재 NGO 참여자: {string.Join(", ", connectedClientIds)}");
    }

    #endregion

    #region Helpers

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
    /// 친구에게 초대를 보내는 메서드
    /// </summary>
    /// <param name="friendId"></param>
    private void SendInvite(CSteamID friendId)
    {
        if (!currentLobbyId.IsValid()) return;

        bool sent = SteamMatchmaking.InviteUserToLobby(currentLobbyId, friendId);
    }

    /// <summary>
    /// 요청 객체(LobbyJoinAttempt) 마다 실행하는 로비 요청 대기 코루틴
    /// </summary>
    /// <param name="attempt"></param>
    /// <returns></returns>
    private IEnumerator WaitForLobbyJoin(LobbyJoinAttempt attempt)
    {
        yield return new WaitForSecondsRealtime(LobbyJoinTimeoutSeconds);
        if (activeJoinAttempt != attempt) yield break;

        attempt.TimedOut = true;
        activeJoinAttempt = null;
        isJoiningLobby = false;
        requestedLobbyId = CSteamID.Nil;

        // Steam 요청은 취소할 수 없으므로 늦게 도착하는 결과도 계속 감시한다.
        if (SteamBootstrap.IsSteamInitialized && attempt.LobbyId.IsValid())
            SteamMatchmaking.LeaveLobby(attempt.LobbyId);
        Debug.LogWarning("Steam 로비 참가 응답 시간이 초과되었습니다.");
    }

    private void AddClientId(ulong clientId)
    {
        if (!connectedClientIds.Contains(clientId))
        {
            connectedClientIds.Add(clientId);
        }
    }

    #endregion

}
