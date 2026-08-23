# 프로젝트 로그

## 개요
- Unity + Photon(PUN2) 기반 멀티플레이어 미니게임 모음 프로젝트
- 프로젝트 루트(작업 디렉토리): `E:\Git\1VS1 Game`
- 버전관리: Git/GitHub 사용 (`github.com/ARHIENE/1VS1-Game`). Plastic SCM은 사용 중단. 커밋은 주로 사용자가 GitHub Desktop으로 직접 진행(요청 시 Claude가 CLI로 커밋/push하기도 함)

## Git 브랜치 전략
- `master`: 실제 작업 브랜치. 프로젝트 전체 파일 포함, **README.md 없음**
- `main`: GitHub 기본 브랜치. **README.md만 관리**(프로젝트 전체 파일 없음), 저장소 메인 페이지 노출용
- 두 브랜치는 공통 조상이 없는 별개 히스토리
- **SAVE 명령 시 main의 README.md도 그날 작업 반영해 최신화할 것** (전역 CLAUDE.md 규칙)

## 씬 구성
- LauncherScene: 접속/초기 진입 → Launcher.cs
- LobbyScene: 로비, 방 생성/참가 → LobbyManager.cs
- GameScene: 메인 게임 씬 → GameManager.cs, MiniGameSelector.cs
- MiniGame1: FPS 미니게임 전용 씬
- MiniGame_PK: PK(승부차기) 미니게임 전용 씬 (구 `MiniGame2.unity`, 2026-08-14 사용자가 네이밍 일관성 위해 리네임)
- MiniGame_LavaRiver: LoL류 AOS 스타일 1대1 미니게임. 전용 스크립트 폴더는 없지만 `Common/AOSController.cs`(이동/스킬/블링크/체력/사망) + `Unused/ChainProjectile.cs`(Q 스킬 투사체, Fireball 프리팹에 부착) + `Unused/AOSCameraFollow.cs`(런타임 부착 카메라)가 `PlayerSetup.cs`의 씬 이름 분기로 활성화되어 실질적으로 상당 부분 구현되어 있음(2026-08-14 유니티AI 답변 + GUID 참조 검색으로 재확인, 예전 "스크립트 연결 미확인" 기재는 오기재였음). 다만 같은 폴더의 `DeathZone.cs`(용암 사망 존 처리용으로 보임)는 실제로 어떤 씬/프리팹에도 부착되어 있지 않아 미사용 — 용암에 빠져도 별도 처리 없음. 자세한 조작법/구현은 `feature/lavariver-minigame` 브랜치 README.md 참고
- MiniGame_Gomoku3D: 3D 오목 미니게임 전용 씬 (2026-08-14 사용자가 신규 생성, 컴포넌트 배치는 진행 상황 확인 필요)
- MiniGame_PasaboloDuel: 파사볼로 대결(슬링샷 거리 경쟁) 미니게임 전용 씬 (2026-08-23 신규 생성, feature/gomoku3d-minigame 브랜치 working tree에서 사용자가 Unity AI로 직접 씬 생성/컴포넌트 배치 진행 — 실제 완료 상태는 다음 세션에서 확인 필요)
- ~~TheRift~~: 2026-08-14 사용자가 삭제(스크립트 연결 없어서 정리)

## Assets/Scripts 폴더 구조
씬 GUID 참조 분석 기반으로 정리됨 (원래 이름 유지, 임의 그룹핑 안 함)
- `Launcher/` Launcher.cs
- `Lobby/` LobbyManager.cs
- `GameScene/` GameManager.cs, MiniGameSelector.cs
- `Common/` AnimatorDebugger, AOSController, CameraFollow, PlayerController, PlayerSetup, MiniGameManager (여러 씬 공용 컴포넌트)
- `FPS MINIGAME/` FPS 전용
- `PK MINIGAME/` PK 전용 (아래 참고)
- `GOMOKU MINIGAME/` 3D 오목 전용 (아래 참고)
- `PASABOLO MINIGAME/` 파사볼로 대결 전용 (아래 참고, 2026-08-23 신규)
- `Unused/` AOSCameraFollow, AnimatorFixer, ChainProjectile, DeathZone — 폴더명과 달리 `AOSCameraFollow`/`ChainProjectile`은 LavaRiver 미니게임에서 실제 사용 중(위 참고), `AnimatorFixer`/`DeathZone`만 실제 미사용으로 확인됨(2026-08-14 재확인)

