# HANDOFF — 현재 상태

세션이나 컨텍스트가 초기화되어도 작업을 이어가기 위한 스냅샷이다. 매번 덮어쓰며, 이력은 SPEC 결과 필드와 git이 맡는다. 갱신 규칙은 `AGENTS.md`의 'HANDOFF 갱신'을 따른다.

**마지막 갱신:** 2026-11-25

## 1. 현재 목표와 최신 요청

- 목표: MVP 완료 (D-007) 뒤 로컬 플레이 다듬기 우선(D-011). Steam·2PC 항목은 환경이 준비되면 `낮음` 묶음으로 진행.
- MVP 체크리스트(SPEC): S-101(PR #9)·S-301(PR #10)·S-103(PR #11)·S-203(PR #12)·S-302(PR #13)·S-304(PR #14) 모두 완료. **MVP 완료 (2026-11-23).** 사용자 전체 플레이 8항목 통과.
- 최신 요청: S-401 [수동] 검증을 미루고 SPEC 우선순위를 나눔(grilling) → D-011(PR #17): 높음 S-502 → S-602, 보통 S-201 → S-102 → S-202, 낮음 S-401 [수동] → S-403 → S-701 → S-402 → S-501 → S-601 → S-702 → S-703 → S-704. 새 상태 `수동 대기`. S-401 자동 부분 병합.
- 사용자가 정한 MVP 값: 이동 쿨타임 기본 0.2초(Inspector 조절), 재시도 키 R, 테스트 스테이지는 난이도 1 핵심 패턴 4개(12→12, 23→23, 1→3→1→3→13→2, 13→2→13→2→13→2, 사이에 빈 사이클, 약 30초). 목표 거리는 마지막 장애물 직후, 속도 5, 사이클 간격 1초.
- 유지되는 요청: 사용자의 명시적인 요청이 모든 규칙보다 우선한다.

## 2. 진행 중인 SPEC 항목

- 없음.
- `수동 대기`: S-401. 자동 부분 PR #18로 병합. 사용자가 2PC에서 SPEC [수동] 절차(기본 8단계, 시간 초과 3단계)를 수행하면 결과를 반영해 보고(`보고됨`). 빌드 실행 방법은 SPEC S-401 테스트 항목과 아래 6절.
- 참고: Input System이 Game 뷰 포커스를 요구해, 에디터가 뒤에 있으면 `runtime simulate_key` 입력이 무시된다. Git Bash에서 `/Player1` 같은 hierarchy 경로는 `MSYS_NO_PATHCONV=1`로 전달한다.

## 3. 완료한 작업과 검증 결과

- PR #4~#6 (squash): 문서 체계 스캐폴드(D-001~D-006), S-000 테스트 기반, `com.unity.pipeline` 1.0.1, 미커밋 기존 작업 커밋, 관련 없는 파일 삭제. 세부는 SPEC 결과 필드와 PR 본문.
- PR #7·#8: SPEC 착수 순서 승인, MVP 재정의(D-007)와 MVP 체크리스트.
- PR #9: S-101 검증. 시작 레일 Player1 1번·Player2 3번.
- PR #10: S-301. 사이클 해석·검증을 순수 C# `ObstacleCycle`로 분리, 전 레일 차단 거부. EditMode 21/21, PlayMode 3/3.
- PR #11: S-103. 레일 위치·이동 쿨타임을 순수 C# `LaneMovement`로 분리, `PlayerController.moveCooldownSeconds` 기본 0.2초. EditMode 30/30, PlayMode 3/3. 조작감 [수동] 확인은 S-304에서.
- PR #12: S-203. 합체·분리 순수 C# `CoopLanes`(레일 1~3), 씬에 하나인 `CoopController`가 두 플레이어 입력·전진 속도·위치를 맡음(`PlayerController` 삭제). 카메라는 `forwardSource`로 같은 속도. EditMode 46/46, PlayMode 6/6. 가정: 합체 프레임 분리 입력·분리 프레임 이동 입력은 버림(O-09), 한 기기 Space는 두 플레이어 분리 입력.
- PR #13: S-302. 상태 전이 순수 C# `StageRun`, `StageController`(충돌 게임 오버, 실제 전진 거리로 클리어, R 재시도, uGUI+TMP 영문 메시지). 충돌은 플레이어 트리거·kinematic Rigidbody·`PlayerHitbox`와 프리팹 `Obstacle` 표시. 목표 거리 기본 100. EditMode 57/57, PlayMode 12/12.
- PR #14: S-304. 테스트 스테이지 D-008(난이도 1 핵심 패턴 4개 고정 순서, 사이 빈 사이클 3개, 28사이클, 속도 5, 간격 1초, 목표 거리 155), 임시 레일 z=-5~185, 패턴 에셋 유효성 EditMode 테스트. EditMode 63/63, PlayMode 12/12. S-303(설정 오류 시 빈 스테이지 진행 방지) `후보` 추가.
- PR #15: S-303. 사용자가 기준을 '스테이지 미시작'에서 '유효한 패턴으로 무작위 대체'로 변경(D-009). 순수 C# `ObstaclePatternResolver`, `ObstacleSpawner`가 시도마다 대체·오류 로그, 대체 불가·프리팹/전진 기준 누락 시 로그 후 장애물 없이 진행. EditMode 70/70, PlayMode 18/18.
- PR #16: D-010(Steam 미실행 시 Create Room에서 자동 실행·30초 대기·자동 진행, 로비 실패 영문 오류 팝업), S-403 `대기` 추가.
- PR #17: D-011 우선순위 3단계·`수동 대기` 상태(AGENTS.md 반영), S-501·S-601을 동기화만 남기고 로컬 부분 S-502·S-602(`후보`) 분리.
- PR #18: S-401 자동 부분. EditMode `LobbySceneSetupTests` 3개, 회귀 91/91(EditMode 73, PlayMode 18). 상태 `수동 대기`.
- PR #19: D-012(스테이지를 패턴 개수 N과 난이도별 비율로 무작위 구성, 고정 순서 모드 병행, 난이도 1·2 패턴 에셋), S-502 `대기`.
- 주의: Play 모드에서 TMP 텍스트가 렌더되면 `Assets/Maplestory Bold SDF.asset`(동적 아틀라스)이 바뀐다. 작업과 무관하면 커밋 전에 되돌린다.
- 테스트 실행 방법 (pipeline 1.0.1 기준): 에디터를 열어둔 상태에서 `unity command test run --mode all --async-tests true --project-path C:/Unity/3RailCoop`로 시작하고, `unity command test status`로 결과를 확인한다. 에디터가 닫혀 있거나 다른 경로(worktree)면 `unity test <경로> --mode EditMode|PlayMode`를 쓴다. Editor 명령은 `unity command <group> <name>` 형식이고 매개변수는 kebab-case다. `test status`는 새 실행이 시작되기 전에 조회하면 직전 결과를 돌려주므로, `running`을 확인한 뒤 결과를 읽는다. PlayMode 실행 중에는 Editor 연결이 잠시 끊길 수 있다. PlayMode 실행이 끝나도 `test status`가 `running`에 머물 수 있다. 이때는 Console의 `[TestResultCollector] Run finished` 로그와 `C:/Users/gulww/AppData/LocalLow/DefaultCompany/3RailCoop/TestResults.xml`로 결과를 확인하고 `test cancel`로 정리한다. 컴파일 오류가 있으면 실행이 시작되지 않고 `running`으로 남으므로 `console status`의 `compilationFailed`를 먼저 확인한다.
- 주의 (병합): `gh pr merge` 뒤 PR 상태가 `MERGED`인지 확인한 다음에 브랜치를 지운다. 병합 전에 원격 브랜치를 지우면 PR이 닫힌다 (PR #9에서 발생, reflog로 복구).
- 주의: worktree를 지울 때 Unity Library 경로가 길어 `git worktree remove`가 실패하면 PowerShell `Remove-Item -LiteralPath '\?\<경로>' -Recurse -Force` 후 `git worktree prune`.
- 주의: Git Bash `sed -i`는 CRLF를 LF로 바꾼다. 레포 문서는 CRLF이므로 수정 후 줄바꿈을 확인한다.
- 주의: 사용자가 IDE를 열어 둔 채 작업하므로, IDE 버퍼의 이전 내용이 저장되어 AI 수정이 덮일 수 있다(2026-11-25 발생). 테스트 전 `git diff`로 파일 상태를 확인한다.

## 4. 다음 할 일

1. S-502(난이도별 무작위 패턴 선택, 로컬)는 D-012로 완료 기준을 승인받아 `대기`. 사용자가 착수를 지시하면 `spec/S-502`에서 시작한다.
2. 그다음 S-602(사망·암살 기록 로컬, `후보`): O-02·O-05와 실시간 판정에서의 암살 기준을 정해야 한다.
3. S-401 수동 결과가 오면 반영. S-403(D-010, `대기`)은 `낮음` 묶음에서 S-401 뒤.

## 5. 열린 질문과 필요한 사용자 응답

- DECISION 미결 사항 O-01~O-09. O-06은 D-008로 결정. O-01(쿨타임 최종값)은 MVP 기본 0.2초로 두고 플레이 테스트로 정한다. O-04·O-09는 MVP 이후 S-201 논의에서 정한다.
- CodeRabbit: 사용자가 GitHub에서 앱을 직접 제거했다 (2026-11-23). `.coderabbit.yaml`과 AGENTS.md의 CodeRabbit 문구는 남아 있으며, 정리 여부는 사용자 결정.
- 리뷰 제안: Steamworks.NET 필수 참조로 PC 외 플랫폼 빌드가 불가하다. 다른 플랫폼 지원 시 SPEC 항목 추가.

## 6. 작업 트리 상태와 임시 작업

- 현재 브랜치: `main` (S-401 자동 부분 병합 후).
- 미커밋 변경: 없음.
- 주의: 빌드하면 `ProjectSettings/ProjectSettings.asset`의 `preloadedAssets`에 Input System 액션 에셋이 남는다. 작업과 무관하므로 되돌린다.
- 임시 작업: `Build/S-401/Timeout` 빌드에 시간 초과 검증용 실행 인자(`-s401CreateTimeout`·`-s401JoinTimeout`·`-s401NgoTimeout`) 코드가 들어 있다. 소스(`SteamLobby.cs`)는 빌드 직후 되돌림(확인함). 소멸 조건: S-401 [수동] 검증이 끝나면 `Build/S-401` 폴더 삭제(gitignore 대상). 수동 검증 전에 S-401 대상 코드가 바뀌면 빌드를 다시 만든다.
