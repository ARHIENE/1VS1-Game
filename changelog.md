# 변경 이력

## 2026-08-23 — 파사볼로 대결(Pasabolo Duel) 미니게임 신규 구현

사용자가 스페인 전통 스포츠 '파사볼로 타블론' 모티브의 신규 미니게임 스펙 제공. 슬링샷으로 공을 발사해 핀 3개를 쳐서 날린 거리를 라운드별로 누적, 합계가 높은 쪽이 승리하는 턴제 게임. Plan 모드로 진입해 Explore 에이전트 3개를 병렬로 띄워 기존 미니게임의 Photon 동기화 패턴/카메라·입력 패턴/씬 스캐폴딩 컨벤션을 조사한 뒤 구현.

**핵심 아키텍처 결정 (사용자 확인 완료)**
- **물리 동기화 방식이 프로젝트 최초 사례**: 오목/PK는 전부 "입력만 RPC로 보내고 양쪽이 동일 로직 재현" 또는 "결과 경로를 계산해 RPC로 보내고 재생" 방식만 사용해 실시간 Rigidbody 물리 동기화 전례가 전혀 없었음. 이번엔 공-핀 충돌이 핵심이라 **마스터 클라이언트 권위 방식** 채택 — 마스터만 실제 `AddForce`/충돌 시뮬레이션 실행, 비마스터는 Rigidbody를 `isKinematic`으로 돌리고 `PhotonTransformView`(이미 `NetPlayer.prefab`에서 검증된 컴포넌트)로 위치만 수신
- rayas(득점선) 가중치 채점 시스템 포함 결정, 랜덤 마찰/습기 구간은 스펙에도 선택 사항으로 명시돼 있어 이번엔 제외하고 추후 확장으로 보류

**스크립트 7개 신규 작성** (`Assets/Scripts/PASABOLO MINIGAME/`): `PasaboloTurnManager`(MonoBehaviourPun 싱글턴, 턴/점수/RPC 중계), `PasaboloPhysicsController`(마스터 전용 물리 시뮬레이션+정지 감지+거리 계산), `PasaboloRayasScorer`(static 거리→점수 유틸), `PasaboloTableSpawner`(테이블/득점선 완전 자동 생성, 공/핀은 PhotonView가 필요해 씬에 미리 배치된 오브젝트의 외형/Rigidbody만 자동 보강), `PasaboloLaunchController`(레이-평면 교차 기반 슬링샷 드래그 입력, PKKicker 기법 재사용), `PasaboloPowerGaugeUI`(런타임 생성 파워 게이지), `PasaboloCameraController`(Aim→Flight→Result 3단계 동적 카메라, Cinemachine 미사용 확인 후 손수 Lerp/Slerp로 구현).

**브랜치 처리 — 예상 밖의 git 복잡도**: 스크립트 작성 후 사용자가 Unity AI로 씬 생성/컴포넌트 배치/GameScene 허브 버튼 연결까지 직접 진행(feature/gomoku3d-minigame 브랜치 working tree 위에서 작업). SAVE 시점에 새 feature 브랜치를 만들려 했으나, GameScene.unity·EditorBuildSettings.asset·PhotonServerSettings.asset(RPC 목록)이 이미 Gomoku3D가 커밋해둔 상태 위에 얹혀 있어 그대로 가져가면 "형제 브랜치가 master에서 깨끗하게 분기"라는 기존 컨벤션이 깨짐(기존 fps/pk/lavariver/gomoku3d 4개는 전부 master에서 직접 분기됨을 git merge-base로 재확인). 사용자에게 확인 후 **master에서 깨끗하게 분기**하는 쪽으로 결정 → `git worktree`로 master 기준 임시 작업공간을 만들어 스크립트/씬/EditorBuildSettings(씬 엔트리 1줄 추가)/PhotonServerSettings(RPC 2개 추가)만 반영해 커밋. GameScene.unity 허브 버튼은 gomoku3d 브랜치의 씬 컨텍스트가 얽혀 있어 패치가 깨끗하게 적용되지 않아(`git apply` 안전하게 거부, 파일 손상 없음) 이관 포기 — 복잡한 씬 YAML을 손으로 짜맞추지 않는다는 원칙 유지. `feature/pasabolo-duel-minigame` 브랜치에 전용 README.md 추가 후 origin push 완료. 이 브랜치에서는 GameScene 허브 버튼 연결을 별도로 다시 해야 함(README에 명시)

