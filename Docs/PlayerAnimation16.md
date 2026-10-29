# 플레이어 모델 — 16.6부터 끝까지

출처: [16. 플레이어 모델과 애니메이션](https://fluoridated-libra-ce2.notion.site/16-406d025da087811eae01cea297cb3b0d). 공개 본문의 마지막 절은 16.6.1이다.

## 현재 설정

| 항목 | 반영 |
| --- | --- |
| 모델 Rig | Humanoid, Create From This Model |
| Idle / Run / Shoot Rig | Humanoid, Copy From Other Avatar, Soldier_demoAvatar |
| Scale Factor | 모델과 세 클립 모두 0.7 |
| 루트 회전 / XZ | Bake Into Pose 켜짐, Original |
| 루트 Y | Bake Into Pose 켜짐, Feet, Offset 0 |
| Loop Time | Idle / Run 켜짐, Shoot 꺼짐 |
| Animator | Apply Root Motion 꺼짐 |
| MoveBlend / Shoot | Foot IK 켜짐 |
| Model 위치 | 원점을 CharacterController 바닥에 배치. 현재 GameScene에서는 local Y = -0.96 |

높이(Y) 기준은 강의의 Original과 다르게 **Feet**를 사용한다. 침하와 떠 있는 자세에 대한 후속 수정이다. 세 클립의 발 기준을 맞추고, Humanoid가 가져온 발 IK 목표를 MoveBlend와 Shoot에서 사용한다. Foot IK는 별도의 지형 레이캐스트 기능이 아니므로 경사면에 발을 각각 붙이는 기능까지 구현한 것은 아니다.

## 정지 자세에서 확인한 내용과 수정

- 키를 놓은 상황이라는 사용자 답변을 받았다. 코드에서는 입력이 0이면 MoveMotion이 0이며, 블렌드 트리의 0에는 demo_combat_idle이 연결되어 있다. 실제 실행 중 상태 값은 확인하지 않았다.
- 원본 Idle FBX의 무릎 회전 곡선은 왼쪽 약 36~39도, 오른쪽 약 43~45도 굽힌 자세다. 무릎을 굽히고 총을 드는 전투 대기 동작 자체는 원본에 포함된다.
- 기존 보정은 Preview Scene에서 Idle의 첫 프레임 메시 최저점을 측정해 모델 전체를 올렸다. 기존 Editor.log에는 Model Y가 -1에서 0.013으로 변경된 기록이 있다. 사용자 화면에서 여전히 발이 떠 있었으므로 이 보정만으로 문제가 해결됐다고 볼 수 없다.
- 해당 메시 높이 보정을 제거했다. 루트 Y를 Feet로 통일하고 Model 원점을 캡슐 바닥으로 되돌린다. 현재 캡슐 Center Y(-0.06) - Height(1.8)/2 = -0.96이다. 클립의 자세 변화는 애니메이션이 담당하고, 게임 이동과 점프는 기존 CharacterController가 담당한다.
- Setup은 새 상태의 Foot IK를 켜고, Repair는 기존 MoveBlend / Shoot의 Foot IK만 켠다. Repair에서 Controller를 재생성하지 않는다.
- 이전의 Avatar 복사 코드는 Unity Inspector처럼 생성된 Avatar의 직렬화된 HumanDescription을 사용한다. 이것만으로 이전 침하의 원인이 확정됐다고 보지는 않는다.

참고: [Unity 루트 모션 / Feet 기준](https://docs.unity3d.com/6000.0/Documentation/Manual/RootMotion.html), [AnimatorState.iKOnFeet](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animations.AnimatorState-iKOnFeet.html).

## 사용자가 적용하고 확인할 순서

컴파일, Play 및 화면 검증은 요청대로 실행하지 않았다. 아래 항목은 아직 확인되지 않았다.

1. Play를 종료하고 스크립트와 에셋 재임포트를 기다린다.
2. 현재 열린 씬에서 `Tools > Repair Player Animation Rig`를 한 번 실행하고 씬을 저장한다. 이전 보정이 열린 씬에 남아 있어도 Model Y를 캡슐 바닥으로 되돌린다. 현재 구성이라면 Console에 `Feet 기준 Model Y=-0.960`이 나온다. `Setup Player Model`을 다시 실행할 필요는 없다.
3. Play 후 키와 마우스 버튼을 놓는다. 전투 대기 자세에서 발이 바닥에 놓이는지, 몸이 반쯤 묻히거나 공중에 떠 있지 않은지 확인한다.
4. 이동 후 정지, 서서 사격 후 대기로 돌아올 때도 높이가 크게 바뀌지 않는지 확인한다. 점프·착지에서는 기존 캡슐 이동을 따라가야 한다.
5. 정지 상태에서 문제가 남으면 Animator의 현재 상태, MoveMotion 값, Model의 local Y를 확인한다. 임의로 모델을 더 올리지 않는다. 정상적인 목표 값은 MoveBlend / MoveMotion 0 / Model Y -0.96이다.

## 16.6의 리타기팅 미리 보기

강의 16.6의 3번은 Unity Inspector 미리 보기 실습이다. 필요한 `Zombie_Reaction_Hit` / `mixamo.com` 피격 클립은 현재 프로젝트에서 찾지 못했다. 해당 에셋을 가져온 뒤 클립 미리 보기에서 Soldier_demo를 대상으로 선택하고 재생해야 한다. 이 미리 보기와 실제 리타기팅 성공 여부는 확인하지 않았다.
