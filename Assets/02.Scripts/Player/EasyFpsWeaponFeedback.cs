using System.Collections.Generic;
using UnityEngine;

// 발사 로직은 유지하고 실제 발사 알림에 사운드와 총구 효과만 연결한다.
[RequireComponent(typeof(PlayerGunFire))]
public class EasyFpsWeaponFeedback : MonoBehaviour
{
    [SerializeField] private PlayerGunFire _gun;
    [SerializeField] private AudioSource _shotSource;
    [SerializeField] private AudioClip _shotClip;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private GameObject[] _muzzleFlashPrefabs;

    private readonly List<GameObject> _activeFlashes = new List<GameObject>();

    private void Awake()
    {
        if (_gun == null) _gun = GetComponent<PlayerGunFire>();
    }

    private void OnEnable()
    {
        if (_gun != null) _gun.OnShot += HandleShot;
    }

    private void OnDisable()
    {
        if (_gun != null) _gun.OnShot -= HandleShot;
        if (_shotSource != null) _shotSource.Stop();
        foreach (GameObject flash in _activeFlashes)
            if (flash != null) Destroy(flash);
        _activeFlashes.Clear();
    }

    private void HandleShot()
    {
        if (_shotSource != null && _shotClip != null) _shotSource.PlayOneShot(_shotClip);
        if (_muzzle == null || _muzzleFlashPrefabs == null || _muzzleFlashPrefabs.Length == 0) return;
        GameObject prefab = _muzzleFlashPrefabs[Random.Range(0, _muzzleFlashPrefabs.Length)];
        if (prefab == null) return;
        _activeFlashes.RemoveAll(flash => flash == null);
        Quaternion rotation = _muzzle.rotation * Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
        GameObject effect = Instantiate(prefab, _muzzle.position, rotation, _muzzle);
        _activeFlashes.Add(effect);
        // 원본 프리팹의 DestroyAfterTimeParticle가 짧은 수명을 관리한다.
    }
}
