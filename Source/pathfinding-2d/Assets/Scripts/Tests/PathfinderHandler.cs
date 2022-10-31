using UnityEngine;

namespace Kennedy.UnityUtility.Pathfinding.Tests
{
    public class PathfinderHandler : MonoBehaviour
    {
        [SerializeField] private Pathfinder m_Pathfinder;

        Vector2 lastPoint;
        Path path;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Vector2 mousePosition = GetMousePosition();
                PathNode node = m_Pathfinder.graph.GetNode(mousePosition);
                if (node != null)
                    node.walkable = !node.walkable;
            }

            if (Input.GetMouseButtonDown(1))
            {
                Vector2 a = lastPoint;
                Vector2 b = GetMousePosition();
                lastPoint = b;
                if (path != null)
                    m_Pathfinder.ReleasePath(ref path);
                path = m_Pathfinder.FindPath(a, b);
            }
        }

        private static Vector2 GetMousePosition() => Camera.main.ScreenToWorldPoint(Input.mousePosition);

        private void OnDrawGizmos()
        {
            if (path == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastPoint, .25f);

            Gizmos.color = Color.green;

            for (int i = 0; i < path.vectorPath.Count - 1; i++)
                Gizmos.DrawLine(path.vectorPath[i], path.vectorPath[i + 1]);
        }
    }
}