# 변경 이력

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
