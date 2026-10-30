# 실제 손에 장착하는 총 모델과 카타나 잔상

기존 전신 기반 1인칭 카메라를 유지한다. 두 총은 `Player/Model/.../RightHand/RifleSocket` 아래에 장착하고 `PlayerWeaponModels`가 무기 모드 알림을 받아 표시와 총구를 교체한다.

| 모드 | 모델 |
| --- | --- |
| 1 일반 | Meshy Futuristic Battle Rifle → FutureRifle.prefab |
| 2 저격 | Gece Studio Rifle VSS - Free → VSSRifle.prefab |
| 3 근접 | 기존 오른손의 SpartanKatana |

프로젝트에 RSS라는 이름의 모델은 없고 Rifle VSS - Free가 있어 2번에 사용했다. 총구 효과와 레이저 시작점은 선택한 총의 Muzzle을 따른다. 3번 근접 모드에서 좌클릭하면 베기와 잔상을 취소하고 1번 Future Rifle로 전환해 발사한다. 탄이 없거나 발사 간격 대기 중이어도 전환은 된다. HUD 아이콘도 모드 변경 알림으로 함께 바뀐다.

## 재질과 장착

- Future Rifle의 비어 있던 Base Map/Normal Map을 연결했다. metallic R과 `1 - roughness`를 URP MetallicSmoothness의 R/A로 묶었다.
- VSS는 제공된 색/노멀/금속·매끄러움/AO 맵을 URP/Lit 복사본에 연결했다.
- `Assets/03.Prefabs/Weapon/FutureRifle.prefab`, `VSSRifle.prefab`은 손잡이를 원점으로, 총열을 +Z 방향으로 정리했다. Future 길이는 0.95, VSS는 1.08로 약 14% 키웠다. 손잡이 원점은 유지하고 총구도 같은 비율로 이동한다. 실제 씬 크기는 플레이어 모델의 0.85 배율을 상속한다.
- 손목의 로컬 축은 현재 Idle 자세를 샘플링해서 보정했다. 모델별 Grip 위치는 `WeaponVisualSetup.MakeRifle` 인수에서 조절한다.
- 재질은 `Assets/09.materials/PlayerWeapons`, 아이콘은 `Assets/04.Images/UI/Weapon_FutureRifle.png`와 `Weapon_VSSRifle.png`에 있다.

## 카타나 대기 자세

`Assets/07.Animations/Player/Melee/MeleeIdle.anim`과 `RightArmMask.mask`를 추가했다. 기존 총 대기 자세에서 오른쪽 쇄골·위팔·팔꿈치·손목을 조정해 손을 몸 바깥쪽과 앞쪽으로 이동했다. 칼 소켓은 손바닥에 유지하고 칼날을 머리에서 떨어뜨린다.

`SpartanCombat.controller`의 `Melee Idle` 레이어는 `PlayerMeleeAttack`이 3번 모드에서만 가중치 1로 켠다. 오른팔과 손가락만 덮으며 머리·왼팔·하체는 기존 동작을 따른다. 그 위의 `Melee Layer`가 Slash를 재생하고 Empty로 돌아오면 전용 대기 자세가 다시 나온다. 총으로 전환하거나 컴포넌트를 끄면 대기 레이어도 꺼진다.

`Tools > Player > Update Katana Idle Pose`로 대기 클립과 마스크만 다시 생성할 수 있다. 전체 근접 구성 재생성에도 포함된다. 생성 시 Generic Transform 클립에 맞게 샘플링용 Animator의 Avatar를 비운다.

## 칼 잔상

`KatanaSlashTrail`은 실제 칼날의 TrailBase/TrailTip 월드 위치를 기록해 두 줄 사이를 잇는 메시를 만든다. 화면에 고정된 효과가 아니라 실제 칼이 지나간 면이다. Actor가 움직여도 과거 위치는 따라 움직이지 않도록 메시의 로컬 좌표로 다시 변환한다.

- Slash의 0.35초 ActivateCollider 이벤트에서 기록 시작
- 0.48초 DeactivateCollider 이벤트에서 기록 종료
- 남은 면은 기본 0.17초 동안 흐려지고 제거
- Collider 방식에서 첫 대상을 맞혀 판정이 닫혀도 잔상은 베기가 끝날 때까지 유지
- 모드 변경/공격 취소/비활성화 시 즉시 정리
- 샘플은 최대 64개이며 큰 위치 이동에는 기록을 비워 긴 줄이 생기는 것을 방지

`Assets/09.materials/SpartanSword/KatanaSlashTrail.shader`는 URP용 양면 가산 블렌딩 셰이더다. 기본 색은 옅은 청색과 흰 칼날 끝이며 깊이 판정으로 벽 뒤는 가린다. 같은 폴더의 mat에서 색을, Player의 Katana Slash Trail에서 Lifetime을 조절한다. 그림자나 미니맵에는 표시하지 않는다.

구성은 `Assets/Editor/WeaponVisualSetup.cs`와 `Tools > Player > Setup Rifle Models And Katana Trail`에서 다시 적용할 수 있다. Unity MCP-CLI에서는 해당 클래스의 `Configure()`를 호출한다.

## 확인

- 모드별 모델 표시, 카타나 표시와 두 총구 전환 검사 통과.
- 잔상 메시 생성, 첫 타격 후 유지, 시간 경과 후 제거, 모드 변경 시 취소 검사 통과.
- 임시 렌더 미리보기에서 두 총의 재질/장착과 잔상 모양 확인.
- 추가 조정: VSS 길이 1.08, 근접 대기 레이어의 실제 손 위치와 하체 회전 유지, Slash가 대기 자세를 덮는 동작, 1번 복귀 시 모델·총구·기본 자세와 공격 취소 확인.
- 코드 자동 검사 및 셰이더 컴파일 확인. 실제 Play에서 조작하며 보는 화면 구도와 잔상의 취향 조절은 사용자에게 남겼다.
