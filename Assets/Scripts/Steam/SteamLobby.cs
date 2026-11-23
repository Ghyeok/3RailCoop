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
    private const float LOBBY_CREATION_TIMEOUT_SECONDS = 20f;
    private const float LOBBY_JOIN_TIMEOUT_SECONDS = 20f;
    private const float NGO_CONNECTION_TIMEOUT_SECONDS = 30f;
    private Coroutine ngoConnectionTimeout;

    private sealed class LobbyCreationAttempt
    {
        public CallResult<LobbyCreated_t> result;
        public Coroutine timeout;
        public bool timedOut;
    }

    private sealed class LobbyJoinAttempt
    {
        public CSteamID lobbyId; // 요청한 로비 ID
        public CallResult<LobbyEnter_t> result; // 요청의 비동기 결과
        public Coroutine timeout; // 응답 대기 시간 관리
        public bool timedOut; // 시간 초과 여부
    }

    private CSteamID currentLobbyId = CSteamID.Nil;
    private CSteamID requestedLobbyId = CSteamID.Nil;
    private List<ulong> connectedClientIds = new List<ulong>();
    private bool isCreatingLobby;
    private bool isJoiningLobby;
    private LobbyCreationAttempt activeCreationAttempt;
    private readonly List<LobbyCreationAttempt> CREATION_ATTEMPTS = new List<LobbyCreationAttempt>();
    private LobbyJoinAttempt activeJoinAttempt;
    private readonly List<LobbyJoinAttempt> JOIN_ATTEMPTS = new List<LobbyJoinAttempt>();
    private NetworkManager registeredNetworkManager;

    public event Action OnLobbyReady;
    public event Action OnLobbyClosed;

    /// <summary>
    /// 로비 참가 요청을 받을 콜백 변수
    /// </summary>
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<SteamServersDisconnected_t> steamServersDisconnected;
    private Callback<SteamServersConnected_t> steamServersConnected;

    /// <summary>
    /// 호스트의 주소의 Key로 사용할 문자열
    /// </summary>
    private const string HOST_STEAM_ID_KEY = "HostSteamId";

    #region Lifecycle

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (!SteamBootstrap.IsSteamInitialized) return;

        // Steam 콜백 등록
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        steamServersDisconnected = Callback<SteamServersDisconnected_t>.Create(OnSteamServersDisconnected);
        steamServersConnected = Callback<SteamServersConnected_t>.Create(OnSteamServersConnected);

        // NGO 콜백 등록
        if (!RegisterNetworkCallbacks(NetworkManager.Singleton))
            Debug.LogError("NetworkManager를 찾을 수 없어 NGO 콜백을 등록하지 못했습니다.");
    }

    private void OnDestroy()
    {
        gameLobbyJoinRequested?.Dispose();
        steamServersDisconnected?.Dispose();
        steamServersConnected?.Dispose();
        foreach (var attempt in CREATION_ATTEMPTS)
            attempt.result?.Dispose();
        CREATION_ATTEMPTS.Clear();
        foreach (var attempt in JOIN_ATTEMPTS)
            attempt.result?.Dispose();
        JOIN_ATTEMPTS.Clear();

        if (registeredNetworkManager != null)
        {
            registeredNetworkManager.OnConnectionEvent -= OnLobbyConnected;
            registeredNetworkManager.OnClientStopped -= OnClientStopped;
        }
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

        if (!SteamUser.BLoggedOn())
        {
            Debug.LogWarning("Steam 서버에 연결되지 않아 로비를 생성할 수 없습니다.");
            return;
        }

        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            Debug.LogError("NetworkManager를 찾을 수 없습니다.");
            return;
        }

        RegisterNetworkCallbacks(manager);

        if (isCreatingLobby || isJoiningLobby || currentLobbyId.IsValid() || manager.IsListening || manager.ShutdownInProgress)
        {
            Debug.LogWarning("이미 로비를 생성 중이거나 네트워크가 실행 중입니다.");
            return;
        }

        isCreatingLobby = true;
        var call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MAX_PLAYER);
        if (call == SteamAPICall_t.Invalid)
        {
            isCreatingLobby = false;
            Debug.LogError("Steam 로비 생성 요청에 실패했습니다.");
            return;
        }

        var attempt = new LobbyCreationAttempt();
        attempt.result = CallResult<LobbyCreated_t>.Create((result, ioFailure) => OnLobbyCreationResult(attempt, result, ioFailure));
        CREATION_ATTEMPTS.Add(attempt);
        activeCreationAttempt = attempt;
        attempt.result.Set(call);
        attempt.timeout = StartCoroutine(WaitForLobbyCreation(attempt));
    }

    /// <summary>
    /// UI 버튼에 연결할 사용자가 로비를 떠날 때 호출되는 함수
    /// </summary>
    public void LeaveLobby()
    {
        CancelNgoConnectionTimeout();

        if (SteamBootstrap.IsSteamInitialized && currentLobbyId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentLobbyId); ;
        }

        currentLobbyId = CSteamID.Nil;

        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening && !manager.ShutdownInProgress)
            manager.Shutdown();

        connectedClientIds.Clear();
        OnLobbyClosed?.Invoke();
        Debug.Log("로비를 나갑니다.");
    }

    public void LeaveLobby(CSteamID steamId)
    {
        CancelNgoConnectionTimeout();

        if (SteamBootstrap.IsSteamInitialized && steamId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(steamId);
        }

        currentLobbyId = CSteamID.Nil;

        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening && !manager.ShutdownInProgress)
            manager.Shutdown();

        connectedClientIds.Clear();
        OnLobbyClosed?.Invoke();
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

    private void OnLobbyCreationResult(LobbyCreationAttempt attempt, LobbyCreated_t callback, bool ioFailure)
    {
        if (attempt.timeout != null)
            StopCoroutine(attempt.timeout);
        CREATION_ATTEMPTS.Remove(attempt);
        attempt.result.Dispose();

        if (attempt.timedOut)
        {
            var lateLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            if (!ioFailure && callback.m_eResult == EResult.k_EResultOK && lateLobbyId.IsValid() &&
                lateLobbyId != currentLobbyId && SteamBootstrap.IsSteamInitialized)
            {
                SteamMatchmaking.LeaveLobby(lateLobbyId);
                Debug.LogWarning("시간 초과 후 생성된 Steam 로비를 떠났습니다.");
            }
            return;
        }

        if (activeCreationAttempt != attempt)
            return;

        activeCreationAttempt = null;
        isCreatingLobby = false;
        if (ioFailure)
        {
            Debug.LogError("Steam 로비 생성 응답을 받는 중 통신 오류가 발생했습니다.");
            return;
        }

        OnLobbyCreated(callback);
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
            Debug.LogError($"Steam 로비 생성 실패: {callback.m_eResult}");
            return;
        }

        var createdLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        if (!createdLobbyId.IsValid())
        {
            Debug.LogError("생성된 Steam 로비 ID가 유효하지 않습니다.");
            return;
        }

        if (!SteamBootstrap.IsSteamInitialized || !SteamUser.BLoggedOn())
        {
            if (SteamBootstrap.IsSteamInitialized)
                SteamMatchmaking.LeaveLobby(createdLobbyId);
            Debug.LogError("Steam 서버 연결이 끊겨 생성된 로비를 사용할 수 없습니다.");
            return;
        }
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.IsListening || manager.ShutdownInProgress)
        {
            SteamMatchmaking.LeaveLobby(createdLobbyId);
            Debug.LogError("네트워크를 시작할 수 없어 생성된 Steam 로비를 떠납니다.");
            return;
        }
        RegisterNetworkCallbacks(manager);

        // 호스트 본인의 Steam ID를 가져와서 문자열로 변환
        string hostSteamId = SteamUser.GetSteamID().ToString();

        // 방금 만들어진 로비(m_ulSteamIDLobby)에 방장 주소 기록
        bool isValidateLobbyData = SteamMatchmaking.SetLobbyData(createdLobbyId, HOST_STEAM_ID_KEY, hostSteamId);
        if (!isValidateLobbyData)
        {
            LeaveLobby(createdLobbyId);
            Debug.LogError("호스트의 Steam ID를 로비 데이터에 저장하지 못했습니다.");
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

        // 참가 중 기존 호스트가 떠나 게스트가 소유자가 되었는지 확인
        var lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        bool isMyLobby = SteamMatchmaking.GetLobbyOwner(lobbyId) == SteamUser.GetSteamID();
        if (isMyLobby)
        {
            Debug.LogWarning("로비 참가 중 소유자가 변경되어 참가를 취소합니다.");
            LeaveLobby(lobbyId);
            return;
        }

        // 호스트가 적어둔 주소를 HostAddress 라는 Key로 꺼내옴
        string hostSteamId = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HOST_STEAM_ID_KEY);
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
        RegisterNetworkCallbacks(manager);

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

        // 로비는 유효하지만 NGO 클라이언트 연결이 완료되지 않은 상태
        if(currentLobbyId.IsValid() && !manager.IsConnectedClient)
        {
            CancelNgoConnectionTimeout();
            ngoConnectionTimeout = StartCoroutine(WaitForNgoConnection(manager, currentLobbyId));
        }

        Debug.Log($"로비 입장 성공! 호스트 ID는 : {hostSteamId}");
    }

    /// <summary>
    /// 초대 수락, 로비 참가 요청 콜백
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        if (!SteamBootstrap.IsSteamInitialized || !SteamUser.BLoggedOn())
        {
            Debug.LogWarning("Steam 서버에 연결되지 않아 로비에 참가할 수 없습니다.");
            return;
        }

        var manager = NetworkManager.Singleton;

        if (manager == null)
        {
            Debug.LogError("NetworkManager를 찾을 수 없습니다.");
            return;
        }

        RegisterNetworkCallbacks(manager);

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

        var attempt = new LobbyJoinAttempt { lobbyId = requestedLobbyId };
        attempt.result = CallResult<LobbyEnter_t>.Create((result, ioFailure) => OnLobbyJoinResult(attempt, result, ioFailure));
        JOIN_ATTEMPTS.Add(attempt);
        activeJoinAttempt = attempt;
        attempt.result.Set(call);
        attempt.timeout = StartCoroutine(WaitForLobbyJoin(attempt));
    }

    private void OnLobbyJoinResult(LobbyJoinAttempt attempt, LobbyEnter_t result, bool ioFailure)
    {
        if (attempt.timeout != null)
            StopCoroutine(attempt.timeout);

        JOIN_ATTEMPTS.Remove(attempt);
        attempt.result.Dispose();

        bool entered = !ioFailure &&
            result.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess;
        var resultLobbyId = new CSteamID(result.m_ulSteamIDLobby);

        if (attempt.timedOut)
        {
            // 같은 로비를 다시 사용 중이면 오래된 응답으로 현재 연결을 끊지 않는다.
            if (entered && resultLobbyId.IsValid() && resultLobbyId != currentLobbyId &&
                (!isJoiningLobby || resultLobbyId != requestedLobbyId) && SteamBootstrap.IsSteamInitialized)
                SteamMatchmaking.LeaveLobby(resultLobbyId);
            return;
        }

        activeJoinAttempt = null;
        if (ioFailure || resultLobbyId != attempt.lobbyId)
        {
            isJoiningLobby = false;
            requestedLobbyId = CSteamID.Nil;
            if (SteamBootstrap.IsSteamInitialized)
            {
                if (attempt.lobbyId.IsValid())
                    SteamMatchmaking.LeaveLobby(attempt.lobbyId);
                if (entered && resultLobbyId.IsValid() && resultLobbyId != attempt.lobbyId)
                    SteamMatchmaking.LeaveLobby(resultLobbyId);
            }
            Debug.LogError("Steam 로비 참가 응답을 확인하지 못했습니다.");
            return;
        }

        // 이전 요청의 늦은 성공으로 이미 들어간 동일 로비도 정리한다.
        if (!entered && SteamBootstrap.IsSteamInitialized && attempt.lobbyId.IsValid())
            SteamMatchmaking.LeaveLobby(attempt.lobbyId);

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

                    CancelNgoConnectionTimeout();

                    // 게스트의 NGO 연결이 완료되면 대기방을 연다.
                    OnLobbyReady?.Invoke();
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

    private void OnClientStopped(bool wasHost)
    {
        CancelNgoConnectionTimeout();

        if (!currentLobbyId.IsValid())
            return;

        Debug.LogWarning(wasHost ? "호스트의 NGO 네트워크가 종료되었습니다." : "게스트의 NGO 클라이언트가 종료되었습니다.");
        LeaveLobby();
    }

    private void OnSteamServersDisconnected(SteamServersDisconnected_t callback)
    {
        Debug.LogWarning($"Steam 서버 연결이 끊어졌습니다. 재연결을 기다립니다: {callback.m_eResult}");

        if (activeCreationAttempt != null)
        {
            var attempt = activeCreationAttempt;
            attempt.timedOut = true;
            if (attempt.timeout != null) StopCoroutine(attempt.timeout);
            activeCreationAttempt = null;
            isCreatingLobby = false;
        }

        if (activeJoinAttempt != null)
        {
            var attempt = activeJoinAttempt;
            attempt.timedOut = true;
            if (attempt.timeout != null) StopCoroutine(attempt.timeout);
            activeJoinAttempt = null;
            isJoiningLobby = false;
            requestedLobbyId = CSteamID.Nil;
            if (SteamBootstrap.IsSteamInitialized && attempt.lobbyId.IsValid())
                SteamMatchmaking.LeaveLobby(attempt.lobbyId);
        }
    }

    private void OnSteamServersConnected(SteamServersConnected_t callback)
    {
        Debug.Log("Steam 서버 연결이 복구되었습니다.");
    }

    #endregion

    #region Helpers

    private bool RegisterNetworkCallbacks(NetworkManager manager)
    {
        if (manager == null)
            return false;
        if (registeredNetworkManager == manager)
            return true;

        if (registeredNetworkManager != null)
        {
            registeredNetworkManager.OnConnectionEvent -= OnLobbyConnected;
            registeredNetworkManager.OnClientStopped -= OnClientStopped;
        }

        registeredNetworkManager = manager;
        manager.OnConnectionEvent -= OnLobbyConnected;
        manager.OnConnectionEvent += OnLobbyConnected;
        manager.OnClientStopped -= OnClientStopped;
        manager.OnClientStopped += OnClientStopped;
        return true;
    }

    private IEnumerator WaitForLobbyCreation(LobbyCreationAttempt attempt)
    {
        yield return new WaitForSecondsRealtime(LOBBY_CREATION_TIMEOUT_SECONDS);
        if (activeCreationAttempt != attempt)
            yield break;

        attempt.timedOut = true;
        activeCreationAttempt = null;
        isCreatingLobby = false;
        Debug.LogWarning("Steam 로비 생성 응답 시간이 초과되었습니다.");
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
        yield return new WaitForSecondsRealtime(LOBBY_JOIN_TIMEOUT_SECONDS);
        if (activeJoinAttempt != attempt) yield break;

        attempt.timedOut = true;
        activeJoinAttempt = null;
        isJoiningLobby = false;
        requestedLobbyId = CSteamID.Nil;

        // Steam 요청은 취소할 수 없으므로 늦게 도착하는 결과도 계속 감시한다.
        if (SteamBootstrap.IsSteamInitialized && attempt.lobbyId.IsValid())
            SteamMatchmaking.LeaveLobby(attempt.lobbyId);
        Debug.LogWarning("Steam 로비 참가 응답 시간이 초과되었습니다.");
    }

    private void AddClientId(ulong clientId)
    {
        if (!connectedClientIds.Contains(clientId))
        {
            connectedClientIds.Add(clientId);
        }
    }

    private IEnumerator WaitForNgoConnection(NetworkManager manager, CSteamID lobbyId)
    {
        yield return new WaitForSecondsRealtime(NGO_CONNECTION_TIMEOUT_SECONDS);
        ngoConnectionTimeout = null;

        if(currentLobbyId != lobbyId || manager != null && manager.IsConnectedClient)
        {
            yield break;
        }

        Debug.LogWarning("NGO 연결 대기 시간이 초과되었습니다.");
        LeaveLobby();
    }

    private void CancelNgoConnectionTimeout()
    {
        if (ngoConnectionTimeout == null)
            return;

        StopCoroutine(ngoConnectionTimeout);
        ngoConnectionTimeout = null;
    }

    #endregion

}
