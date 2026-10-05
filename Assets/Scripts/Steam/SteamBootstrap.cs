using UnityEngine;
using Steamworks;

public class SteamBootstrap : MonoBehaviour
{
    public static bool IsSteamInitialized { get; private set; }

    private static SteamBootstrap instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        IsSteamInitialized = SteamAPI.Init();

        if (IsSteamInitialized)
        {
            Debug.Log($"스팀 연결 성공! 현재 사용자: {SteamFriends.GetPersonaName()}");
        }
        else
        {
            Debug.LogError("스팀 연결 실패! PC에 스팀 클라이언트가 켜져 있고 로그인되어 있는지 확인하세요.");
        }
    }

    private void Update()
    {
        if (IsSteamInitialized)
        {
            SteamAPI.RunCallbacks(); // 매 프레임 스팀 콜백 확인
        }
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        if (IsSteamInitialized)
        {
            SteamAPI.Shutdown();
            IsSteamInitialized = false;
        }

        instance = null;
    }
}
