# HANDOFF — 현재 상태

세션이나 컨텍스트가 초기화되어도 작업을 이어가기 위한 스냅샷이다. 매번 덮어쓰며, 이력은 SPEC 결과 필드와 git이 맡는다. 갱신 규칙은 `AGENTS.md`의 'HANDOFF 갱신'을 따른다.

**마지막 갱신:** 2026-11-23

## 1. 현재 목표와 최신 요청

- 목표: MVP 완료 (D-007). 1명의 로컬 플레이어가 Steam 없이 두 플레이어를 조작해 테스트 스테이지를 진행하고 게임 오버·재시도·클리어까지 하는 상태. 판정은 실시간.
- MVP 체크리스트(SPEC): S-101 → S-301 → S-103 → S-203 → S-302 → S-304. S-101(PR #9)·S-301(PR #10) 완료, 나머지는 `대기`. 다음은 S-103.
- 최신 요청: "MVP 체크리스트 전부를 진행해줘." 항목별로 사용자 승인을 받은 뒤 다음 항목을 자동으로 이어간다. S-103·S-203·S-302의 [수동] 확인은 S-304 전체 플레이 때 한꺼번에 한다.
- 사용자가 정한 MVP 값: 이동 쿨타임 기본 0.2초(Inspector 조절), 재시도 키 R, 테스트 스테이지는 난이도 1 핵심 패턴 4개(12→12, 23→23, 1→3→1→3→13→2, 13→2→13→2→13→2, 사이에 빈 사이클, 약 30초). 목표 거리는 마지막 장애물 직후, 속도 5, 사이클 간격 1초.
- 유지되는 요청: 사용자의 명시적인 요청이 모든 규칙보다 우선한다.

## 2. 진행 중인 SPEC 항목

- 없음.
- 참고: Input System이 Game 뷰 포커스를 요구해, 에디터가 뒤에 있으면 `runtime simulate_key` 입력이 무시된다. Git Bash에서 `/Player1` 같은 hierarchy 경로는 `MSYS_NO_PATHCONV=1`로 전달한다.

## 3. 완료한 작업과 검증 결과

- PR #4~#6 (squash): 문서 체계 스캐폴드(D-001~D-006), S-000 테스트 기반, `com.unity.pipeline` 1.0.1, 미커밋 기존 작업 커밋, 관련 없는 파일 삭제. 세부는 SPEC 결과 필드와 PR 본문.
- PR #7·#8: SPEC 착수 순서 승인, MVP 재정의(D-007)와 MVP 체크리스트.
- PR #9: S-101 검증. 시작 레일 Player1 1번·Player2 3번.
- PR #10: S-301. 사이클 해석·검증을 순수 C# `ObstacleCycle`로 분리, 전 레일 차단 거부. EditMode 21/21, PlayMode 3/3.
- 테스트 실행 방법 (pipeline 1.0.1 기준): 에디터를 열어둔 상태에서 `unity command test run --mode all --async-tests true --project-path C:/Unity/3RailCoop`로 시작하고, `unity command test status`로 결과를 확인한다. 에디터가 닫혀 있거나 다른 경로(worktree)면 `unity test <경로> --mode EditMode|PlayMode`를 쓴다. Editor 명령은 `unity command <group> <name>` 형식이고 매개변수는 kebab-case다. `test status`는 새 실행이 시작되기 전에 조회하면 직전 결과를 돌려주므로, `running`을 확인한 뒤 결과를 읽는다. PlayMode 실행 중에는 Editor 연결이 잠시 끊길 수 있다.
- 주의 (병합): `gh pr merge` 뒤 PR 상태가 `MERGED`인지 확인한 다음에 브랜치를 지운다. 병합 전에 원격 브랜치를 지우면 PR이 닫힌다 (PR #9에서 발생, reflog로 복구).
- 주의: worktree를 지울 때 Unity Library 경로가 길어 `git worktree remove`가 실패하면 PowerShell `Remove-Item -LiteralPath '\?\<경로>' -Recurse -Force` 후 `git worktree prune`.
- 주의: Git Bash `sed -i`는 CRLF를 LF로 바꾼다. 레포 문서는 CRLF이므로 수정 후 줄바꿈을 확인한다.

## 4. 다음 할 일

1. S-103 실시간 이동 쿨타임 착수 (기본 0.2초, 순수 C# + EditMode).
2. 이어서 S-203 → S-302 → S-304. S-203에는 전진 속도 통합(플레이어·카메라 속도 한 곳에서 관리)이 포함된다.
3. MVP 이후: S-401 → S-701 → S-201(틱 판정 구조 논의, O-01·O-04·O-09) → …

## 5. 열린 질문과 필요한 사용자 응답

- DECISION 미결 사항 O-01~O-09. MVP에서는 O-01(쿨타임 값)은 Inspector 조절·플레이 테스트로, O-06(테스트 스테이지)은 S-304에서 정한다. O-04·O-09는 MVP 이후 S-201 논의에서 정한다.
- CodeRabbit: 사용자가 GitHub에서 앱을 직접 제거했다 (2026-11-23). `.coderabbit.yaml`과 AGENTS.md의 CodeRabbit 문구는 남아 있으며, 정리 여부는 사용자 결정.
- 리뷰 제안: Steamworks.NET 필수 참조로 PC 외 플랫폼 빌드가 불가하다. 다른 플랫폼 지원 시 SPEC 항목 추가.

## 6. 작업 트리 상태와 임시 작업

- 현재 브랜치: `main` (PR #10 병합 후).
- 미커밋 변경: 없음.
- 임시 작업: 없음.
