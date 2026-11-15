using UnityEngine;

namespace NoodleLimbs
{
    /// <summary>
    /// A rubber-hose limb drawn live: a smooth, ink-outlined tube along a curve from one joint to another
    /// (shoulder to hand, hip to ankle), with rounded ends. <see cref="Bend"/> bows it sideways, so arms and
    /// legs flex like hoses instead of hinging. One small mesh per limb (outline, then fill on top), redrawn each
    /// frame with the NoodleLimbs/NoodleLimb shader, so it is lit by 2D lights and sorts with the character; cheap
    /// enough for a horde. Runs after whatever moves the joints (DefaultExecutionOrder 200).
    /// </summary>
    /// <remarks>
    /// Setup: put this on a GameObject that also has a SpriteRenderer (no sprite needed) whose material uses the
    /// NoodleLimbs/NoodleLimb shader. That renderer is only a template: its material and sorting layer are copied
    /// to the generated mesh, then it is disabled. Positions and <see cref="Width"/> are in the parent's local
    /// space, so scaling the character scales its limbs.
    /// </remarks>
    [DefaultExecutionOrder(200)]
    public class NoodleLimb : MonoBehaviour
    {
        private const int MaxSegments = 24;
        private const int CapSteps = 5;

        [SerializeField] private Transform _start;
        [SerializeField] private Transform _end;
        [Tooltip("Width of the coloured part (parent's local units).")]
        [SerializeField, Min(0.01f)] private float _width = 0.094f;
        [Tooltip("Ink outline thickness on each side (parent's local units).")]
        [SerializeField, Min(0f)] private float _outline = 0.039f;
        [SerializeField] private Color _fill = new(0.29f, 0.2f, 0.15f);
        [SerializeField] private Color _outlineColor = new(0.1f, 0.08f, 0.06f);
        [Tooltip("Straight pieces along the curve; more = smoother bends.")]
        [SerializeField, Range(2, MaxSegments)] private int _segments = 8;

        private MeshRenderer _renderer;
        private Mesh _mesh;
        private Vector3[] _vertices;
        private Color[] _colors;
        private readonly Vector2[] _points = new Vector2[MaxSegments + 1];
        private readonly Vector2[] _normals = new Vector2[MaxSegments + 1];
        private int _n;
        private int _layerVertices;
        private Color _tint = Color.white;
        private int _order;

        /// <summary>Sideways bow of the limb, in its parent's local units; positive bows to the left of start → end.</summary>
        public float Bend { get; set; }

        public int SortingOrder
        {
            get => _order;
            set
            {
                _order = value;
                if (_renderer != null && _renderer.sortingOrder != value)
                    _renderer.sortingOrder = value;
            }
        }

        /// <summary>Multiplies both colours (hit flash, fade).</summary>
        public Color Tint
        {
            get => _tint;
            set => _tint = value;
        }

        public Color Fill
        {
            get => _fill;
            set => _fill = value;
        }

        public Color OutlineColor
        {
            get => _outlineColor;
            set => _outlineColor = value;
        }

        /// <summary>Width of the coloured part, in the parent's local units.</summary>
        public float Width
        {
            get => _width;
            set => _width = Mathf.Max(0.01f, value);
        }

        /// <summary>Ink outline thickness on each side, in the parent's local units.</summary>
        public float Outline
        {
            get => _outline;
            set => _outline = Mathf.Max(0f, value);
        }

        public void SetJoints(Transform start, Transform end)
        {
            _start = start;
            _end = end;
        }