## PK(승부차기) 미니게임 — 스펙대로 전면 재구현 완료
대상 씬: MiniGame_PK. 스크립트: `Assets/Scripts/PK MINIGAME/`
- `PKKicker.cs`: 마우스로 곡선 궤적 직접 그리기 → 확정 후 파워 게이지 단계, 10초 제한(초과 시 약한 기본 슛)
- `PKPowerGauge.cs`: 클릭으로 게이지 정지, Perfect 존과의 거리로 정확도(오차량) 산출
- `PKTrajectoryUtil.cs`: 정확도 → 원본 궤적에 오차 적용해 실제 궤적 생성
- `PKBall.cs`: 물리(AddForce) 대신 궤적 경로 추종(path-follow) 방식으로 이동
- `PKGoalkeeper.cs`: 골대 1~6구역 선택 방식, `GoalkeeperDive1~6` 트리거 호출, 10초 제한(초과 시 랜덤 구역)
- `PKGoalkeeperCursor.cs`: 골키퍼 화면에만 보이는 반투명 1~6구역 UI(런타임 자동 생성)
- `PKGoalDetector.cs`: 물리 충돌 대신 실제 궤적 종착점 구역 vs 골키퍼 선택 구역 비교로 판정
- `PKGoalZones.cs`: 골대 2행3열 구역 좌표 계산 유틸
- `PKGameManager.cs`: 카메라를 역할 무관 고정 시점으로 통일, 구역 좌표 중앙화, 카메라 위치/회전 인스펙터 노출
- 사용자가 Unity AI로 null-safety 등 직접 보강한 부분 유지

## FPS 미니게임
- 대상 씬: MiniGame1. 스크립트: `Assets/Scripts/FPS MINIGAME/`
- 1대1 총격전, 라운드제(3선승, 최대 5라운드)

## 3D 오목(입체 오목) 미니게임 — 스크립트 작성 완료, 씬 생성됨(컴포넌트 배치 확인 필요)
대상 씬: `MiniGame_Gomoku3D`(사용자가 생성 완료). 스크립트: `Assets/Scripts/GOMOKU MINIGAME/` (7개). 상세 구현 히스토리(버그 수정 과정 등)는 `changelog.md`의 2026-08-13~14 항목 참고.

- 규칙: 15x15x15 그리드(3375칸, 표준 오목판 15x15를 정육면체로 확장), 커넥트4처럼 (x,y) 교차점에 중력 배치 → z는 자동 결정(최대 15층), 13방향(축3+평면대각6+입체대각4) 검사로 5목 승리, 무승부는 보드가 가득 찼을 때
- `GomokuGrid.cs`: 순수 C# 클래스(MonoBehaviour 아님). 보드 데이터/배치/조회/초기화. `GomokuGrid.Size = 15`를 다른 스크립트가 전부 참조하므로 크기 값만 바꾸면 전체 반영됨
- `GomokuWinChecker.cs`: static 유틸(PK의 `PKGoalZones`/`PKTrajectoryUtil`과 동일 컨벤션). 13방향 승리 판정 + 승리 라인 좌표 반환
- `GomokuTurnManager.cs`: `MonoBehaviourPun` 싱글턴(`Instance`). 아래 세 가지를 전부 같은 PhotonView RPC로 동기화:
  - 돌 배치: `RequestPlace`→`RPC_PlaceStone` (결정론적 로직이라 좌표+플레이어 번호만 전송)
  - 상대 아바타 머리 회전: `ReportLocalHeadLook`→`RPC_UpdateHeadLook`(초당 약 20회 스로틀)→`OnOpponentHeadLook` 이벤트로 전달
  - 보드 90도 회전(Q/E): `RequestRotateBoard`→`RPC_RotateBoard`. **현재 턴인 로컬 플레이어만 가능**(`IsLocalTurn()` 재사용, 게임 종료 시 자동 차단), 턴을 소모하지 않는 자유 행동
  - `PhotonNetwork.IsConnected` 아니면 로컬 핫싯 모드로 폴백. `ReturnToHub()` 제공. `LocalPlayer`(1/2)는 다른 스크립트가 `Start()`에서 안전 참조 가능하도록 `Awake()`에서 확정
