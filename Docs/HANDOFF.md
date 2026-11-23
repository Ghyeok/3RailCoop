# HANDOFF — 현재 상태

세션이나 컨텍스트가 초기화되어도 작업을 이어가기 위한 스냅샷이다. 매번 덮어쓰며, 이력은 SPEC 결과 필드와 git이 맡는다. 갱신 규칙은 `AGENTS.md`의 'HANDOFF 갱신'을 따른다.

**마지막 갱신:** 2026-11-23

## 1. 현재 목표와 최신 요청

- S-000 완료·승인. 다음 SPEC 항목 착수 전 `후보` 항목 승인 필요.
- 유지되는 요청: 사용자의 명시적인 요청이 모든 규칙보다 우선한다.

## 2. 진행 중인 SPEC 항목

- 없음.

## 3. 완료한 작업과 검증 결과

- 문서 체계 스캐폴드 (D-001~D-005): AGENTS 재구성, SPEC·DECISION·HANDOFF 생성, Game_Design §8·§10 이전, 가이드 3개 삭제. 브랜치 `spec/S-000`에 커밋.
- S-000 테스트 기반 구축: 자동 테스트 2/2 통과, 컴파일 오류 0, 씬 진입은 사용자 확인. 브랜치 `spec/S-000`에 커밋.
- 테스트 실행 방법: 에디터를 열어둔 상태에서 `unity command run_tests --mode all --project-path C:/Unity/3RailCoop`. EditMode 결과는 즉시 반환되고, PlayMode는 비동기로 돌기 때문에 `unity command test_status`로 결과를 확인한다. 에디터가 닫혀 있으면 `unity test`를 쓴다.

## 4. 다음 할 일

1. 사용자가 SPEC '권장 착수 순서'와 `후보` 항목의 완료 기준을 승인한다.
2. 승인된 첫 `대기` 항목을 사용자 지시로 시작한다.

## 5. 열린 질문과 필요한 사용자 응답

- SPEC '권장 착수 순서'를 승인할지.
- S-101을 S-102에 합칠지 (S-101 메모 참고).
- DECISION 미결 사항 O-01~O-09. 특히 O-01·O-04·O-09는 S-201·S-102 착수 전에 필요하다.
- `spec/S-000` 브랜치의 push·main 병합 여부 (요청 시 진행).

## 6. 작업 트리 상태와 임시 작업

- 현재 브랜치: `spec/S-000`.
- 스캐폴드 이전부터 있던 사용자의 미커밋 변경은 그대로 남아 있다: `SteamLobby.cs`, `LobbyPlayer.cs`, `UI_LobbyCanvas.cs`, `UI_Popup.cs`, `UI_Popup_FriendInvite.cs`, `InGameScene.unity`, `ReadmeEditor.cs`, `Maplestory Bold SDF.asset`, `Notion_Writing_Guide.md` 수정. `UI_Base.cs`·`SteamFriendSlot.cs`·`Unity.lnk` 삭제. 장애물·플레이어·카메라 스크립트, `UIBase.cs`, `UI_SteamFriendSlot.cs`, Materials, ObstaclePatterns, `Obstacle.prefab`, `start-claude.bat` 미추적.
- 임시 작업: 없음.
