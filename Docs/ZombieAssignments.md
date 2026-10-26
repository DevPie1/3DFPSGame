# 좀비 / 전투 과제 구현 및 직접 확인 항목

## 연결된 씬과 기본값

- `Assets/01.Scenes/GameScene.unity`의 기존 고정 좀비를 `ZombieSpawner` 생성으로 전환했다.
- `ZombieSpawner`의 자식 `SpawnPoint_Roam`, `SpawnPoint_Patrol`에서 번갈아 생성한다.
- 한 웨이브 총 5마리, 동시에 살아 있는 좀비 최대 2마리, 생성 간격 1초다.
- 화면 위의 `남은 좀비: 5 / 5`는 아직 생성하지 않은 좀비를 포함한 웨이브 잔여 수다. 한 마리 처치할 때마다 감소한다.
- 패트롤 경로는 기존 `ZombiePatrol_Pos/PostionA`, `PositionB`를 사용한다.
- 스포너의 `Spawn Points`에서 위치, Idle Mode, Patrol Points를 변경할 수 있다.

## 구현 구조

1. `Zombie.OnHealthChanged` → `ZombieHealthBar.RefreshHealth`: 활성화 시 구독, 비활성화 시 해제한다. 위치와 회전만 `LateUpdate`에서 처리한다.
2. `ZombieSpawner`는 Lean Pool에서 좀비를 꺼내 생성 지점별 행동을 설정한다.
3. `Zombie.OnDied` → 스포너의 잔여 수 계산 → `OnCountChanged(remaining, maximum)` → `UI_ZombieCount`: UI에서 매 프레임 좀비를 검색하지 않는다.
4. `ZombieStates.cs`의 Idle, Patrol, Roam, Trace, Comeback, Attack, Hit, Death가 각각 별도 상태 객체다. 좀비마다 한 번 생성한 객체를 전환하며 재사용한다.
5. 좀비와 폭탄은 `IPoolable`을 구현했다. 좀비는 체력·상태·생성 원점·CharacterController를, 폭탄은 속도·회전 속도·수명·충돌 무시 설정을 초기화한다.
6. 총은 Line Renderer 하나를 만들어 재사용한다. 명중 지점 또는 최대 사거리까지 빨간 선을 0.06초 표시한다. 총구 참조가 없으면 카메라 오른쪽 아래에서 시작한다.

폭탄의 기존 충돌 폭발 효과는 유지하며, 충돌하지 않아도 5초 후 폭발하고 풀로 반환한다. 이 과제에서 폭탄의 광역 피해는 추가하지 않았다.

## Unity에서 직접 확인할 순서

실행, 컴파일, 화면 검증은 사용자에게 인계한다. 이번 변경 후 자동 빌드나 플레이 검증은 실행하지 않았다.

1. Unity의 스크립트 컴파일이 끝난 뒤 Console의 오류를 확인한다.
2. `GameScene`에서 Play한다. 준비 시간이 지나면 두 생성 위치에서 좀비가 나오며, 동시에 살아 있는 수가 2마리를 넘지 않는지 확인한다.
3. 좀비를 한 번 맞혀 머리 위 체력바가 줄어드는지 확인한다. 처치하면 잔여 수가 `5 / 5 → 4 / 5`로 한 번만 줄어야 한다.
4. 처치 후 새 좀비가 생성되고, 재사용된 좀비의 체력바가 가득 차며 다시 움직이는지 확인한다. 총 5마리를 처치하면 `0 / 5`가 되고 추가 생성이 멈춰야 한다.
5. 플레이어가 탐지 거리 밖에 있을 때 패트롤 좀비가 두 지점을 왕복하고, 로밍 좀비가 생성 위치 주변을 이동하는지 확인한다. 경로는 직선으로 이동할 수 있는 지점에 배치한다.
6. 플레이어가 접근하면 추격·공격으로 전환하고, 생성 위치에서 멀어지면 복귀 후 순찰 또는 배회를 다시 시작하는지 확인한다.
7. 오른쪽 마우스로 폭탄을 여러 번 던진다. 재사용 시 이전 투척의 속도나 회전이 남지 않고, 충돌 또는 5초 경과 후 풀로 돌아가는지 확인한다. G 키로 폭탄을 보충할 수 있다.
8. 왼쪽 마우스로 사격한다. 명중하면 표면에서, 빗나가면 최대 사거리에서 빨간 레이저가 끝나는지 확인한다.
9. 플레이 중 `ZombieSpawner` 컴포넌트 메뉴의 `Restart Wave`로 새 웨이브를 시작할 수 있다. 다시 `5 / 5`로 초기화되고 풀의 좀비가 재사용되는지 확인한다.

## Inspector 조정 위치

- 생성 수량·지점·간격: 씬의 `ZombieSpawner`.
- 이동 속도·탐지 거리·로밍 반경·지점 대기 시간: `Assets/03.Prefabs/Monster/Zombie.prefab`의 `Zombie`.
- 폭탄 수명: `Assets/03.Prefabs/Weapon/Bomb.prefab`의 `Bomb`.
- 레이저 길이·표시 시간·두께·총구: 씬 Player의 `PlayerGunFire`. 길이는 총의 Range를 사용한다.
- 레이저 색상: `Assets/09.materials/GunLaser.mat`.