- `GomokuStoneSpawner.cs`: 그리드↔월드 좌표 변환, 나무색 베이스 플레인 + 15x15 격자선(LineRenderer) + 돌(기본 Sphere, `stonePrefab` 비우면 자동 생성) 전부 런타임 자동 생성. 보드 전체(플레인+격자선+돌)가 **보드 중심에 피벗을 둔 `boardRoot`** 자식으로 묶여 있어 `SetBoardRotation()`으로 통째로 부드럽게(Slerp, 기본 0.35초) 90도씩 회전 가능. `GridToWorld()`가 `boardRoot.TransformPoint()` 기반이라 회전 중에도 항상 정확한 위치 반환(다른 스크립트는 이 함수를 통해 자동으로 회전 반영받음)
- `GomokuInputHandler.cs`: 바둑판 지면과의 평면 수식 교차(`Plane.Raycast`) + `boardRoot` 역변환으로 (x,y) 클릭 판정 — 카메라 각도/보드 회전 각도와 무관하게 항상 정확. 선택 가능 교차점만 마커 표시(마커는 매 프레임 해당 기둥의 현재 쌓인 높이 위로 따라 올라감), Q/E 키다운 감지(턴 검증은 TurnManager가 담당)
- `GomokuVisualHighlighter.cs`: 승리 라인 돌 색상 변경 + 펄스 애니메이션
- `GomokuSeatedCamera.cs`: 1인칭 착석 시점(궤도 회전 아님). `GomokuTurnManager.LocalPlayer`를 보고 보드를 사이에 둔 두 좌석 중 내 자리에 배치, 우클릭 드래그로 pitch(내리면 바둑판/올리면 상대 얼굴, 제한된 범위)와 제한된 yaw만 조절. 눈높이/좌석거리는 보드 전체 폭 비율(`eyeHeightRatio 0.6155`, `seatSetbackRatio 0.375`)로 계산 — 보드 크기가 또 바뀌어도 자동으로 맞춰짐. 양쪽 좌석에 아바타(캡슐+구체, `avatarPrefab` 비우면 자동 생성) 로컬 생성, 내 아바타는 렌더러만 꺼서 안 보이게 하고 머리 회전만 RPC로 상대에게 전파
- Common의 `MiniGameManager.cs`(플레이어 아바타 스폰 전제)는 재사용하지 않음 — 보드 게임 특성상 물리 아바타가 필요 없어 `GomokuTurnManager`가 자체적으로 턴/종료 흐름을 관리

### 아직 사용자가 직접 해야 하는 작업 (3D 오목)
1. ~~`Assets/Scenes/MiniGame_Gomoku3D.unity` 씬 생성~~ — 완료(2026-08-14)
2. 빈 GameObject에 `GomokuTurnManager` 부착 + **PhotonView 컴포넌트 추가**(Scene 소유로 설정) — `photonView.RPC` 호출에 필요
3. 빈 GameObject에 `GomokuStoneSpawner`, `GomokuVisualHighlighter`, `GomokuInputHandler` 각각 부착(같은 오브젝트에 몰아도 무방). 서로 참조 필드는 비워두면 `FindAnyObjectByType`로 자동 탐색됨
4. Main Camera에 `GomokuSeatedCamera` 부착(`spawner` 비워두면 자동 탐색되므로 좌표 수동 계산 불필요)
5. `GomokuInputHandler.targetCamera` 연결(비워두면 `Camera.main` 자동 사용)
6. (선택) 턴 안내/결과 UI(TMP 텍스트, 결과 패널) 제작 후 `GomokuTurnManager`의 `turnText`/`resultPanel`/`resultText` 필드에 연결 — 비워두면 UI 없이도 로직은 정상 동작
7. "허브로 나가기" 버튼 제작 후 `GomokuTurnManager.ReturnToHub()`를 OnClick에 연결(PK처럼 Esc 일시정지 UI는 미포함 상태)
8. `GameScene`의 미니게임 선택 UI에 `MiniGameSelector.LoadMiniGame("MiniGame_Gomoku3D")` 호출 버튼 추가
9. 돌 프리팹(`stonePrefab`)/바둑판 재질(`boardMaterial`)/좌석 아바타(`GomokuSeatedCamera.avatarPrefab`)를 커스텀으로 쓰고 싶으면 각각 연결(전부 비워두면 자동 생성). `Assets/Materials/Gomoku/`에 사용자가 재질 작업 중인 것으로 보임(2026-08-14 확인)
10. MiniGame_PK 사례처럼 ParrelSync로 로컬 2클라이언트 접속해 실제 2인 플레이 테스트

