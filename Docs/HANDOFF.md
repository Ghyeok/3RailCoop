# HANDOFF — 현재 상태

세션이나 컨텍스트가 초기화되어도 작업을 이어가기 위한 스냅샷이다. 매번 덮어쓰며, 이력은 SPEC 결과 필드와 git이 맡는다. 갱신 규칙은 `AGENTS.md`의 'HANDOFF 갱신'을 따른다.

**마지막 갱신:** 2026-11-23

## 1. 현재 목표와 최신 요청

- 목표: MVP 완료 (D-007). 1명의 로컬 플레이어가 Steam 없이 두 플레이어를 조작해 테스트 스테이지를 진행하고 게임 오버·재시도·클리어까지 하는 상태. 판정은 실시간.
- MVP 체크리스트(SPEC): S-101 → S-301 → S-103 → S-203 → S-302 → S-304, 모두 `대기`.
- 유지되는 요청: 사용자의 명시적인 요청이 모든 규칙보다 우선한다.

## 2. 진행 중인 SPEC 항목

- 없음.

## 3. 완료한 작업과 검증 결과

- PR #4 (squash): 문서 체계 스캐폴드(D-001~D-005), S-000 테스트 기반 구축, `com.unity.pipeline` 1.0.1, 병합 절차(D-006), CodeRabbit 자동 리뷰 끔.
- S-000 검증: 열린 에디터와 깨끗한 worktree 모두 EditMode 1/1, PlayMode 1/1 통과. 코드 리뷰(high) 6건 처리.
- PR #5 (squash): 미커밋 기존 작업 커밋(코드 수정 없음). 회귀 2/2 통과. 리뷰 8건은 SPEC 연결 또는 보고만(PR #5 본문). `SteamLobby` 수명 문제는 S-702 완료 기준에 추가.
- PR #6 (squash): Notion 가이드 참고 목록 08~11, `start-claude.bat`, URP 템플릿 Readme(`Assets/TutorialInfo`, `Assets/Readme.asset`) 삭제. 회귀 2/2 통과.
- MVP 재정의(D-007)와 MVP 체크리스트 추가, S-103·S-203·S-304 신설, S-302 MVP용 수정 — 모두 사용자 승인.
- 테스트 실행 방법 (pipeline 1.0.1 기준): 에디터를 열어둔 상태에서 `unity command test run --mode all --async-tests true --project-path C:/Unity/3RailCoop`로 시작하고, `unity command test status`로 결과를 확인한다. 에디터가 닫혀 있거나 다른 경로(worktree)면 `unity test <경로> --mode EditMode|PlayMode`를 쓴다. Editor 명령은 `unity command <group> <name>` 형식이고 매개변수는 kebab-case다. `test status`는 새 실행이 시작되기 전에 조회하면 직전 결과를 돌려주므로, `running`을 확인한 뒤 결과를 읽는다. PlayMode 실행 중에는 Editor 연결이 잠시 끊길 수 있다.
- 주의: worktree를 지울 때 Unity Library 경로가 길어 `git worktree remove`가 실패하면 PowerShell `Remove-Item -LiteralPath '\\?\<경로>' -Recurse -Force` 후 `git worktree prune`.

## 4. 다음 할 일

1. 사용자 지시로 MVP 체크리스트 순서대로 착수한다: S-101 → S-301 → S-103 → S-203 → S-302 → S-304.
2. S-304 착수 시 테스트 스테이지 구성·목표 거리·속도·간격을 사용자와 정한다 (O-06).
3. MVP 이후: S-401 → S-701 → S-201(틱 판정 구조 논의, O-01·O-04·O-09) → …

## 5. 열린 질문과 필요한 사용자 응답

- DECISION 미결 사항 O-01~O-09. MVP에서는 O-01(쿨타임 값)은 Inspector 조절·플레이 테스트로, O-06(테스트 스테이지)은 S-304에서 정한다. O-04·O-09는 MVP 이후 S-201 논의에서 정한다.
- 리뷰 제안: Steamworks.NET 필수 참조로 PC 외 플랫폼 빌드가 불가하다. 다른 플랫폼 지원 시 SPEC 항목 추가.

## 6. 작업 트리 상태와 임시 작업

- 현재 브랜치: `main` (MVP 체크리스트 docs PR 병합 후).
- 미커밋 변경: 없음.
- 임시 작업: 없음.