        private void Awake()
        {
            var template = GetComponent<Renderer>();   // carries the material and sorting layer
            var go = new GameObject("Mesh");
            go.transform.SetParent(transform, false);
            _mesh = new Mesh { name = "Noodle" };
            _mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            if (template != null)
            {
                _renderer.sharedMaterial = template.sharedMaterial;
                _renderer.sortingLayerID = template.sortingLayerID;
                template.enabled = false;
            }
            _renderer.sortingOrder = _order;

            // Fixed topology: a strip plus a fan cap at each end, once for the outline and once for the fill.
            _n = Mathf.Clamp(_segments, 2, MaxSegments);
            _layerVertices = (_n + 1) * 2 + 2 * (CapSteps + 2);
            _vertices = new Vector3[_layerVertices * 2];
            _colors = new Color[_layerVertices * 2];
            int layerTriangles = _n * 6 + 2 * CapSteps * 3;
            var triangles = new int[layerTriangles * 2];
            int t = 0;
            for (int layer = 0; layer < 2; layer++)
            {
                int b = layer * _layerVertices;
                for (int i = 0; i < _n; i++)
                {
                    int a = b + i * 2;
                    triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = a + 2;
                    triangles[t++] = a + 1; triangles[t++] = a + 3; triangles[t++] = a + 2;
                }
                int c = b + (_n + 1) * 2;
                for (int cap = 0; cap < 2; cap++, c += CapSteps + 2)
                {
                    for (int i = 0; i < CapSteps; i++)
                    {
                        triangles[t++] = c; triangles[t++] = c + 1 + i; triangles[t++] = c + 2 + i;
                    }
                }
            }
            _mesh.vertices = _vertices;
            _mesh.uv = new Vector2[_vertices.Length];
            _mesh.colors = _colors;
            _mesh.triangles = triangles;
        }

        private void OnEnable()
        {
            if (_renderer != null)
                _renderer.enabled = true;
        }

        private void OnDisable()
        {
            if (_renderer != null)
                _renderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void LateUpdate()
        {
            if (_start == null || _end == null)
                return;

            Transform parent = transform.parent;
            Vector2 a = parent != null ? (Vector2)parent.InverseTransformPoint(_start.position) : (Vector2)_start.position;
            Vector2 b = parent != null ? (Vector2)parent.InverseTransformPoint(_end.position) : (Vector2)_end.position;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            Vector2 along = b - a;
            Vector2 left = along.sqrMagnitude > 1e-8f ? new Vector2(-along.y, along.x).normalized : Vector2.right;
            Vector2 control = (a + b) * 0.5f + left * (Bend * 2f);   // the curve's middle sits halfway to it

            for (int i = 0; i <= _n; i++)
            {
                float t = i / (float)_n;
                float u = 1f - t;
                _points[i] = u * u * a + 2f * u * t * control + t * t * b;
                Vector2 tangent = 2f * u * (control - a) + 2f * t * (b - control);
                if (tangent.sqrMagnitude < 1e-10f)
                    tangent = along.sqrMagnitude > 1e-10f ? along : Vector2.right;
                tangent.Normalize();
                _normals[i] = new Vector2(-tangent.y, tangent.x);
            }

            float half = _width * 0.5f;
            Layer(0, half + _outline, _outlineColor * _tint);
            Layer(_layerVertices, half, _fill * _tint);
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
        }

        private void Layer(int v, float radius, Color color)
        {
            // Vertex colours reach the shader as-is; SpriteRenderer colours are converted to linear, so match that.
            Color c = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
            int start = v;
            for (int i = 0; i <= _n; i++)
            {
                _vertices[v++] = _points[i] + _normals[i] * radius;
                _vertices[v++] = _points[i] - _normals[i] * radius;
            }
            // Swung counter-clockwise from the normal: +n sweeps out behind the start, -n out past the end.
            Cap(ref v, _points[0], _normals[0], radius);
            Cap(ref v, _points[_n], -_normals[_n], radius);
            for (int i = start; i < v; i++)
                _colors[i] = c;
        }

        // A half-disc fan from one side of the tube end around to the other.
        private void Cap(ref int v, Vector2 centre, Vector2 from, float radius)
        {
            _vertices[v++] = centre;
            for (int i = 0; i <= CapSteps; i++)
            {
                float angle = Mathf.PI * i / CapSteps;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                _vertices[v++] = centre + new Vector2(from.x * cos - from.y * sin, from.x * sin + from.y * cos) * radius;
            }
        }
    }
}