## 파사볼로 대결(Pasabolo Duel) 미니게임 — 스크립트 작성 완료, 전용 브랜치 push 완료
대상 씬: `MiniGame_PasaboloDuel`. 스크립트: `Assets/Scripts/PASABOLO MINIGAME/` (7개). 상세 구현/브랜치 처리 과정은 `changelog.md`의 2026-08-23 항목 참고.

- 규칙: 턴제(골프형), 한 턴 = 공 1회 발사로 핀 3개 타격, 핀 이동 거리를 rayas(득점선) 가중치로 채점해 누적. 기본 3턴/플레이어(인스펙터 조정 가능), 합계 높은 쪽 승리
- **이 프로젝트 최초로 실시간 Rigidbody 물리를 Photon으로 동기화** — 오목/PK는 결정론적 RPC 또는 결과-재생 방식만 썼음. 마스터 클라이언트만 실제 `AddForce`/충돌 시뮬레이션 실행(`PasaboloPhysicsController`), 비마스터는 Rigidbody `isKinematic` 전환 후 `PhotonTransformView`로 위치만 수신
- `PasaboloTurnManager.cs`: `MonoBehaviourPun` 싱글턴(`Instance`). `RequestLaunch()`→`RPC_Launch`(RpcTarget.All — 카메라 전환은 양쪽 다 필요, 실제 물리 적용은 마스터/오프라인만), `PhysicsController`가 정지 감지 후 `ReportTurnResult()`→`RPC_TurnResult`(점수 반영·핀 위치 하드 스냅·턴 전환·승패 판정). `OnShotFired`/`OnShotSettled` 이벤트를 카메라가 구독
- `PasaboloPhysicsController.cs`: 마스터(또는 오프라인 핫싯) 권위. 정지 판정은 속도 임계값이 일정 시간 유지되는지로 확인, rayas 채점은 `PasaboloRayasScorer`(static 유틸) 재사용
- `PasaboloTableSpawner.cs`: 테이블/rayas 득점선은 완전 자동 생성(비네트워크 시각 요소). 공/핀은 PhotonView가 씬 저장 시점에 필요해 런타임 자동 생성이 불가능 — 씬에 미리 배치된 Transform을 받아 외형/Rigidbody/Collider가 없을 때만 채워 넣는 방식
- `PasaboloLaunchController.cs`: 슬링샷 드래그 입력(레이-평면 교차, PKKicker 기법 재사용). `PasaboloCameraController.cs`: Aim(조준)→Flight(비행 추적)→Result(전체 조망) 3단계, Cinemachine 미사용 확인 후 손수 Lerp/Slerp
- `PasaboloPowerGaugeUI.cs`: 프리팹 없이 런타임 Canvas/Slider 자동 생성(수동 UI 작업 최소화)

### 아직 사용자가 직접 해야 하는 작업 (파사볼로 대결)
1. `feature/gomoku3d-minigame` 브랜치 working tree에서 사용자가 Unity AI로 씬 생성/컴포넌트 배치/GameScene 허브 버튼 연결까지 진행한 상태(2026-08-23) — **실제 완료 범위(PhotonView/PhotonTransformView 부착 여부 등)는 다음 세션에서 확인 필요**, 아직 커밋되지 않은 uncommitted 상태로 남아 있음
2. `feature/pasabolo-duel-minigame`(master에서 깨끗하게 분기, origin push 완료)에는 스크립트/씬/빌드설정/RPC목록만 반영돼 있고 **GameScene 허브 버튼은 미연결** — 이 브랜치에서 별도로 버튼을 다시 추가해야 함(자세한 이유는 changelog.md 2026-08-23 참고)
3. 오프라인 핫싯 모드로 턴/점수 로직 우선 검증 → ParrelSync 2클라이언트로 실제 물리 동기화(마스터-비마스터 간 핀 위치 드리프트 체감) 테스트
4. `PasaboloRayasScorer`의 `rayaDistances`/`rayaWeights` 밸런스 튜닝(마지막 턴 역전 가능하도록)

## README.md 구성 (main 브랜치)
개발 환경 / 씬 구성 / 프로젝트 구조 / 미니게임(FPS·PK·LavaRiver·Gomoku3D) / 스크린샷(자리만, 이미지 추가 예정) / 향후 계획 / 버전 관리

