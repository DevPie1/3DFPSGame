using UnityEngine;

// 대미지 한 번에 딸린 정보. 주는 쪽이 채워서 맞는 쪽에 넘긴다.
public struct DamageInfo
{
    public float Value; // 대미지 양
    public Vector3 HitPoint; // 맞은 자리
    public Vector3 HitNormal; // 맞은 면이 바라보는 방향 (공격이 온 쪽)
    public GameObject Attacker; // 때린 쪽
}
