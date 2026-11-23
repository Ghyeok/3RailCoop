# HANDOFF — 현재 상태

세션이나 컨텍스트가 초기화되어도 작업을 이어가기 위한 스냅샷이다. 매번 덮어쓰며, 이력은 SPEC 결과 필드와 git이 맡는다. 갱신 규칙은 `AGENTS.md`의 'HANDOFF 갱신'을 따른다.

**마지막 갱신:** 2026-11-23

## 1. 현재 목표와 최신 요청

- S-000과 문서 체계, 병합 절차(D-006)를 PR #4로, 미커밋이던 기존 작업을 PR #5로, 관련 없는 Notion 링크·런처·템플릿 Readme 정리를 PR #6으로 main에 병합했다. SPEC 착수 순서를 승인받았고, 기존 구현 검증 S-101·S-301·S-401·S-701이 `대기`다. S-201 이후는 틱 판정 구조 논의 전까지 보류.
- 유지되는 요청: 사용자의 명시적인 요청이 모든 규칙보다 우선한다.

## 2. 진행 중인 SPEC 항목

- 없음.

## 3. 완료한 작업과 검증 결과

- PR #4 (squash): 문서 체계 스캐폴드(D-001~D-005), S-000 테스트 기반 구축, `com.unity.pipeline` 1.0.1, 병합 절차(D-006), CodeRabbit 자동 리뷰 끔.
- S-000 검증: 열린 에디터와 깨끗한 worktree 모두 EditMode 1/1, PlayMode 1/1 통과. 코드 리뷰(high) 6건 처리.
- PR #5 (squash): 미커밋 기존 작업 커밋(코드 수정 없음). 회귀 2/2 통과. 리뷰 8건은 SPEC 연결 또는 보고만(PR #5 본문). `SteamLobby` 수명 문제는 S-702 완료 기준에 추가.
- PR #6 (squash): Notion 가이드 참고 목록 08~11, `start-claude.bat`, URP 템플릿 Readme(`Assets/TutorialInfo`, `Assets/Readme.asset`) 삭제. 회귀 2/2 통과.
- 테스트 실행 방법 (pipeline 1.0.1 기준): 에디터를 열어둔 상태에서 `unity command test run --mode all --async-tests true --project-path C:/Unity/3RailCoop`로 시작하고, `unity command test status`로 결과를 확인한다. 에디터가 닫혀 있거나 다른 경로(worktree)면 `unity test <경로> --mode EditMode|PlayMode`를 쓴다. Editor 명령은 `unity command <group> <name>` 형식이고 매개변수는 kebab-case다. `test status`는 새 실행이 시작되기 전에 조회하면 직전 결과를 돌려주므로, `running`을 확인한 뒤 결과를 읽는다. PlayMode 실행 중에는 Editor 연결이 잠시 끊길 수 있다.
- 주의: worktree를 지울 때 Unity Library 경로가 길어 `git worktree remove`가 실패하면 PowerShell `Remove-Item -LiteralPath '\\?\<경로>' -Recurse -Force` 후 `git worktree prune`.

## 4. 다음 할 일

1. 사용자 지시로 S-101부터 순서대로 착수한다 (S-101 → S-301 → S-401 → S-701). S-401·S-701은 두 PC·두 Steam 계정이 필요한 [수동] 검증이다.
2. S-201 틱 판정 구조(O-01·O-04·O-09)를 사용자와 논의해 정한다. 그 뒤 S-201~S-302 등 보류 항목의 완료 기준을 다시 승인받는다.

## 5. 열린 질문과 필요한 사용자 응답

- DECISION 미결 사항 O-01~O-09. 특히 O-01·O-04·O-09는 S-201 논의에서 정해야 한다 (사용자: 많은 논의 필요).
- 리뷰 제안: Steamworks.NET 필수 참조로 PC 외 플랫폼 빌드가 불가하다. 다른 플랫폼 지원 시 SPEC 항목 추가.

## 6. 작업 트리 상태와 임시 작업

- 현재 브랜치: `main` (SPEC 승인 docs PR 병합 후).
- 미커밋 변경: 없음.
- 임시 작업: 없음.
