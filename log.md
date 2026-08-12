# 프로젝트 로그

## 개요
- Unity + Photon(PUN2) 기반 멀티플레이어 미니게임 모음 프로젝트
- 프로젝트 루트(작업 디렉토리): `E:\Git\1VS1 Game`
- 버전관리: **Plastic SCM 사용 중단, Git/GitHub로 전환**(사용자 결정). git 저장소는 기존부터 초기화되어 있었음(`.gitignore` 있음). 커밋은 사용자가 직접 진행.

## 씬 구성
- LauncherScene: 접속/초기 진입 → Launcher.cs
- LobbyScene: 로비, 방 생성/참가 → LobbyManager.cs
- GameScene: 메인 게임 씬 → GameManager.cs, MiniGameSelector.cs
- MiniGame1: FPS 미니게임 전용 씬
- **MiniGame2: PK(승부차기) 미니게임 전용 씬** (씬 파일 GUID 분석으로 확인됨)
- MiniGame_LavaRiver, TheRift: 아직 스크립트 연결 확인 안 됨(직접 참조 스크립트 없음)

## Assets 폴더 구조 (최근 재정리 완료)
- 최상위: `Asset`(구매 아트), `Scripts`, `Scenes`, `Materials`, `Fonts`, `Settings`, `Resources`, `Photon`, `ParrelSync-master`, `Plugins`, `AI Toolkit`, `TextMesh Pro`, `TutorialInfo` — 원래 이름 그대로 유지(내용 타입별, 사용자 지시로 임의 이름 그룹핑 안 함)
- `Scripts` 내부는 **실제 씬 GUID 참조 분석 결과에 따라** 재정리됨:
  - `Launcher/` Launcher.cs
  - `Lobby/` LobbyManager.cs
  - `GameScene/` GameManager.cs, MiniGameSelector.cs
  - `Common/` AnimatorDebugger, AOSController, CameraFollow, PlayerController, PlayerSetup, MiniGameManager (여러 씬 공용 플레이어 프리팹 구성요소)
  - `FPS MINIGAME/` 기존 유지
  - `PK MINIGAME/` 기존 유지 (아래 참고)
  - `Unused/` AOSCameraFollow, AnimatorFixer, ChainProjectile, DeathZone — 어떤 씬에서도 직접 참조 미확인(미배치 추정, 추후 확인 필요)

## PK(승부차기) 미니게임 — 이번 세션에 스펙대로 전면 재구현
대상 씬: MiniGame2. 스크립트: `Assets/Scripts/PK MINIGAME/`
- `PKKicker.cs`: 마우스로 곡선 궤적 직접 그리기 → 확정 후 파워 게이지 단계, 10초 제한(초과 시 약한 기본 슛)
- `PKPowerGauge.cs`: 클릭으로 게이지 정지, Perfect 존과의 거리로 정확도(오차량) 산출
- `PKTrajectoryUtil.cs`(신규): 정확도 → 원본 궤적에 오차 적용해 실제 궤적 생성
- `PKBall.cs`: 물리(AddForce) 대신 궤적 경로 추종(path-follow) 방식으로 이동
- `PKGoalkeeper.cs`: 골대 1~6구역 선택 방식, `GoalkeeperDive1~6` 트리거 호출, 10초 제한(초과 시 랜덤 구역)
- `PKGoalkeeperCursor.cs`: 골키퍼 화면에만 보이는 반투명 1~6구역 UI(런타임 자동 생성)
- `PKGoalDetector.cs`: 물리 충돌 대신 실제 궤적 종착점 구역 vs 골키퍼 선택 구역 비교로 판정
- `PKGoalZones.cs`(신규): 골대 2행3열 구역 좌표 계산 유틸
- `PKGameManager.cs`: 카메라를 역할 무관 고정 시점으로 통일(공격자 뒤에서 골대 보는 3인칭), 구역 좌표 중앙화. **카메라 위치/회전을 인스펙터 필드(`Camera Position`/`Camera Euler Angles`)로 노출**해 코드 수정 없이 튜닝 가능하게 함. 라운드/서든데스 규칙은 기존 로직 유지(이미 요구사항과 일치).
- 사용자가 Unity AI(에디터 내장 AI 어시스턴트)로 이 파일을 직접 보강한 흔적 있음(null-safety, `GetActiveKicker`/`GetDefendingGoalkeeper` 강화, 매 턴 전체 키커 위치 갱신, `CameraFollow` 컴포넌트 명시적 비활성화) — 그대로 유지하며 작업함.

## 아직 Unity 에디터에서 사용자가 직접 해야 하는 작업
1. 골키퍼 Animator Controller에 `GoalkeeperDive1`~`GoalkeeperDive6` 트리거 6개 추가 + 다이빙 클립 연결
2. `PKGoalkeeperCursor`가 Canvas 자식으로 배치되어 있는지 확인
3. 각 PK 스크립트 인스펙터 레퍼런스 연결 확인(특히 `PKUIManager.timerText`는 이번에 추가된 신규 필드라 UI 요소를 새로 만들어 연결해야 함)
4. MiniGame2 씬에서 2인 실제 플레이 테스트, 카메라/오차감/타이머 체감 튜닝
5. GitHub 저장소 생성 후 push (사용자가 직접 진행 예정)

## 다음 세션 참고
- 세션 시작 시 이 파일을 먼저 읽고 구조/진행상황 파악할 것
- SAVE 명령 시 이 파일 내용을 갱신하고, 이전 내용은 날짜와 함께 changelog.md로 이관
