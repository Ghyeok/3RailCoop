# HANDOFF — 현재 상태

세션이나 컨텍스트가 초기화되어도 작업을 이어가기 위한 스냅샷이다. 매번 덮어쓰며, 이력은 SPEC 결과 필드와 git이 맡는다. 갱신 규칙은 `AGENTS.md`의 'HANDOFF 갱신'을 따른다.

**마지막 갱신:** 2026-11-23

## 1. 현재 목표와 최신 요청

- S-000과 문서 체계, 병합 절차(D-006)를 PR #4로 main에 병합했다. 다음 SPEC 항목 착수 전에 `후보` 항목 승인이 필요하다.
- 유지되는 요청: 사용자의 명시적인 요청이 모든 규칙보다 우선한다.

## 2. 진행 중인 SPEC 항목

- 없음.

## 3. 완료한 작업과 검증 결과

- PR #4 (squash): 문서 체계 스캐폴드(D-001~D-005), S-000 테스트 기반 구축, `com.unity.pipeline` 1.0.1, 병합 절차(D-006), CodeRabbit 자동 리뷰 끔.
- S-000 검증: 열린 에디터와 깨끗한 worktree 모두 EditMode 1/1, PlayMode 1/1 통과. 코드 리뷰(high) 6건 처리.
- 테스트 실행 방법 (pipeline 1.0.1 기준): 에디터를 열어둔 상태에서 `unity command test run --mode all --async-tests true --project-path C:/Unity/3RailCoop`로 시작하고, `unity command test status`로 결과를 확인한다. 에디터가 닫혀 있거나 다른 경로(worktree)면 `unity test <경로> --mode EditMode|PlayMode`를 쓴다. Editor 명령은 `unity command <group> <name>` 형식이고 매개변수는 kebab-case다.
- 주의: worktree를 지울 때 Unity Library 경로가 길어 `git worktree remove`가 실패하면 PowerShell `Remove-Item -LiteralPath '\\?\<경로>' -Recurse -Force` 후 `git worktree prune`.

## 4. 다음 할 일

1. 사용자가 SPEC '권장 착수 순서'와 `후보` 항목의 완료 기준을 승인한다 (`docs/` 브랜치로 반영).
2. 승인된 첫 `대기` 항목을 사용자 지시로 시작한다.

## 5. 열린 질문과 필요한 사용자 응답

- SPEC '권장 착수 순서'를 승인할지.
- S-101을 S-102에 합칠지 (S-101 메모 참고).
- DECISION 미결 사항 O-01~O-09. 특히 O-01·O-04·O-09는 S-201·S-102 착수 전에 필요하다.
- 기존 구현 검증 항목(S-101, S-301, S-701)의 대상 파일이 사용자의 미커밋 작업이다. 어떤 브랜치로 커밋할지 정해야 한다.
- 리뷰 제안: Steamworks.NET 필수 참조로 PC 외 플랫폼 빌드가 불가하다. 다른 플랫폼 지원 시 SPEC 항목 추가.

## 6. 작업 트리 상태와 임시 작업

- 현재 브랜치: `main` (PR #4 병합 후).
- 스캐폴드 이전부터 있던 사용자의 미커밋 변경은 그대로 남아 있다: `SteamLobby.cs`, `LobbyPlayer.cs`, `UI_LobbyCanvas.cs`, `UI_Popup.cs`, `UI_Popup_FriendInvite.cs`, `InGameScene.unity`, `ReadmeEditor.cs`, `Maplestory Bold SDF.asset`, `Notion_Writing_Guide.md` 수정. `UI_Base.cs`·`SteamFriendSlot.cs`·`Unity.lnk` 삭제. 장애물·플레이어·카메라 스크립트, `UIBase.cs`, `UI_SteamFriendSlot.cs`, Materials, ObstaclePatterns, `Obstacle.prefab`, `start-claude.bat` 미추적.
- 임시 작업: 없음.
