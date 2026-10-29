# 옵저버 패턴 PlayerWeapons

오른쪽 아래에 일반/저격 무기 이미지, 현재 총알 / 탄창 최대 총알, 남은 폭탄 수를 표시한다. 별도 무기 인벤토리를 추가하는 대신 기존 총의 조준 모드를 이미지로 구분하며 탄창은 공유한다.

## 조작

- 왼쪽 클릭: 총 발사.
- 오른쪽 클릭 유지: 저격 확대. 놓으면 일반 모드로 복귀.
- G 유지 후 놓기: 폭탄 충전 투척. 기존 충전 시간과 왼팔 모션을 사용한다.
- R: 총 재장전.
- B: 폭탄 재장전. 기존 G 기능을 투척과 충돌하지 않도록 이동했다.

## 변경 알림 흐름

```text
PlayerGunFire.OnAmmoChanged ──────┐
                                 ├→ PlayerWeapons → AmmoHud
PlayerBombFire.OnBombCountChanged ┘       ↑
                                   조준 모드 변경
```

`PlayerGunFire`는 발사로 총알이 줄거나 재장전이 끝나면 알린다. `PlayerBombFire`는 실제 폭탄 발사와 재장전 완료 시 알린다. 충전 취소나 발사 대기 중 취소 시에는 폭탄을 차감하지 않는다.

`PlayerWeapons`는 OnEnable에서 두 무기를 구독하고 OnDisable에서 해제한다. 현재 모드와 탄약 상태를 UI에 제공하고 모드 변경 이벤트를 발행한다. 우클릭 입력과 카메라 FOV 처리는 이 컴포넌트가 담당한다. 포커스 상실·컴포넌트 비활성화 시 조준을 해제한다.

`AmmoHud`도 OnEnable/OnDisable에서 PlayerWeapons를 구독/해제한다. 활성화할 때 현재 상태를 즉시 읽어 초기 표시하고 이후에는 변경 이벤트만 받는다. HUD에 Update 폴링은 없다. UI가 늦게 활성화되더라도 오래된 숫자를 표시하지 않는다.

초기 탄약은 현재 씬 설정대로 총 30 / 30, 폭탄 10이다. 첨부 예시의 폭탄 3으로 게임 스탯을 바꾸지는 않았다.

## 씬과 에셋

- Player 오브젝트: `PlayerWeapons` 추가 및 총·폭탄·Main Camera 참조 연결.
- Canvas/PlayerWeaponsHUD: 기존 AmmoText를 재사용한 탄약 텍스트, WeaponIcon, BombText.
- Canvas/WeaponScopeOverlay: 원형 시야와 조준선을 그리는 `UI_ScopeOverlay`. 다른 HUD 아래에 배치해 미니맵과 수치를 유지한다.
- `Assets/04.Images/UI/EasyFPS/Weapon_Auto.png`, `Weapon_Semi.png`: 현재 사용하는 Easy FPS 기반 일반/조준 모드 Sprite. 기존 Weapon_Rifle/Weapon_Sniper는 Easy FPS 아이콘이 없을 때의 기본 이미지다.
- `Assets/Editor/PlayerWeaponsSetup.cs`: 중복 없이 UI를 구성하고 참조를 연결한다. 기존 Create Ammo HUD 메뉴도 이 구성으로 연결했다.

구현은 `Tools/unity-mcp.ps1`의 code execute/editor 명령으로 적용하고 GameScene에 저장했다. Python 클라이언트나 Computer Use를 사용하지 않았다.

## 확인 범위

Unity 컴파일·콘솔 오류를 확인했고 별도 Preview Scene에서 다음을 검증했다. 실제 GameScene의 Play는 시작하지 않았다.

1. 초기 스냅샷: 30 / 30, 폭탄 10.
2. 모드 이벤트: 저격 Sprite와 원형 조준경 활성화.
3. 실제 총 재장전 코루틴 완료 시 HUD 갱신.
4. 실제 폭탄 재장전 완료 코드의 변경 이벤트와 HUD 갱신.
5. UI 비활성화 후 구독 해제.
6. UI 재활성화 시 현재 모드로 즉시 복구.
7. 일반/저격 HUD 렌더링.

진단 결과는 `Logs/player-weapons-verification.json`에 있다. 진단 오브젝트는 Preview Scene 종료 시 정리했다. 실제 키 입력, 카메라 확대 체감, 발사·투척과 HUD의 동작은 사용자 Play 확인 항목이다.
