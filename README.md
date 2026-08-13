# 1VS1 Game

Unity + Photon(PUN2) 기반 1대1 멀티플레이어 미니게임 모음 프로젝트입니다.

## 개발 환경

- Unity 6000.5.8f1
- Photon Unity Networking (PUN2)

## 씬 구성

| 씬 | 설명 |
| --- | --- |
| `LauncherScene` | 접속 및 초기 진입 |
| `LobbyScene` | 로비, 방 생성/참가 |
| `GameScene` | 메인 게임(미니게임 선택) |
| `MiniGame1` | FPS 미니게임 |
| `MiniGame_PK` | PK(승부차기) 미니게임 |
| `MiniGame_LavaRiver` | 미니게임 (개발 중) |
| `MiniGame_Gomoku3D` | 3D 오목(입체 오목) 미니게임 |

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Launcher/       # 접속 관련
│   ├── Lobby/          # 로비 관련
│   ├── GameScene/      # 메인 게임 관련
│   ├── Common/          # 여러 씬 공용 스크립트
│   ├── FPS MINIGAME/   # FPS 미니게임 전용
│   ├── PK MINIGAME/    # PK 승부차기 미니게임 전용
│   ├── GOMOKU MINIGAME/ # 3D 오목 미니게임 전용
│   └── Unused/          # 미사용/미배치 스크립트
├── Scenes/
├── Resources/
├── Photon/
└── ...
```

## 미니게임

### FPS

- 1대1 총격전
- 라운드제(3선승, 최대 5라운드)

### PK(승부차기)

- 마우스로 슛 궤적을 직접 그려 확정한 뒤 파워 게이지로 슛
- 골키퍼는 골대 6구역 중 하나를 선택해 방어
- 정확도(파워 게이지 결과)에 따라 실제 궤적에 오차가 적용됨
- 라운드제 + 서든데스 규칙

### 3D 오목 (입체 오목)

- 기존 오목에 높이(z축) 개념을 더한 15x15x15 입체 격자 오목
- 커넥트4처럼 (x,y) 교차점을 선택하면 중력에 따라 그 기둥의 가장 낮은 빈 칸에 돌이 쌓임
- 가로/세로/높이/대각선 13방향 중 어느 쪽이든 5개가 이어지면 승리
- 마주 앉은 1인칭 착석 시점 카메라, 시선을 올리면 상대방이 보임
- Q/E 키로 자기 턴에 보드를 90도씩 회전시켜 여러 각도에서 확인 가능

### MiniGame_LavaRiver

- 개발 중 (씬만 존재, 스크립트 연결 미확인)

## 스크린샷

(추가 예정)

## 향후 계획

- 미니게임 추가 예정
- Claude를 활용한 테스트 케이스(TC) 작성 및 QA 진행 예정

## 버전 관리

- Git / GitHub 사용
- 브랜치: `main`(README 전용), `master`(실제 작업 브랜치, 프로젝트 전체 파일) — 두 브랜치는 공통 조상 없는 별개 히스토리
- 미니게임별 feature 브랜치: `feature/fps-minigame`, `feature/pk-minigame`, `feature/lavariver-minigame`, `feature/gomoku3d-minigame` — master에서 분기, 완료 시 각각 master로 병합
