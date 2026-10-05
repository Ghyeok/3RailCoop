# 3RailCoop 로비 UI

`Assets/Scenes/SampleScene.unity`를 열면 완성된 로비 화면이 표시됩니다.
재사용용 Canvas는 `Assets/UI/Lobby/LobbyCanvas.prefab`입니다.

## 이번 작업 범위

- UI 작업에서 기존 Steam/NGO 스크립트 4개는 수정하지 않았습니다. 작업 도중 외부에서 변경된 `SteamLobby.OnLobbyEntered(LobbyEnter_t)` 시그니처는 유지했습니다.
- 화면 바인딩을 위해 `Assets/Scripts/Steam/LobbyPlayerSlotUI.cs`만 추가했습니다.
- 기존 `LobbyPlayer.OnReadyChanged`에는 UI 갱신 TODO만 있어 옮길 UI 구현은 없었습니다.
- Canvas, 입력 시스템용 EventSystem, 한글 TMP 글꼴과 필수 TMP 리소스를 추가했습니다.
- `LobbyCanvas/LobbyActions`의 `SteamLobby`를 방 만들기 버튼의 영구 이벤트에 연결했습니다.

## 플레이어 카드 연결

각 카드의 `LobbyPlayerSlotUI`는 실행 중 `NetworkManager.Singleton`의 생성된 플레이어 오브젝트를 조회합니다. 해당 오브젝트에 `LobbyPlayer`가 있어야 합니다. 소유자 클라이언트 ID 순서로 첫 번째와 두 번째 슬롯을 채우며, 준비 상태와 Steam ID 변경을 구독합니다.

- Steam 이름과 아바타: 기존 `SteamAvatarUtility`를 사용합니다.
- 준비 버튼: 자신이 소유하고 네트워크에 생성된 플레이어에만 활성화하며 기존 `LobbyPlayer.ToggleReady()`를 호출합니다.
- 플레이어 해제: 이름, 상태, 아바타를 초기화하고 이벤트 구독을 해제합니다.
- 공개 `Bind(LobbyPlayer)`는 직접 바인딩과 UI 단독 확인에도 사용할 수 있습니다. 실행 중에는 자동 조회 결과가 우선합니다.

## 현재 사용할 수 없는 기능

사용자 확인에 따라 현재 프로젝트에 있는 구성만 사용했습니다. 다음 항목이 없어 방 만들기·친구 로비 참가·게임 시작 버튼은 의도적으로 비활성화되어 있습니다.

1. 씬의 NetworkManager, 전송 컴포넌트, LobbyPlayer가 붙은 네트워크 플레이어 프리팹.
2. Steam 로비 참가 요청/입장 콜백 등록과 호스트 주소의 전송 계층 연결. 현재 `OnLobbyEntered(LobbyEnter_t)`는 등록되지 않았습니다.
3. 외부에서 호출 가능한 게임 시작 진입점과 `InGameScene`. 현재 `LoadInGameSceneNetwork`는 private이며 대상 씬은 없습니다.

해당 로직은 이번 작업에서 수정하지 않았습니다. 방 만들기 버튼에는 함수 참조만 연결되어 있으며, 네트워크 구성을 완료한 뒤 개발자가 활성화해야 합니다. 참가와 시작 버튼은 각 기능의 공개 진입점 구현 후 연결해야 합니다. 연결 대기 안내 문구도 현재 씬 상태를 나타내는 고정 문구입니다.

실제 두 Steam 클라이언트의 접속은 검증하지 않았습니다. UI 표시, 빈 슬롯/준비 완료/해제 상태, 준비 상태 변경 이벤트, 마우스 클릭/키보드 제출, 안내 창을 검증했습니다. 1920×1080과 1024×768 배치를 확인했으며, 최종 플레이 모드 재실행에서 콘솔 오류와 경고는 모두 0건이었습니다.

결과 이미지는 프로젝트 루트의 `Documentation/LobbyUI/preview.png`와 `preview-4by3.png`에 있습니다.

## UI 재사용

Canvas 프리팹에는 EventSystem을 포함하지 않습니다. 다른 씬에 배치할 때 `EventSystem`과 `InputSystemUIInputModule`을 하나만 두세요. 기존 `SteamBootstrap`도 필요합니다.

CanvasScaler는 1920×1080 기준 `Scale With Screen Size / Expand`로 설정되어 있습니다. 16:9와 4:3 화면을 지원합니다. `?` 버튼은 로비 안내 창을 열고, `돌아가기`는 닫습니다.

## 글꼴

[Noto Sans CJK KR](https://github.com/notofonts/noto-cjk)을 사용합니다. 라이선스는 `Fonts/OFL.txt`에 포함되어 있습니다. 고정 UI 문구는 정적 TMP 아틀라스에, Steam 닉네임 등 추가 문자는 동적 대체 글꼴에 표시합니다.
