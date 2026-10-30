# 21장 공장 맵 꾸미기

원본: [21. 맵 꾸미기](https://fluoridated-libra-ce2.notion.site/21-409d025da08781628885fbc5d03eee97)

사용자가 21.1 에셋 가져오기와 머티리얼 변환을 마친 상태에서 21.2, 21.4, 21.5를 GameScene에 적용했다. 요청에 따라 21.3의 해·하늘·안개 변경은 제외했다. 기존 주광과 RenderSettings는 유지하고, 함께 들어온 중복 Light 묶음만 껐다. Generate Lighting은 실행하지 않았다.

## 맵 구성

- 원본 `Assets/RPG_FPS_game_assets_industrial/Map_v1.unity`의 Map_v1 루트를 GameScene의 Map 아래로 옮겼다. 원본 씬 파일은 변경하지 않았다.
- Map_v1의 원래 위치 (-1.997586, 12.99573, -12.00824), 회전과 크기를 유지했다. 기존 Static 프리팹 구성을 유지하고 루트도 Static으로 표시했다.
- 기존 시험용 바닥·벽·계단·경사로 13개는 `Map/TestTerrain` 아래에 보관하고 비활성화했다.
- 공장 맵의 Renderer 407개에서 사용하는 재질이 URP 셰이더를 사용하는지 확인했다.
- 기존 컨버터가 남긴 `Dust_v1`의 Mobile/Particles/Additive 셰이더만 URP/Particles/Unlit으로 교체했다. 원래 텍스처를 Base Map에 연결하고 Transparent/Additive로 설정했다.

## NavMesh

Map의 NavMeshSurface는 Current Object Hierarchy(Children), Default 레이어, Render Meshes를 수집하도록 설정했다. 활성화된 공장 지형만 포함하며, 꺼 둔 TestTerrain과 Map 밖의 플레이어·드럼통·상자·스포너는 굽기에서 제외된다.

기존 Humanoid의 Radius 0.5, Height 2, Step Height 0.75, Max Slope 45 설정과 최소 영역 0.5를 유지했다. 공장 지형으로 다시 생성한 NavMesh는 삼각형 2,430개다. `Assets/01.Scenes/GameScene/NavMesh-Map.asset`에 저장하면서 기존 GUID를 유지했다.

두 일반 좀비 생성 지점과 엘리트 생성 지점에서 플레이어까지 모두 PathComplete가 나오는지 확인했다. 기존 좀비의 상태 객체·풀링·점프 제한·공격 설정은 변경하지 않았다.

## 배치

| 대상 | 위치 또는 기준 |
| --- | --- |
| Player | (10, 1, 0), 처음에 +Z 방향 |
| 일반 배회 스폰 | (45, 약 3.072, -29) |
| 일반 순찰 스폰 | (48, 약 3.072, -31) |
| 순찰 지점 | (48, 약 2.122, -31), (44, 해당 NavMesh 높이 + 0.05, -24) |
| 엘리트 스폰 | (47, 약 3.155, -40) |
| 드럼통 3개 | X 40/41/42, Y 약 2.928, Z -27 |
| 기존 상자 Box | (16, 약 0.428, -18) |

좀비의 Y는 새로 구운 NavMesh 높이 + Agent Base Offset 1이다. 페이지의 약 2.9보다 높아진 것은 해당 Bake 결과의 발 높이에 맞춘 값이다. 엘리트는 키 3.6m의 공간이 확보되고 플레이어 길과 이어진 마당을 골랐다.

드럼통은 현재 BlueDrum 모델과 기존 콜라이더가 페이지 예시보다 높아, 실제 콜라이더의 맨 아래가 공장 바닥에 닿도록 조정했다. 상자도 NavMesh 높이의 복셀 오차 대신 물리 바닥에 맞췄다. 기존 상자에 빠져 있던 Box 대미지 컴포넌트를 연결하고 Drum 레이어를 적용해 총·근접·범위 피해를 받을 수 있게 했다.

드럼통과 상자는 Map 밖에 유지하며, 새 드럼통도 기존 폭발 스크립트와 BlueDrum 프리팹을 사용한다. 일반 좀비 5 + 엘리트 1의 수량과 골드 기능은 유지한다.

## 미니맵과 카메라

- MinimapCamera의 Height를 30으로 바꿨다. 카메라가 창고 지붕보다 위에 오도록 초기 위치도 맞췄다.
- 미니맵 UI, RenderTexture, Culling Mask(257), Size와 확대·축소 버튼은 유지했다.
- 메인 카메라는 기존 전신 1인칭 CameraPosition을 그대로 따라가도록 새 플레이어 위치에 맞췄다. 회전 초기값만 +Z로 맞췄다.

## 다시 적용하기

`Assets/Editor/FactoryMapSetup.cs`의 Configure 또는 `Tools > Setup Chapter 21 Factory Map`을 GameScene에서 실행한다. 런타임 스크립트는 추가하지 않았다.

작업 전 백업:

- `.utmp/GameScene.before-factory-map.unity`
- `.utmp/NavMesh-Map.before-factory.asset`

미니맵과 스폰 경로, 재질 연결을 Unity MCP-CLI로 확인했다. 실제 Play 이동·추적·드럼통 폭발과 화면 구도 확인은 사용자에게 남겼다. 임시 진단 코드와 씬은 정리했다.
