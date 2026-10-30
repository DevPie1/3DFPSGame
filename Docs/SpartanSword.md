# 그린 스파르탄 검 반복 시연

현재 GameScene은 [20장 근접 무기](MeleeCombat20.md) 구성이다. 아래는 초기 반복 시연 기록이며, 현재는 숫자 3과 우클릭으로 한 번씩 벤다.

GameScene을 Play하면 기존 1인칭 시점에서 JTS Katana를 오른손에 쥐고 Free Sword Animations의 `slash3`를 약 1.08초마다 반복한다. 이번 구성은 모션 시연이며 검 타격/데미지 판정은 없다.

## 씬 구성

- `Player/Model`: 그린 스파르탄 전신. 이동/게임 매니저의 Animator 참조를 이 모델로 연결했다. 모델 원점은 CharacterController 바닥, 크기는 0.85이다.
- `Player/PreviousSoldierModel`: 기존 Soldier를 비활성 상태로 보관했다.
- `Model/.../RightHand/SwordSocket/SpartanKatana`: 전신 오른손에 붙인 검. 손바닥 메시 위치에 손잡이를 맞췄다. 검 메시의 긴 축은 로컬 Z이므로 장착 시 X=-90도로 돌린다.
- `Player/CameraPosition`: 로컬 위치 (0, 0.553, 0.2)의 눈높이 기준점이다. 기존 `CameraFollow`가 이 위치를 따라가며 `CameraRotate`가 마우스 회전을 처리한다. 실제 전신 모델과 오른손에 붙은 검을 그대로 바라본다. 휘두르기 중 팔이나 검이 화면 밖으로 나갈 수 있다.
- 전신 렌더러는 일반 표시 상태이며 씬뷰와 게임뷰가 같은 몸/검을 본다. 별도로 만들었던 1인칭 팔 복제본, 전용 팔 메시, 카메라별 전신 숨김 스크립트는 제거했다.
- `PlayerGunFire`, `EasyFpsWeaponFeedback`는 검 시연 중 비활성이다. 기존 무기 HUD와 미니맵은 유지했다. `PlayerWeapons.Start`에서 초기 탄약 상태를 한 번 발행해 발사 컴포넌트가 꺼져 있어도 초기 UI가 갱신되도록 했다.

## 주요 파일

- `Assets/07.Animations/Player/SpartanSword/SpartanSwordLoop.controller`
- `Assets/07.Animations/Player/SpartanSword/Slash3_SpartanLoop.anim`
- `Assets/03.Prefabs/Weapon/SpartanKatana.prefab`
- `Assets/09.materials/SpartanSword/KatanaGeneric01_URP.mat`
- `Tools/Unity/RetargetSpartanSword.cs`

원본 JTS/Free Sword 패키지는 수정하지 않았다. JTS의 Built-in 재질 대신 URP/Lit 복사본을 만들었다. 원본 마스크의 R=금속성, G=매끄러움을 URP의 R/A 채널로 변환했다. 장착용 검의 물리 콜라이더는 껐고 Rigidbody는 제거했다.

## 클립 변환

그린 스파르탄 FBX를 Humanoid로 임포트했으며 Avatar는 유효하다. 다만 이 모델과 공급된 클립을 직접 연결한 미리보기에서 몸 방향/위치가 틀어졌다. 원본 샘플의 Humanoid 재생 자세를 `HumanPoseHandler`로 스파르탄에 전달한 결과는 정상이어서, 그 결과를 60fps, 53개 뼈의 로컬 위치/회전 곡선으로 저장했다.

따라서 최종 클립은 스파르탄 전용 Transform 애니메이션이다. 전신 Animator의 Avatar는 None이며 Foot IK와 Apply Root Motion은 껐다. 이미 변환한 곡선을 Humanoid로 다시 해석하지 않는다. 다른 모델에 그대로 재사용할 때는 다시 변환해야 한다.

수평 루트 이동을 제거하고 매 프레임 발바닥을 원점으로 맞췄다. 마지막 16% 구간을 시작 자세에 연결해 반복 경계를 보정했다. Animator는 기본 상태 `SwordSwing` 하나로 반복하며 `MoveMotion`, `Attack`, `ThrowBomb` 파라미터는 기존 스크립트와의 호환을 위해 남겨 두었다. 기존 이동/사격/폭탄 애니메이션 상태로 전환하지는 않는다.

클립을 다시 만들려면 Unity 에디터와 MCP 연결을 켠 상태에서 프로젝트 루트에서 실행한다.

```powershell
./Tools/unity-mcp.ps1 --timeout 60 --format json code execute --file Tools/Unity/RetargetSpartanSword.cs
```

이 도구는 기존 컨트롤러/씬 구성을 전제로 클립만 다시 생성한다. 임시 Preview Scene과 HumanPoseHandler는 실행 종료 시 정리한다.

## 확인 범위

검 클립은 Unity MCP-CLI에서 반복 동작을 확인했다. 3회 반복 후 루트 위치 이동은 0이며, 모든 애니메이션 경로가 모델에 존재한다. 전신 기반 1인칭으로 변경한 뒤에는 카메라 추적 참조, 복제본 제거, 전신 렌더러 표시와 씬 저장 상태를 확인했다. 변경한 시점의 실제 Play 조작과 주변 벽/머리 모델의 화면 간섭 확인은 사용자에게 남겼다.