**남은 작업**: `feature/gomoku3d-minigame` 브랜치 working tree(사용자의 실제 로컬 Unity 프로젝트)에는 Pasabolo 관련 변경사항이 전부 커밋되지 않은 상태로 남아 있음(의도적 — SAVE 규칙상 새 미니게임은 전용 브랜치로만 분리, 현재 브랜치는 건드리지 않음). 사용자가 로컬에서 계속 테스트하다 별도로 커밋하고 싶으면 직접 처리 필요. 공/핀 오브젝트의 PhotonView/PhotonTransformView 부착 여부 등 Unity Editor 쪽 실제 완료 상태는 다음 세션에서 확인 필요.

## 2026-08-12 — 최초 log.md 작성 시점 스냅샷
- Unity + Photon(PUN2) 기반 멀티플레이어 미니게임 모음 프로젝트로 파악
- 씬: LauncherScene, LobbyScene, GameScene, MiniGame1, MiniGame2, MiniGame_LavaRiver, TheRift
- 스크립트: Launcher/LobbyManager/GameManager(로비·매칭), MiniGameManager/MiniGameSelector(미니게임 전환), PlayerController/PlayerSetup/CameraFollow/AOSCameraFollow/AOSController, ChainProjectile/DeathZone/AnimatorDebugger/AnimatorFixer(유틸), FPS MINIGAME/(FPS 전용), PK MINIGAME/(승부차기 전용)
- 당시엔 구체적 작업 목표 미확인 상태였음

## 2026-08-13 — Scripts 재구성, PK 미니게임 재구현, Git/GitHub 전환
- **Scripts 폴더 재정리**: 씬 GUID 참조 분석 기반으로 `Launcher/`, `Lobby/`, `GameScene/`, `Common/`, `Unused/`로 분리(원래 이름 유지, 임의 그룹핑 안 함)
- **PK(승부차기) 미니게임 전면 재구현**: 마우스 드래그 궤적 그리기 → 파워 게이지 → 정확도 기반 오차 적용 궤적(`PKTrajectoryUtil` 신규), 물리 대신 path-follow 이동, 골대 1~6구역 판정 방식(`PKGoalZones` 신규), 카메라 고정 시점 통일 및 인스펙터 노출
- **버전관리 전환**: Plastic SCM 중단, Git/GitHub(`github.com/ARHIENE/1VS1-Game`)로 전환 결정
- **Git 브랜치 전략 확립**: `master`(작업 브랜치, 전체 파일)와 `main`(GitHub 기본 브랜치, README 전용)으로 역할 분리 — 두 브랜치는 공통 조상 없는 별개 히스토리
- **README.md 신규 작성**: 개발 환경/씬 구성/프로젝트 구조/미니게임(FPS·PK·LavaRiver·TheRift)/스크린샷(자리만)/향후 계획(미니게임 추가 예정, Claude 활용 TC 작성 및 QA 예정) 구성
- 전역 CLAUDE.md에 규칙 추가: SAVE 시 main README.md도 최신화할 것
- 남은 작업: 골키퍼 Animator 트리거 6개 연결, PK 인스펙터 레퍼런스 확인, 2인 실플레이 테스트, README 스크린샷 추가

