# 엘리트 좀비와 골드 드랍

원본: [번외 H. 엘리트 좀비와 골드 드랍](https://fluoridated-libra-ce2.notion.site/H-409d025da08780d8aafad614f4ad6ba3)

H.1~H.3 본문과 H.4 실습 1~5를 기존 GameScene에 적용했다. 실습 6은 선행 CameraShake 기능이 있을 때 하는 선택 과제이며 현재 프로젝트에는 없어 제외했다. 새 에셋의 Map_v1 씬은 수정하지 않았다.

## 엘리트

- `Assets/03.Prefabs/Monster/EliteZombie.prefab`은 기존 Zombie의 **Prefab Variant**다. 루트 크기는 1, ZombieVisual 크기만 2다.
- 체력 500, 공격력 30, 속도 2.5, 감지 12m, 공격 시작 거리 3m, 복귀 거리 30m, 공격 간격 3초. 엘리트는 무한 추적 옵션을 끄고 복귀 거리를 적용했다.
- Capsule: Center (0, 0.8, 0), Radius 0.5, Height 3.6. Agent: Height 3.6, Radius 0.5, 기존 Base Offset 1 유지.
- 체력바 위치는 루트 기준 Y 3.2, 캔버스와 막대 크기는 200×24다. 텍스처를 유지한 붉은 URP 머티리얼을 사용한다.
- 슈퍼아머는 피해·피 이펙트·체력 알림을 유지하며 Hit 상태로 바꾸지 않는다. Idle/Patrol/Roam/Comeback 중 맞으면 Trace로 전환한다.
- 체력이 **250 미만**으로 떨어지면 한 번만 분노하고 속도가 3.75가 된다. 풀에서 재사용되면 체력·속도·분노·공격·드랍 여부와 몸 콜라이더를 초기화한다.

`EliteZombieSpawner/EliteSpawnPoint`를 기존 NavMesh의 (0, 약 0.09, 20) 지점에 맞춰 추가했다. Agent Base Offset을 포함한 생성 위치는 약 (0, 1.09, 20)이다. 엘리트 한 마리를 별도로 생성하며, 기존 일반 좀비 5마리와 합쳐 HUD는 6 / 6부터 시작한다. 기존 일반 스포너의 배치와 생성 설정은 유지한다.

## 내려찍기

기존 ZombieAttackState와 ZombieAnimation을 사용한다. 일반 좀비의 기존 즉시 공격은 유지하고, Smash Radius가 있는 엘리트만 예고와 애니메이션 타격을 사용한다.

1. 공격을 시작하면 발밑에 반경 3.5m의 붉은 원을 만든다.
2. `Z_Attack.anim`의 0.45초 `OnAttackHit` 이벤트를 모델의 `ZombieAnimationEvents`가 루트 Zombie에 전달한다.
3. 예고 원을 지우고 발밑에서 먼지를 퍼뜨리며, 반경 내 플레이어와 Drum 레이어 대상에 각각 30 피해를 준다.
4. 약 1.1초의 공격 동작 동안 멈춰 선다. 시작 뒤 플레이어가 물러나도 내려찍기는 취소하지 않는다. 이벤트가 누락되면 0.6초에 한 번 판정하는 보완 처리가 있다.

현재 플레이어는 Player라는 레이어 대신 Characters(6)를 사용하므로 Characters + Drum(9)을 대상으로 설정했다. 자식 콜라이더는 부모의 IDamageable로 연결하며, 여러 콜라이더와 상·하체 레이어의 중복 이벤트가 있어도 한 번만 피해를 준다. 사망/풀 반환/피격 취소 때는 예고 원을 정리한다.

먼지는 Duration 0.5, Lifetime 0.6, Speed 5, Size 1, 흙색, Circle Radius 0.5, Burst 30, Stop Action Destroy다. URP 파티클 재질과 부드러운 원형 알파 텍스처를 사용한다. 예고 원은 전용 URP 셰이더로 표시하며 충돌체는 없다.

## 골드와 UI

- `Assets/03.Prefabs/Item/Gold.prefab`: 납작한 원기둥, 크기 (0.3, 0.02, 0.3), 값 10. Box Collider와 Continuous Rigidbody를 사용하며 Ignore Raycast 레이어다.
- `BigGold.prefab`: Gold의 Prefab Variant. 크기 두 배, 값 50.
- 일반 좀비는 2~4개, 엘리트는 9~11개의 일반 골드와 큰 골드 1개를 떨어뜨린다. 실습 3의 무작위 개수를 적용한 결과다.
- 죽는 순간 한 번만 드랍하고 몸 콜라이더를 끈다. 풀 반환이나 중복 피해로 추가 골드가 생기지 않는다.
- 튀어나온 뒤 1초가 지나고 플레이어가 3m 이내면 물리와 충돌을 끄고 0.4초 동안 2차 베지어 곡선으로 날아간다. 끝점은 움직이는 플레이어의 현재 위치를 따른다.
- Player의 `PlayerGold`가 잔액과 변경 이벤트를 소유한다. 왼쪽 위 `Canvas/GoldText`의 `UI_Gold`가 구독하고 활성화 시 현재 잔액을 즉시 읽는다.
- 습득 시 머리 위에 +10/+50 월드 텍스트가 떠오르며 사라진다. 전신 1인칭에서도 보이도록 골드 HUD 아래에도 획득 숫자를 함께 표시한다.
- 골드 잔액과 동전의 수집 상태는 Play 중 스크립트 재컴파일 때 복원한다. 씬 재시작 시 영구 저장하는 기능은 포함하지 않는다.

## 재구성과 확인

`Assets/Editor/EliteGoldSetup.cs`의 `Configure()` 또는 `Tools > Setup Elite Zombie And Gold`로 GameScene에서 다시 적용할 수 있다. 기존 좀비와 골드 프리팹을 바탕으로 변형 프리팹을 생성하고, HUD·스포너를 중복 없이 연결한다. Play를 종료한 상태에서 사용한다.

Unity MCP-CLI로 구성했다. 작업 전의 미저장 씬 상태를 포함한 백업은 `.utmp/GameScene.before-elite-gold.unity`에 보관했다.

임시 에디터 씬에서 확인한 항목:

- 변형 프리팹 연결, 엘리트가 기본 골드 프리팹 참조를 상속하는지 확인.
- 일반 좀비의 Hit, 엘리트의 슈퍼아머와 절반 미만 분노, 속도가 중첩되지 않는지 확인.
- 플레이어/Drum 레이어의 범위 피해 30, 중복 콜라이더·이벤트의 추가 피해 방지, 범위 밖 제외.
- 사망 시 일반 2~4개, 엘리트 9~11개 + BigGold 1개 드랍, 중복 사망 방지.
- 엘리트 풀 재사용 시 체력 500·속도 2.5·콜라이더 복원.
- 골드 10 + 50 습득 알림에서 HUD 60 갱신, 일반 + 엘리트 합산 6 / 6 및 엘리트 사망 알림 후 5 / 6.
- 임시 렌더에서 엘리트와 일반 좀비 크기·붉은 재질·골드·먼지·예고 원 확인. 엘리트 발 높이 오차 약 0.001m, 예고 원 지름 7m, 먼지 입자 30개.

실제 GameScene Play에서의 조작, 애니메이션 타격감, 물리 드랍·비행과 화면 구도 확인은 사용자에게 남겼다. 진단용 임시 씬과 코드는 작업 후 정리한다.
