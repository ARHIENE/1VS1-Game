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
- MiniGame2: PK(승부차기) 미니게임 전용 씬
- MiniGame_LavaRiver, TheRift: 아직 스크립트 연결 확인 안 됨(직접 참조 스크립트 없음)

## Assets/Scripts 폴더 구조
씬 GUID 참조 분석 기반으로 정리됨 (원래 이름 유지, 임의 그룹핑 안 함)
- `Launcher/` Launcher.cs
- `Lobby/` LobbyManager.cs
- `GameScene/` GameManager.cs, MiniGameSelector.cs
- `Common/` AnimatorDebugger, AOSController, CameraFollow, PlayerController, PlayerSetup, MiniGameManager (여러 씬 공용 컴포넌트)
- `FPS MINIGAME/` FPS 전용
- `PK MINIGAME/` PK 전용 (아래 참고)
- `Unused/` AOSCameraFollow, AnimatorFixer, ChainProjectile, DeathZone — 어떤 씬에서도 직접 참조 미확인

## PK(승부차기) 미니게임 — 스펙대로 전면 재구현 완료
대상 씬: MiniGame2. 스크립트: `Assets/Scripts/PK MINIGAME/`
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

## README.md 구성 (main 브랜치)
개발 환경 / 씬 구성 / 프로젝트 구조 / 미니게임(FPS·PK·LavaRiver·TheRift) / 스크린샷(자리만, 이미지 추가 예정) / 향후 계획(미니게임 추가 예정, Claude 활용 TC 작성 및 QA 예정) / 버전 관리

## 아직 사용자가 직접 해야 하는 작업
1. 골키퍼 Animator Controller에 `GoalkeeperDive1`~`GoalkeeperDive6` 트리거 6개 추가 + 다이빙 클립 연결
2. `PKGoalkeeperCursor`가 Canvas 자식으로 배치되어 있는지 확인
3. 각 PK 스크립트 인스펙터 레퍼런스 연결 확인(특히 `PKUIManager.timerText`는 신규 필드라 UI 요소를 새로 만들어 연결해야 함)
4. MiniGame2 씬에서 2인 실제 플레이 테스트, 카메라/오차감/타이머 체감 튜닝
5. README.md 스크린샷/이미지 추가

## Git 저장소 공개 여부
- **Public 전환 보류 중** (2026-08-13 결정)
- 사유: `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`에 실제 Photon App ID(`AppIdRealtime`)가 하드코딩되어 커밋 히스토리에 존재 → 그대로 노출 시 무단 사용/쿼터 소진 위험
- 재요청 시 먼저 확인할 것: (1) Photon Dashboard에서 App ID 재발급 (2) 새 값은 커밋 제외 처리 (3) 기존 히스토리에서 값 제거(git filter-repo 등) 여부

## 미니게임별 feature 브랜치
- master에서 개별 분기(형제 관계, 계층 아님), 완료 후 각각 master로 병합하는 방식으로 운영하기로 함
- 현재 생성 및 origin push 완료: `feature/fps-minigame`, `feature/pk-minigame`, `feature/lavariver-minigame` (모두 master 최신 커밋 기준, 아직 병합 전)
- `TheRift`는 씬 파일만 있고 연결된 스크립트 없어 제외 (요청 시 추가 가능)

## 다음 세션 참고
- 세션 시작 시 이 파일을 먼저 읽고 구조/진행상황 파악할 것
- SAVE 명령 시: (1) 이 파일 갱신 (2) 이전 내용은 날짜와 함께 changelog.md로 이관 (3) main의 README.md도 최신화
