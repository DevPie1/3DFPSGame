# Easy FPS 사운드·총구 효과·아이콘 적용

기존 PlayerMove, PlayerGunFire, PlayerWeapons, AmmoHud를 사용한다. Easy FPS의 이동·카메라·발사 스크립트를 플레이어에 추가하지 않았다.

## 적용 리소스

| 기능 | 원본 | 연결 방식 |
| --- | --- | --- |
| 발사음 | Easy FPS/Sounds/shotSound.wav | PlayerGunFire.OnShot을 EasyFpsWeaponFeedback이 구독, ShotAudio.PlayOneShot 호출 |
| 걷기 발소리 | Easy FPS/Sounds/playerSound/walk_sound 1.wav | 지상에서 실제 수평 이동 속도가 0.15보다 높을 때 반복 |
| 달리기 발소리 | Easy FPS/Sounds/playerSound/run_sound 1.wav | PlayerMove.IsSprinting에 따라 클립 교체 |
| 총구 효과 | Easy FPS/MuzzelFlash/MuzzlePrefabs/muzzelFlash 01~05 | 매 발사 시 URP 복사본 중 하나를 선택, 0.05초 후 원본 수명 스크립트로 제거 |
| 일반 아이콘 | Easy FPS/Resources/Weap_Icons/NewGun_auto_img.png | 흰색·가로 방향 Sprite 복사본을 Rifle 표시로 사용 |
| 저격 모드 아이콘 | Easy FPS/Resources/Weap_Icons/NewGun_semi_img.png | 같은 방식의 복사본을 Sniper 모드 표시로 사용 |

에셋에 저격 전용 아이콘이 따로 없으므로 semi 아이콘을 조준 모드 표시에 사용한다. 실제 총 종류나 탄창·공격력을 바꾸는 기능은 아니다.

## 씬 구성

- Player: EasyFpsWeaponFeedback, PlayerFootstepAudio 추가.
- Player/ShotAudio: 2D, Volume 0.35, Play On Awake 끔, 루프 끔.
- Player/FootstepAudio: 2D, Volume 0.35, Play On Awake 끔, 루프 켬.
- Player/Model/.../Bip001 R Hand/GunMuzzle: 현재 Soldier_mesh의 demo_weapon 서브메시에서 총 끝을 측정해 오른손 뼈에 붙인 기준점이다. 원래 카메라 아래에 있던 기준점을 이곳으로 옮겼다. 레이저와 플래시가 같은 Transform을 사용하고 총의 애니메이션을 따라간다. 재구성 시에도 지정된 총구를 재사용한다.
- AmmoHud: 기존 옵저버 방식으로 모드에 맞는 Easy FPS 아이콘으로 변경한다.

발소리는 입력만으로 재생하지 않는다. CharacterController의 실제 수평 속도와 접지 여부를 확인한다. 벽에 막혀 움직이지 않거나, 공중에 있거나, 게임오버·일시정지·포커스 상실·이동 컴포넌트 비활성화 상태면 멈춘다. 걷기/달리기 클립은 전환 시에만 바꾼다.

## 원본 보존과 변환

- `Assets/Easy FPS` 아래의 원본은 수정하지 않았다.
- 총구 효과 복사본: `Assets/03.Prefabs/Weapon/Effects/EasyFPS/MuzzleFlash_01~05.prefab`.
- 머티리얼: `Assets/09.materials/EasyFPS`. 메시 기반 효과이므로 URP/Unlit, 투명 Additive, 양면, ZWrite 끔. 원본처럼 평면 형태를 유지하며 그림자 투사·수신을 끈다.
- 아이콘: `Assets/04.Images/UI/EasyFPS/Weapon_Auto.png`, `Weapon_Semi.png`. 원본 PNG 알파를 직접 읽고, 옅은 배경 무늬 제거·흰색화·방향 정렬·Sprite 임포트를 수행한다. 원본 파일은 유지한다.
- `Assets/Editor/EasyFpsResourcesSetup.cs`가 위 변환과 참조 연결을 담당한다. 이후 PlayerWeapons HUD를 재구성해도 Easy FPS 아이콘이 있으면 우선 사용한다.

## 검증 범위

MCP CLI로 적용하고 별도 Preview Scene에서 검사했다. 실제 GameScene의 Play는 시작하지 않았다. 오디오 소스는 검사 중 음소거했으므로 소리를 직접 듣고 검증했다고 주장하지 않는다.

- 기존 Shoot 메서드 호출 시 발사 이벤트 1회와 플래시 생성 확인.
- 총구 효과 5종 각각의 URP 렌더링 확인.
- 걷기/달리기 클립 선택과 정지 처리 확인.
- 두 아이콘의 투명 픽셀·불투명 픽셀 및 HUD 렌더링 확인.
- 피드백 컴포넌트의 이벤트 구독 해제 확인.
- 검사 오브젝트는 Preview Scene 종료 시 정리.

검사 결과는 `Logs/easy-fps-feedback-verification.json`에 저장한다. 현재 Player의 Idle 자세와 CameraPosition을 사용한 1인칭 Preview 렌더에서 총 끝의 플래시 위치를 확인했다. 실제 청취 음량·발소리 리듬·모든 조준 각도와 이동 자세에서의 정렬은 사용자 Play 확인 항목이다. 총구 위치는 Player의 오른손 뼈 아래 GunMuzzle, 음량은 Player의 두 AudioSource에서 조정할 수 있다.
