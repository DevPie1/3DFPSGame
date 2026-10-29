# BlueDrum

파란 드럼 모델과 텍스처, URP 머티리얼을 한 폴더에 정리한 에셋이다.

- Models/blue_drum.fbx
- Textures/blue_oil_drum_3d_model_basecolor.JPEG
- Materials/BlueDrum.mat

연결은 FBX의 머티리얼 매핑 → BlueDrum.mat의 Base Map → JPEG 순서다. 모델과 텍스처가 같은 폴더에 있을 필요는 없다. Unity Project 창에서 파일을 이동하면 .meta의 GUID를 유지할 수 있다.

FBX에 JPEG 데이터를 내장한 것은 아니다. 프로젝트 밖으로 한 파일로 전달하려면 프로젝트의 Exports/BlueDrum.unitypackage를 사용한다. 다른 프로젝트에서 Import Package로 가져오면 위 폴더와 파일이 생성된다.

머티리얼은 Universal Render Pipeline/Lit를 사용한다. 가져올 프로젝트에 URP가 필요하며 Unity/URP 버전이 다르면 머티리얼 호환성을 확인해야 한다. 내보낸 파일에는 URP 패키지의 소스는 포함하지 않는다.

기존 DrumCylinder 폭발 드럼 프리팹은 별도 에셋이다. 이 폴더는 모델·표면 리소스이며 폭발 스크립트나 충돌체를 새로 추가하지 않는다.
