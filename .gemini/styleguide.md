\# 코드 리뷰 가이드 (Unity C#)



\- 리뷰는 한국어로 작성한다.

\- Update/FixedUpdate 안의 GetComponent, Find, 문자열 연결 등 매 프레임 할당을 지적한다.

\- null 참조 가능성, 이벤트 구독 후 해제 누락(OnDisable/OnDestroy)을 확인한다.

\- public 필드 대신 \[SerializeField] private 사용을 권장한다.

\- .meta 파일, Library/Temp 폴더 변경분은 리뷰하지 않는다.

\- 심각한 문제를 먼저, 사소한 스타일 지적은 최소화한다.

