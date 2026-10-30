# 22장 CullingLab — 22.4까지

원본: [22. 컬링과 최적화](https://fluoridated-libra-ce2.notion.site/22-409d025da08781bf96b1d7260c1b07c6)

사용자가 만든 CullingLab 씬의 카메라와 조명을 유지하고 22.2~22.4를 구성했다. 22.1은 개념 설명이다. 22.5 Mesh LOD와 22.6 GameScene 적용은 포함하지 않았다. 런타임 스크립트나 게임 씬 설정은 변경하지 않았다.

## 오브젝트와 표시

| 오브젝트 | 위치 | 크기 | 재질 | Static 플래그 |
| --- | --- | --- | --- | --- |
| Ground | (0, 0, 7) | (30, 1, 30) | 회색 URP/Lit | Occluder Static만 |
| Wall | (0, 3, 0) | (6, 6, 1) | Yellow | Occluder Static만 |
| Target1 | (-2, 1, 5) | (1, 2, 1) | Green | Occludee Static만 |
| Target2 | (0, 1, 7) | (1, 2, 1) | Green | Occludee Static만 |
| Target3 | (2, 1, 9) | (1, 2, 1) | Green | Occludee Static만 |

회전은 모두 0이다. 전체 Static을 켜지 않고 필요한 컬링 플래그만 설정했다. 재질은 `Assets/09.materials/CullingLab`에 있다.

노션 표의 Ground Scale (30, 1, 30)을 그대로 사용했다. Unity 기본 Plane 자체가 10×10m이므로 실제 바닥 폭은 300m다. 본문의 '30m 바닥' 설명과 표의 Scale이 다른 부분이다.

## 카메라와 베이크

- 기본 시점: 위치 (0, 2, -8), 회전 (0, 0, 0)
- Perspective, FOV 60, Near 0.3, Far 1000, Occlusion Culling 켬
- Smallest Occluder 5, Smallest Hole 0.25, Backface Threshold 100
- 오클루전 베이크 완료: PVS 데이터 9,968바이트, 약 9.7KiB
- 저장 위치: `Assets/01.Scenes/CullingLab/OcclusionCullingData.asset`

Unity의 비동기 오클루전 베이크 API로 계산하고, 완료 시 데이터와 씬을 저장했다. [Unity 6.3 베이크 설정 안내](https://docs.unity3d.com/6000.3/Documentation/Manual/occlusion-culling-getting-started.html), [GenerateInBackground API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/StaticOcclusionCulling.GenerateInBackground.html)를 참고했다.

## 실험 메뉴

`Assets/Editor/CullingLabSetup.cs`에 다음 메뉴를 추가했다. CullingLab만 단독으로 연 Edit 모드에서 사용한다.

| Tools > CullingLab 메뉴 | 동작 |
| --- | --- |
| Setup And Bake Through 22.4 | 오브젝트·재질·플래그를 구성하고 다시 Bake |
| 22.3 Cubes Visible (Wall Off, Far 1000) | 벽과 오클루전을 끄고 큐브를 보는 기준 시점 |
| 22.3 Far Clip 10 (Wall Off) | 위 상태에서 Far를 10으로 줄여 큐브를 시야 밖으로 제외 |
| 22.4 Side View | 벽과 오클루전을 켜고 카메라를 (8, 2, -8), 회전 (0, -25, 0)으로 이동 |
| Restore Front View | 벽 켬, Far 1000, 오클루전 켬, 기본 정면 시점으로 복귀 |

베이크 후 벽만 비활성화해도 저장된 가림 정보는 남는다. 따라서 22.3 비교 메뉴는 프러스텀 판정만 볼 수 있도록 오클루전을 잠시 끄며, 기본 복귀 메뉴에서 다시 켠다. 정적 가림 물체의 위치·크기·활성 상태를 바꾸고 오클루전을 다시 비교하려면 Bake를 다시 한다.

## 확인 범위와 직접 볼 항목

- 기본 정면 카메라, 벽 활성화, Far 1000, 오클루전 켬 상태로 저장했다.
- 프러스텀/AABB 판정에서 Far 1000은 큐브 3개를 포함하고 Far 10은 모두 제외했다.
- 옆 시점의 실제 충돌체 선분 검사에서 Target1 중심은 Wall에 가려지고, Target2·Target3 중심까지는 시선이 연결됨을 확인했다. 이 검사는 배치 확인이며 렌더러의 실제 컬링 결과나 Stats 측정을 대신하지 않는다.
- 베이크 종료, 데이터 에셋 연결, Static 플래그, 컴파일·Unity 콘솔 오류를 확인했다.
- Occlusion Culling 창을 열고 Main Camera를 선택해 두었다. Visualization 탭에서 실제 가려짐을 확인할 수 있다.
- 실제 Play는 실행하지 않았다. 22.4.5는 Game 창 Stats를 켜고 Camera > Rendering > Occlusion Culling을 껐다 켜며 Tris를 비교한다. FPS 개선 수치는 측정하거나 보장하지 않았다.

작업 전 씬은 `.utmp/CullingLab.before-22-4.unity`에 백업했다. 임시 진단 코드는 작업 후 정리했다.
