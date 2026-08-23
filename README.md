# 파사볼로 대결 (Pasabolo Duel) 미니게임

`1VS1 Game` 미니게임 모음 중 하나. 스페인 전통 스포츠 '파사볼로 타블론'을 모티브로 한 1대1 턴제 거리 경쟁 게임. 슬링샷 방식으로 공을 발사해 판 위의 핀 3개를 쳐서 날린 거리를 라운드마다 누적 합산, 최종 합계가 높은 쪽이 승리한다.

## 게임 설명

- 골프형 턴제 진행: 두 플레이어가 번갈아 한 턴씩 진행
- 한 턴 = 공 1회 발사 → 판 위 핀 3개를 타격 → 핀 각각의 이동 거리를 채점해 합산
- 한 플레이어당 턴(발사) 횟수는 기본 3회(인스펙터에서 조정 가능, 스펙상 3~5 권장)
- **rayas(득점선) 가중치 채점**: 특정 거리선을 넘을 때마다 배점 상승. 첫 번째 선 이전이라도 핀이 날아갔다면 최소 1점
- 모든 턴 종료 후 누적 점수 합계가 높은 쪽 승리
- Photon PUN2 기반 실시간 1대1 대전. 접속이 안 되어 있으면 한 화면에서 번갈아 진행하는 로컬 핫싯 모드로 자동 전환
- 랜덤 마찰/습기 구간(판 표면 변수 요소)은 이번 구현에는 포함하지 않음 — 추후 확장 예정

## 조작법

| 입력 | 동작 |
| --- | --- |
| 좌클릭(공 위에서 다운) | 내 턴일 때 공을 잡음 |
| 드래그(누른 채 이동) | 발사하고 싶은 방향의 반대쪽으로 당김 — 당긴 거리가 파워, 당긴 각도가 발사 방향 |
| 마우스 버튼 뗌 | 당긴 방향의 반대 방향으로 공 발사(새총 방식). 당김 거리는 최대치로 제한됨 |

- 드래그 중에는 화면 하단에 파워 게이지가, 공에서 당김 지점까지 당김 방향 안내선이 실시간으로 표시된다.
- 카메라는 상황에 따라 자동 전환된다: 조준 중(드래그 중)에는 공을 따라가는 근접 시점 → 발사 순간 핀들을 추적하는 시점으로 전환 → 핀이 멈추면 판 전체를 조망하는 시점으로 줌아웃되며 거리/점수가 표시된다.

## 구현 방식

씬: `Assets/Scenes/MiniGame_PasaboloDuel.unity` · 스크립트: `Assets/Scripts/PASABOLO MINIGAME/`

| 스크립트 | 역할 |
| --- | --- |
| `PasaboloTurnManager.cs` | `MonoBehaviourPun` 싱글턴. 턴/점수 관리, 발사 요청을 RPC로 중계, 결과 수신 후 점수 반영·턴 전환·승패 판정. 카메라가 구독하는 `OnShotFired`/`OnShotSettled` 이벤트 제공 |
| `PasaboloPhysicsController.cs` | **마스터 클라이언트(또는 오프라인 핫싯)만** 실제 Rigidbody 물리(`AddForce`, 충돌)를 시뮬레이션. 공/핀이 모두 멈추면 거리 계산 후 결과를 TurnManager에 보고. 비마스터 클라이언트는 Rigidbody를 `isKinematic`으로 전환해 PhotonTransformView가 전달하는 위치만 그대로 반영 — 이 프로젝트에서 실시간 물리를 Photon으로 동기화하는 첫 사례(오목/PK는 결정론적 RPC 또는 결과-재생 방식만 사용) |
| `PasaboloRayasScorer.cs` | static 유틸. 거리를 rayas 가중치 점수로 변환 |
| `PasaboloTableSpawner.cs` | 테이블(판)과 rayas 득점선은 완전 자동 생성(비네트워크 시각 요소). 공/핀은 PhotonView가 필요해 씬에 미리 배치된 오브젝트를 받아 외형/Rigidbody/Collider가 비어있을 때만 기본값을 채움 |
| `PasaboloLaunchController.cs` | 슬링샷 드래그 입력 처리(레이-평면 교차로 당김 벡터 계산), 발사 시 TurnManager에 요청 전달 |
| `PasaboloPowerGaugeUI.cs` | 프리팹 없이 런타임에 Canvas/Slider를 생성하는 파워 게이지 UI |
| `PasaboloCameraController.cs` | Aim(조준)→Flight(비행 추적)→Result(전체 조망) 3단계 동적 카메라. Cinemachine 미사용, 프로젝트 관행대로 `Lerp`/`Slerp` 코루틴으로 직접 구현 |

공/핀 오브젝트는 PhotonView가 필요해 씬에 미리 배치돼야 하지만, 외형·Rigidbody·Collider는 비워두면 `PasaboloTableSpawner`가 기본 도형(Sphere/Cylinder)으로 자동 생성한다. 테이블과 rayas 득점선은 완전 자동 생성된다.

## 알려진 제한사항

- 이 브랜치의 GameScene(허브) 씬에는 아직 파사볼로 대결로 진입하는 버튼이 연결돼 있지 않다. `MiniGameSelector.LoadMiniGame("MiniGame_PasaboloDuel")`을 호출하는 버튼을 GameScene 선택 UI에 직접 추가해야 한다.
