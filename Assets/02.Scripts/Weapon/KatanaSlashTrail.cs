using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 칼의 밑동/끝 위치를 월드 공간에 기록해 움직인 면을 잔상 메시로 만든다.
public class KatanaSlashTrail : MonoBehaviour
{
    [SerializeField] private Transform _bladeBase;
    [SerializeField] private Transform _bladeTip;
    [SerializeField] private Material _material;
    [SerializeField, Range(0.04f, 0.4f)] private float _lifetime = 0.17f;
    [SerializeField, Range(8, 96)] private int _maxSamples = 64;

    private struct Sample
    {
        public Vector3 Base;
        public Vector3 Tip;
        public float Time;
    }

    private readonly List<Sample> _samples = new List<Sample>(96);
    private readonly List<Vector3> _vertices = new List<Vector3>(192);
    private readonly List<Color> _colors = new List<Color>(192);
    private readonly List<Vector2> _uv = new List<Vector2>(192);
    private readonly List<int> _triangles = new List<int>(570);
    private Mesh _mesh;
    private MeshRenderer _renderer;
    private Transform _meshTransform;
    private bool _emitting;

    public bool IsEmitting => _emitting;

    private void Awake()
    {
        EnsureMesh();
    }

    private void EnsureMesh()
    {
        if (_mesh != null) return;
        var go = new GameObject("KatanaSlashTrailMesh");
        go.layer = 2;
        go.transform.SetParent(transform, false);
        _meshTransform = go.transform;
        _mesh = new Mesh { name = "KatanaSlashTrail_Runtime" };
        _mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = _mesh;
        _renderer = go.AddComponent<MeshRenderer>();
        _renderer.sharedMaterial = _material;
        _renderer.shadowCastingMode = ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.enabled = false;
    }

    public void BeginTrail()
    {
        ClearTrail();
        if (_bladeBase == null || _bladeTip == null || _material == null) return;
        EnsureMesh();
        _emitting = true;
        AddSample(Time.time);
    }

    public void EndTrail() => _emitting = false;

    public void ClearTrail()
    {
        _emitting = false;
        _samples.Clear();
        if (_mesh != null) _mesh.Clear();
        if (_renderer != null) _renderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (_emitting && _bladeBase != null && _bladeTip != null) AddSample(Time.time);
        while (_samples.Count > 0 && Time.time - _samples[0].Time > _lifetime) _samples.RemoveAt(0);
        Rebuild();
    }

    private void AddSample(float now)
    {
        var sample = new Sample { Base = _bladeBase.position, Tip = _bladeTip.position, Time = now };
        if (_samples.Count > 0)
        {
            Sample previous = _samples[_samples.Count - 1];
            float distance = Mathf.Max(Vector3.Distance(previous.Base, sample.Base), Vector3.Distance(previous.Tip, sample.Tip));
            if (distance < .006f) return;
            // 이동/텔레포트 때 화면을 가로지르는 긴 띠가 생기지 않게 한다.
            if (distance > 2f) _samples.Clear();
            else
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt(distance / .06f), 1, 8);
                for (int i = 1; i < steps; i++)
                {
                    float t = (float)i / steps;
                    _samples.Add(new Sample
                    {
                        Base = Vector3.Lerp(previous.Base, sample.Base, t),
                        Tip = Vector3.Lerp(previous.Tip, sample.Tip, t),
                        Time = Mathf.Lerp(previous.Time, now, t)
                    });
                }
            }
        }
        _samples.Add(sample);
        while (_samples.Count > _maxSamples) _samples.RemoveAt(0);
    }

    private void Rebuild()
    {
        if (_mesh == null) return;
        _mesh.Clear();
        _renderer.enabled = _samples.Count >= 2;
        if (_samples.Count < 2) return;
        _vertices.Clear(); _colors.Clear(); _uv.Clear(); _triangles.Clear();
        for (int i = 0; i < _samples.Count; i++)
        {
            Sample sample = _samples[i];
            float age = Mathf.Clamp01((Time.time - sample.Time) / _lifetime);
            float alpha = (1f - age) * (1f - age);
            _vertices.Add(_meshTransform.InverseTransformPoint(sample.Base));
            _vertices.Add(_meshTransform.InverseTransformPoint(sample.Tip));
            _colors.Add(new Color(1, 1, 1, alpha)); _colors.Add(new Color(1, 1, 1, alpha));
            _uv.Add(new Vector2(0, age)); _uv.Add(new Vector2(1, age));
            if (i == 0) continue;
            int index = i * 2;
            _triangles.Add(index - 2); _triangles.Add(index - 1); _triangles.Add(index);
            _triangles.Add(index); _triangles.Add(index - 1); _triangles.Add(index + 1);
        }
        _mesh.SetVertices(_vertices); _mesh.SetColors(_colors); _mesh.SetUVs(0, _uv);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
    }

    private void OnDisable() => ClearTrail();

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}