## 아직 사용자가 직접 해야 하는 작업 (PK)
1. 골키퍼 Animator Controller에 `GoalkeeperDive1`~`GoalkeeperDive6` 트리거 6개 추가 + 다이빙 클립 연결
2. `PKGoalkeeperCursor`가 Canvas 자식으로 배치되어 있는지 확인
3. 각 PK 스크립트 인스펙터 레퍼런스 연결 확인(특히 `PKUIManager.timerText`는 신규 필드라 UI 요소를 새로 만들어 연결해야 함)
4. MiniGame_PK 씬에서 2인 실제 플레이 테스트, 카메라/오차감/타이머 체감 튜닝
5. README.md 스크린샷/이미지 추가

## Git 저장소 공개 여부
- **Public 전환 보류 중** (2026-08-13 결정)
- 사유: `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`에 실제 Photon App ID(`AppIdRealtime`)가 하드코딩되어 커밋 히스토리에 존재 → 그대로 노출 시 무단 사용/쿼터 소진 위험
- 재요청 시 먼저 확인할 것: (1) Photon Dashboard에서 App ID 재발급 (2) 새 값은 커밋 제외 처리 (3) 기존 히스토리에서 값 제거(git filter-repo 등) 여부

## 미니게임별 feature 브랜치
- master에서 개별 분기(형제 관계, 계층 아님), 완료 후 각각 master로 병합하는 방식으로 운영하기로 함
- 5개 전부 origin push 완료(아직 master로 병합 전): `feature/fps-minigame`, `feature/pk-minigame`, `feature/lavariver-minigame`, `feature/gomoku3d-minigame`(현재 브랜치), `feature/pasabolo-duel-minigame`(2026-08-23 신규, master에서 `git worktree`로 깨끗하게 분기)
- **2026-08-14: 4개 브랜치(fps/pk/lavariver/gomoku3d)에 각 미니게임 전용 README.md 추가**(게임 설명/조작법/스크립트별 구현 방식). 2026-08-23 `feature/pasabolo-duel-minigame`에도 동일 형식으로 추가. `master`/`main`에는 없고 각 feature 브랜치 루트에만 존재
- `feature/pasabolo-duel-minigame`는 GameScene 허브 버튼이 빠진 채로 push됨(사유는 changelog.md 2026-08-23 참고) — 다른 4개 브랜치와 달리 이 부분만 불완전
- `TheRift`는 씬 자체가 삭제되어 더 이상 해당 없음

## 다음 세션 참고
- 세션 시작 시 이 파일을 먼저 읽고 구조/진행상황 파악할 것
- 3D 오목: 스크립트는 완성 상태. Unity 에디터에서 컴포넌트 배치/PhotonView 추가/UI 연결이 어디까지 진행됐는지 먼저 확인할 것(위 체크리스트 참고)
- 파사볼로 대결: 스크립트는 완성 상태. `feature/gomoku3d-minigame` working tree에 uncommitted 상태로 로컬 Editor 작업(씬/컴포넌트/GameScene 버튼)이 돼 있음 — 실제 완료 범위 확인 필요(위 체크리스트 참고). `feature/pasabolo-duel-minigame`는 GameScene 버튼만 별도로 다시 붙여야 함
- SAVE 명령 시(전역 CLAUDE.md 규칙, 2026-08-14 확장): (1) 이 파일 갱신 (2) 이전 내용은 날짜와 함께 changelog.md로 이관 (3) main의 README.md 최신화(신규/변경 미니게임 간단 설명 추가) (4) 세션 중 새로 만든 미니게임이 있으면 전용 feature 브랜치 생성 후 커밋·push (5) 기존 미니게임 규칙이 바뀌었으면 해당 feature 브랜치 README.md도 수정 후 커밋·push
- 새 미니게임의 feature 브랜치를 만들 때 GameScene.unity/EditorBuildSettings.asset/PhotonServerSettings.asset처럼 여러 미니게임이 공유하는 파일이 이미 다른 브랜치에 커밋돼 있으면, 그 브랜치 working tree를 그대로 분기하지 말고 **master에서 새로 분기**할 것(형제 관계 유지). 공유 파일 중 리스트형(EditorBuildSettings/PhotonServerSettings RpcList)은 항목 추가로 안전하게 반영 가능하지만, GameScene.unity처럼 복잡한 씬 YAML은 다른 브랜치의 변경사항과 컨텍스트가 얽혀 patch가 깨끗하게 안 먹을 수 있음 — 이 경우 손으로 YAML을 짜맞추지 말고 해당 브랜치에서 버튼 연결을 다시 하도록 남겨둘 것