### 추가 세션 — 저장소 공개 검토 및 미니게임별 브랜치 분리
- **Public 전환 검토 후 보류**: 커밋 히스토리 스캔 결과 `PhotonServerSettings.asset`에 실제 Photon App ID 하드코딩 확인 → 노출 위험으로 사용자가 보류 결정. 재발급 전까지 재경고하기로 함
- **미니게임별 feature 브랜치 도입**: master에서 개별 분기(형제 관계) 후 완료 시 각각 master로 병합하는 워크플로 확립. `feature/fps-minigame`, `feature/pk-minigame`, `feature/lavariver-minigame` 생성 및 origin push 완료. `TheRift`는 스크립트 없어 제외
- git 브랜치가 폴더처럼 계층 구조가 아니라 커밋 기반 형제 관계라는 점, 분기(master→feature)와 병합(feature→master) 방향이 반대라는 점을 사용자에게 다이어그램으로 설명

## 2026-08-13 ~ 2026-08-14 — 3D 오목(입체 오목) 미니게임 신규 구현
`feature/gomoku3d-minigame` 브랜치(로컬, origin 미push)에서 진행. 스펙이 세 차례에 걸쳐 확장됨: 5x5x5 기본 규칙 → 15x15x15(3375칸, 표준 오목판 크기 반영) → 바둑판 시각 요소(나무 재질+격자선) 요구사항 추가.

**스크립트 7개 신규 작성** (`Assets/Scripts/GOMOKU MINIGAME/`): `GomokuGrid`(순수 C# 보드 데이터), `GomokuWinChecker`(static 13방향 판정 유틸, PK의 `PKGoalZones` 컨벤션 따름), `GomokuTurnManager`(MonoBehaviourPun 싱글턴, RPC 동기화 허브), `GomokuStoneSpawner`(좌표 변환+보드 시각 요소+돌 생성), `GomokuInputHandler`(클릭 판정+마커), `GomokuVisualHighlighter`(승리 라인 강조), `GomokuSeatedCamera`(1인칭 착석 카메라, 애초 이름은 `GomokuOrbitCamera`였다가 요구사항이 궤도 회전→착석 시점으로 바뀌면서 교체됨).

**주요 설계 결정**
- 돌 배치는 결정론적 로직이라 `(x,y,player)`만 RPC로 보내면 양쪽이 동일한 결과에 도달 — PhotonNetwork.Instantiate 없이 PK와 동일한 RPC 패턴 재사용
- 상대 아바타 머리 회전, 보드 90도 회전(Q/E)도 전부 같은 방식(기존 GomokuTurnManager PhotonView RPC 재사용)으로 동기화 — 새 프리팹/Resources 등록 일절 불필요
- 좌석 아바타 2개(내 자리 숨김/상대 자리 표시)를 양쪽 클라이언트가 로컬에서 동일하게 생성, 머리 회전만 RPC로 전파
- 보드 전체(플레인+격자선+돌)는 `boardRoot`라는 회전 가능한 자식 Transform 아래 구성해서 Q/E로 통째로 돌아가게 함

**발견·수정한 버그 3건**
1. 기둥 콜라이더가 바닥~15층까지 뚫린 "벽"처럼 동작해 앞줄이 뒷줄 클릭을 가려버림(카메라 각도가 낮을수록 심해짐) → Plane.Raycast 기반 지면 교차 방식으로 전환해 근본 해결
2. 카메라 눈높이/좌석거리가 사람 스케일 고정값(1.6m/3m)이라 15층(18유닛) 규모 보드와 안 맞아 거의 수평으로 보임 → 보드 전체 폭 비율 기반(`eyeHeightRatio`/`seatSetbackRatio`) 계산으로 전환, 사용자가 에디터에서 직접 찾은 좌표에 맞춰 최종 튜닝
3. 보드 회전 피벗이 모서리(원점)에 있어 Q/E로 돌리면 보드가 화면 밖으로 튕겨나감 → `boardRoot`를 보드 중심으로 이동, 관련 좌표 계산 전부 수정

**남은 작업**: 씬(`MiniGame_Gomoku3D`)은 사용자가 직접 생성 완료(에디터에서 진행), 컴포넌트 부착/PhotonView 추가/UI 연결 등은 다음 세션에서 진행 상태 확인 필요(체크리스트는 log.md 참고). PK 미니게임 씬도 이 세션 중 사용자가 `MiniGame2.unity` → `MiniGame_PK.unity`로 이름 변경, `TheRift.unity` 삭제.
