using System.Collections.Generic;
using System.Linq;
using Netcode.Transports;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// LobbyScene의 Steam·NGO 연결 설정이 맞는지 확인한다 (S-401).
/// 실제 Steam 로비·접속은 [수동] 절차로 확인한다.
/// </summary>
public class LobbySceneSetupTests
{
    private const string LOBBY_SCENE_PATH = "Assets/Scenes/LobbyScene.unity";

    private Scene scene;
    private bool wasInHierarchy;
    private bool loadedByTest;

    [OneTimeSetUp]
    public void OpenScene()
    {
        scene = SceneManager.GetSceneByPath(LOBBY_SCENE_PATH);
        wasInHierarchy = scene.IsValid();
        loadedByTest = !scene.isLoaded;
        if (loadedByTest)
            scene = EditorSceneManager.OpenScene(LOBBY_SCENE_PATH, OpenSceneMode.Additive);
    }

    [OneTimeTearDown]
    public void CloseScene()
    {
        // 테스트 전 상태로 되돌린다. 남은 씬이 하나뿐이면 닫을 수 없으므로 그대로 둔다.
        if (!loadedByTest || SceneManager.sceneCount <= 1)
            return;

        Assert.IsTrue(EditorSceneManager.CloseScene(scene, !wasInHierarchy), "LobbyScene을 닫지 못했습니다.");
    }

    private List<T> FindInScene<T>() where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToList();
    }

    [Test]
    public void Scene_HasOneSteamBootstrapAndSteamLobby()
    {
        Assert.AreEqual(1, FindInScene<SteamBootstrap>().Count);
        Assert.AreEqual(1, FindInScene<SteamLobby>().Count);
    }

    [Test]
    public void NetworkManager_UsesSteamNetworkingSocketsTransport()
    {
        var managers = FindInScene<NetworkManager>();

        Assert.AreEqual(1, managers.Count);
        Assert.IsInstanceOf<SteamNetworkingSocketsTransport>(managers[0].NetworkConfig.NetworkTransport);
    }

    [Test]
    public void NetworkManager_PlayerPrefabHasLobbyPlayer()
    {
        var manager = FindInScene<NetworkManager>().Single();
        GameObject prefab = manager.NetworkConfig.PlayerPrefab;

        Assert.IsNotNull(prefab);
        Assert.IsNotNull(prefab.GetComponent<NetworkObject>());
        Assert.IsNotNull(prefab.GetComponent<LobbyPlayer>());
    }
}
