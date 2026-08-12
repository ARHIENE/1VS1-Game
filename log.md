# 프로젝트 로그

## 개요
- Unity + Photon(PUN2) 기반 멀티플레이어 미니게임 모음 프로젝트
- 로비에서 매칭 후 여러 미니게임 중 하나로 진입하는 구조로 추정

## 씬 구성
- LauncherScene: 접속/초기 진입
- LobbyScene: 로비, 방 생성/참가
- GameScene: 메인 게임 씬
- MiniGame1, MiniGame2, MiniGame_LavaRiver, TheRift: 개별 미니게임 씬

## 스크립트 구성 (Assets/Scripts)
- Launcher.cs, LobbyManager.cs, GameManager.cs: 접속·로비·매칭 관리
- MiniGameManager.cs, MiniGameSelector.cs: 미니게임 전환/선택
- PlayerController.cs, PlayerSetup.cs, CameraFollow.cs, AOSCameraFollow.cs, AOSController.cs: 플레이어 조작 및 카메라 (AOS 스타일 포함)
- ChainProjectile.cs, DeathZone.cs, AnimatorDebugger.cs, AnimatorFixer.cs: 공용 유틸/기믹
- FPS MINIGAME/: FPS 미니게임 전용 스크립트 (FPSController, GunController, KillCam, MapGenerator, MG01Manager, PlayerAnimator, PlayerHealth, ScoreUI, GameManager)
- PK MINIGAME/: 승부차기(PK) 미니게임 전용 스크립트 (PKBall, PKGameManager, PKGoalDetector, PKGoalkeeper, PKGoalkeeperCursor, PKKicker, PKPowerGauge, PKUIManager)

## 현재 진행 상황
- 아직 파악 중 (구체적 작업 목표 미확인)

## 다음 세션 참고
- 세션 시작 시 이 파일을 먼저 읽고 구조/목표 파악할 것
- SAVE 명령 시 이 파일 내용을 갱신하고, 이전 내용은 changelog.md로 이관
