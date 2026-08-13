# 라바리버(LavaRiver) 미니게임

`1VS1 Game` 미니게임 모음 중 하나. 리그 오브 레전드류 AOS(하향시점 액션) 스타일의 1대1 대결 미니게임. 씬 이름과 달리 실제 구현은 `Common/` 폴더의 공용 스크립트가 담당하고 있어서, 예전엔 "스크립트 미연결"로 파악됐었지만 실제로는 이동/스킬/전투까지 상당 부분 구현되어 있음.

## 게임 설명

- 용암(라바) 강을 사이에 둔 좁은 지형에서 벌어지는 1대1 스킬 대결
- 우클릭 이동, 스킬(투사체), 두 종류의 블링크(순간이동)를 활용해 상대를 먼저 처치하는 쪽이 유리
- 체력이 0이 되면 3초 후 반대편 스폰 지점에서 체력을 채운 채 리스폰
- 체력은 시간이 지나면 자동으로 조금씩 회복됨(`healthRegen`)
- 캐릭터가 25배 크기로 스케일업되어 있어(`PlayerSetup.cs`) 광활한 지형 위에서 싸우는 느낌으로 연출됨
- Photon PUN2 기반 실시간 1대1 대전

## 조작법

| 입력 | 동작 |
| --- | --- |
| 우클릭 | 클릭한 지점으로 이동(NavMesh 기반) |
| **Q** | 스킬 발동 — 마우스 방향으로 사슬형 투사체 발사(쿨다운 있음, 체력을 소모해 사용). 적중 시 상대에게 피해를 주고 시전자는 체력을 회복 |
| **D** / **F** | 블링크(순간이동) — 마우스 위치 방향으로 즉시 이동. 각각 1회용 |
| **Space**(누르고 있는 동안) | 카메라를 강제로 내 캐릭터에 고정 |
| **Y** | 카메라 고정 on/off 토글 |

## 구현 방식

씬: `Assets/Scenes/MiniGame_LavaRiver.unity`. 전용 스크립트 폴더 없이 `Common/`과 `Unused/`의 공용 스크립트가 `PlayerSetup.cs`에서 씬 이름(`MiniGame_LavaRiver`)을 감지해 활성화되는 구조.

| 스크립트 | 역할 |
| --- | --- |
| `Common/AOSController.cs` | 이동/스킬/블링크/체력·리젠/사망·리스폰 등 캐릭터 핵심 로직 전부 담당. `PlayerSetup.cs`가 이 씬에서만 활성화 |
| `Unused/ChainProjectile.cs` | Q 스킬로 발사되는 투사체. `Fireball_Orange`/`Fireball_Blue` 프리팹(`Assets/Resources/`)에 부착되어 있고, SphereCast로 상대를 감지해 피해를 주고 시전자 체력을 회복시킴(폴더명은 `Unused`지만 실제로 사용 중) |
| `Unused/AOSCameraFollow.cs` | LoL 스타일 하향 고정 각도(약 55~60도) 카메라. `PlayerSetup.cs`가 이 씬 진입 시 Main Camera에 동적으로 부착(런타임에 `AddComponent`로 붙어서 씬/프리팹 파일에는 정적으로 남아있지 않음) |
| `PlayerSetup.cs`(Common) | 씬 이름으로 AOS 씬 여부 판단, 25배 스케일 적용, `AOSController` 활성화, 플레이어별 파이어볼 프리팹(`Fireball_Orange`/`Fireball_Blue`) 배정, 카메라 세팅 |
| `MiniGameManager.cs`(Common) | 씬 진입 시 `NetPlayer` 프리팹을 용암 절벽 좌/우 스폰 지점(`Vector3(140 * side, 75.5f, ...)`)에 스폰 |

`Unused/ChainProjectile.cs`/`AOSCameraFollow.cs`는 폴더명과 달리 실제로 사용 중이지만, 같은 폴더의 `DeathZone.cs`(용암 사망 존 처리용으로 작성된 것으로 보임)는 어떤 씬/프리팹에도 부착되어 있지 않아 **실제로는 미사용 상태**임 — 이름 그대로 용암에 빠져도 별도 처리가 없음. 폴더 정리 및 DeathZone 실제 배치 여부 확인이 필요할 수 있음.
