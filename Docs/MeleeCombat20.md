# 20장 근접 무기

원본: [20. 근접 무기](https://fluoridated-libra-ce2.notion.site/20-407d025da08781639fe7defd7858ea8e)

사용자가 선택한 본문 20.1~20.9만 구현했다. 20.10 실습과제는 제외했다. 모델 준비 과정은 기존 그린 스파르탄과 JTS 카타나를 재사용했다. 눈높이 카메라와 실제 전신/오른손의 검을 유지한다.

## 조작

| 입력 | 동작 |
| --- | --- |
| 1 | 일반 모드. 우클릭 충전 후 놓으면 폭탄 투척 |
| 2 | 저격 모드. 우클릭을 누르는 동안 확대 |
| 3 | 근접 모드. 우클릭 한 번에 한 번 베기 |
| 좌클릭 / R | 총 발사 / 재장전. 3번에서 좌클릭하면 1번 총으로 전환 후 발사 |
| G / B | 기존 충전 폭탄 투척 / 폭탄 재장전 |

`PlayerWeapons.OnModeChanged`로 검과 무기 아이콘을 갱신한다. `OnScopeChanged`는 실제 확대 여부를 별도로 알린다. 탄약과 폭탄 개수도 기존 이벤트를 사용하며 미니맵 UI는 유지한다.

## 구현 대응

| 본문 | 구현 |
| --- | --- |
| 20.1~20.5 | 스파르탄/카타나 재사용. Blade 자식에 칼날 상자, Kinematic Rigidbody, Ignore Raycast |
| 20.6 | 1/2/3 모드, 검 아이콘, Empty → Slash → Empty 상체 레이어 |
| 20.7 | Animator의 PlayerMeleeEvent가 루트 PlayerMeleeAttack에 이벤트 전달 |
| 20.8 | MeleeWeapon 트리거. 한 대상만 타격, 자기 몸 제외, DamageInfo/피격 효과 |
| 20.9 | Fan 판정. 2m/90도, 벽 차단, 여러 대상 각각 한 번, 선택 시 부채꼴 Gizmo |

Inspector의 `Player > Player Melee Attack > Hit Mode`에서 **Fan/Collider**를 비교한다. 기본값은 Fan이다. Collider는 첫 유효 타격 뒤 닫힌다. Fan은 대상에게 콜라이더가 여러 개 있어도 한 번만 피해를 준다. 모드 변경·비활성화·포커스 상실·게임오버 시 남은 판정을 닫는다. 연타로 다음 베기를 예약하지 않는다.

Blade는 JTS 메시의 로컬 Z축을 따라 위치 (0, 0.018, 0.365), 크기 (0.07, 0.035, 0.69)로 배치했다. 손잡이는 제외한다. Trigger이며 Rigidbody는 Kinematic, 중력 없음이다. 총의 기본 레이캐스트가 건너뛰도록 Ignore Raycast 레이어를 사용한다.

## 애니메이션과 파일

`Assets/07.Animations/Player/Melee/SpartanCombat.controller`:

1. Base Layer: Idle/Run, MoveMotion
2. Fire Layer: 상체 사격, Attack
3. Bomb Left Arm: 왼팔 투척, ThrowBomb
4. Melee Idle: 3번 모드 전용 오른팔 대기 자세, RightArmMask
5. Melee Layer: 상체 베기, Slash. 대기 자세보다 우선 적용

`Slash.anim`은 Free Sword의 **slash1**, 길이 약 0.583초이며 반복하지 않는다. 이벤트는 0.35초 ActivateCollider, 0.42초 FanAttack, 0.48초 DeactivateCollider이다. 진입 전환은 0.1초, 복귀는 Exit Time 0.9와 0.1초 전환이다.

이 스파르탄은 Humanoid 클립 직접 재생 시 축 문제가 있었으므로, 기존에 사용한 HumanPose 변환으로 Idle/Run/Shoot/ThrowBomb/Slash를 스파르탄 전용 Transform 곡선으로 저장했다. 같은 폴더의 UpperBodyMask/LeftArmMask도 해당 뼈 경로를 사용한다. Animator는 Avatar None, Root Motion/Foot IK 끔이다. 원본 패키지의 클립은 보존했다.

주요 코드: `PlayerMeleeAttack.cs`, `PlayerMeleeEvent.cs`, `MeleeWeapon.cs`, `PlayerWeapons.cs`. 에디터 구성은 `Assets/Editor/SpartanCombatAnimations.cs`와 `MeleeCombatSetup.cs`에 있다. Unity MCP-CLI의 code execute에서 `MeleeCombatSetup.Configure()`를 호출하거나 `Tools > Player > Setup Chapter 20 Melee Combat`으로 재구성한다. Play를 종료하고 기존 Player/Model/SpartanKatana가 있는 GameScene에서 실행한다.

## 대미지와 대상

기존 TakeDamage(float)를 유지하며 IDamageable에 TakeDamage(DamageInfo)를 더했다. 총/근접 공격은 맞은 점, 방향, 공격자도 전달한다. Zombie/PlayerStats/DrumCylinder/Box가 둘 다 지원한다. 좀비에는 URP 파티클 재질로 복사한 MeleeBlood.prefab을 연결했다. 상자는 체력 30으로 두 번 베면 제거된다. 드럼통의 폭발과 좀비의 사망/풀 반환은 기존 경로를 사용한다.

Drum(9), Zombie(10) 레이어를 대상/프리팹에 적용했다. 상자도 Drum에 포함한다. Target Mask는 이 둘, Obstacle Mask는 Default이다. 미니맵/체력바 UI 레이어는 유지했다. 발사를 다시 켰고, 이후 [무기 모델/칼 잔상](WeaponVisuals.md) 구성에서 선택한 총 모델의 총구로 연결했다. 근접 모드에서 좌클릭하면 1번 총과 해당 총구로 전환한다.

## 확인 범위

- 컴파일, 컨트롤러/마스크/이벤트/Blade/아이콘 참조 확인.
- 임시 추가 씬의 Physics 쿼리: 앞의 두 대상만 각각 25 피해. 옆/뒤/벽 뒤는 피해 없음.
- 중복 콜라이더/중복 Fan 이벤트의 추가 피해 방지.
- Collider 방식의 자기 몸/판정 구간 밖 타격 제외, 첫 대상만 타격 후 닫힘.
- 모드 변경 시 공격 취소 및 연타 예약 방지.
- Run+Slash 샘플링에서 하체 뼈 회전 차이 0도, 상체는 베기 자세. Empty는 기본 상체 동작을 덮지 않음.
- 임시 검증 씬은 정리했다. 실제 Play 입력, 카메라 구도, 타격감과 시각 효과 확인은 사용자에게 남겼다.
