using UnityEngine;

namespace Kennedy.UnityUtility.Pathfinding
{
    // TODO: Add lines in mesh
    [RequireComponent(typeof(Pathfinder))]
    public class PathfinderRenderer : MonoBehaviour
    {
        private static readonly Quaternion k_vr0 = Quaternion.Euler(0, 0, -270);
        private static readonly Quaternion k_vr1 = Quaternion.Euler(0, 0, -180);
        private static readonly Quaternion k_vr2 = Quaternion.Euler(0, 0, -90);
        private static readonly Quaternion k_vr3 = Quaternion.Euler(0, 0, 0);

        [SerializeField] private bool m_DrawGizmos;

        [Header("Nodes")]
        [SerializeField, Range(0, 1)] private float m_NodeTransparency;
        [SerializeField, ColorUsage(false, false)] private Color m_WalkableColor;
        [SerializeField, ColorUsage(false, false)] private Color m_UnWalkableColor;

        [Header("Lines")]
        [SerializeField, Range(0, 1)] private float m_LineTransparency;
        [SerializeField, ColorUsage(false, false)] private Color m_LineColor;

        private Pathfinder _pathfinder;

        private Mesh _mesh;
        private Material _mat;

        private Vector3[] _vertices;
        private Vector2[] _uv;
        private int[] _tris;
        private Color[] _colors;

        public Pathfinder pathfinder
        {
            get
            {
                if (!_pathfinder)
                    _pathfinder = GetComponent<Pathfinder>();
                return _pathfinder;
            }
        }

        public Mesh GeneratedMesh
        {
            get
            {
                if (!_mesh)
                    GenerateMesh();
                return _mesh;
            }
        }

        private void Start()
        {
            GenerateMesh();
            if (pathfinder.graph != null)
                pathfinder.graph.NodeChanged += UpdateNode;
        }

        private void OnDestroy()
        {
            if (_mesh)
                DestroyImmediate(_mesh);
        }

        public void GenerateMesh()
        {
            if (!_mesh)
            {
                _mesh = new Mesh();
                _mesh.name = "Pathfinder-graph";
            }

            int width = pathfinder.GraphWidth;
            int height = pathfinder.GraphHeight;

            int quadCount = width * height;
            _vertices = new Vector3[4 * quadCount];
            _uv = new Vector2[_vertices.Length];
            _tris = new int[6 * quadCount];
            _colors = new Color[_vertices.Length];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    UpdateNode(x, y);

            RebuildMesh();
        }

        public void UpdateColors()
        {
            for (int x = 0; x < _pathfinder.GraphWidth; x++)
                for (int y = 0; y < _pathfinder.GraphHeight; y++)
                    UpdateNode(x, y, updateColor: true, updateTris: false, updateUV: false, updateVertices: false);

            _mesh.colors = _colors;
        }

        private void RebuildMesh()
        {
            _mesh.Clear(false);
            _mesh.vertices = _vertices;
            _mesh.uv = _uv;
            _mesh.triangles = _tris;
            _mesh.colors = _colors;
        }

        private void UpdateNode(PathNode node)
        {
            UpdateNode(node.position.x, node.position.y);
            RebuildMesh();
        }

        private void UpdateNode(int x, int y, bool updateColor = true, bool updateUV = true, bool updateVertices = true, bool updateTris = true)
        {
            CellPosition cell = new CellPosition(x, y);
            int index = x + y * pathfinder.GraphWidth;

            int vIndex = index * 4;
            int vIndex0 = vIndex;
            int vIndex1 = vIndex + 1;
            int vIndex2 = vIndex + 2;
            int vIndex3 = vIndex + 3;

            if (updateVertices)
            {
                Vector3 position = pathfinder.GetCellWorldPosition(cell);
                position += new Vector3(.5f, .5f) * pathfinder.GraphCellSize;
                var baseSize = new Vector2(pathfinder.GraphCellSize, pathfinder.GraphCellSize) * 0.5f;

                _vertices[vIndex0] = position + k_vr0 * baseSize;
                _vertices[vIndex1] = position + k_vr1 * baseSize;
                _vertices[vIndex2] = position + k_vr2 * baseSize;
                _vertices[vIndex3] = position + k_vr3 * baseSize;
            }

            if (updateColor)
            {
                bool walkable = pathfinder.graph != null ? pathfinder.graph.GetNode(cell).walkable : true;
                Color color = walkable ? m_WalkableColor : m_UnWalkableColor;
                color.a = m_NodeTransparency;
                _colors[vIndex0] = color;
                _colors[vIndex1] = color;
                _colors[vIndex2] = color;
                _colors[vIndex3] = color;
            }

            if (updateUV)
            {
                _uv[vIndex0] = new Vector2(0, 0);
                _uv[vIndex1] = new Vector2(0, 1);
                _uv[vIndex2] = new Vector2(1, 0);
                _uv[vIndex3] = new Vector2(1, 1);
            }

            if (updateTris)
            {
                int tIndex = index * 6;

                _tris[tIndex + 0] = vIndex0;
                _tris[tIndex + 1] = vIndex3;
                _tris[tIndex + 2] = vIndex1;

                _tris[tIndex + 3] = vIndex1;
                _tris[tIndex + 4] = vIndex3;
                _tris[tIndex + 5] = vIndex2;
            }
        }

        private void OnDrawGizmos()
        {
            if (!m_DrawGizmos || !_mesh)
                return;

            Graphics.DrawMeshNow(_mesh, Vector3.zero, Quaternion.identity);

            {
                Color c = m_LineColor;
                c.a = m_LineTransparency;
                Gizmos.color = c;
            }

            int width = _pathfinder.GraphWidth;
            int height = _pathfinder.GraphHeight;
            float cellSize = _pathfinder.GraphCellSize;
            Vector2 offset = _pathfinder.GraphOffset;

            Vector3 start = _pathfinder.GetCellWorldPosition(new CellPosition(0, 0));
            Vector3 end = _pathfinder.GetCellWorldPosition(new CellPosition(width, height));

            for (int x = 0; x < width; x++)
            {
                float xPos = x * cellSize + offset.x;
                Gizmos.DrawLine(new Vector3(xPos, start.y), new Vector3(xPos, end.y));
            }
            for (int y = 0; y < height; y++)
            {
                float yPos = y * cellSize + offset.y;
                Gizmos.DrawLine(new Vector3(start.x, yPos), new Vector3(end.x, yPos));
            }

            Gizmos.DrawLine(new Vector3(start.x, end.y), new Vector3(end.x, end.y));
            Gizmos.DrawLine(new Vector3(end.x, start.y), new Vector3(end.x, end.y));
        }
    }
}