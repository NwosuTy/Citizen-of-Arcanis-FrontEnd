using UnityEngine;
using System.Collections.Generic;

public class WayPointNode : MonoBehaviour
{
    [Header("Parameters")]
    public Vector2 offset;
    [SerializeField] private List<WayPointNode> connectedNodes = new();

    public Vector3 NodePosition => transform.position;
    public List<WayPointNode> ConnectedNodes => connectedNodes;

    private void OnDrawGizmosSelected() => DisplayNodeGizmos();
    
    public void DisplayNodeGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 1.0f);

        connectedNodes.RemoveAll(x => x == null);
        for (int i = 0; i < connectedNodes.Count; i++)
        {
            Gizmos.color = Color.yellow;
            Vector3 pos = connectedNodes[i].transform.position;
            Gizmos.DrawLine(transform.position, pos);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos, 1.0f);
        }
    }

    public void CreateNewNode()
    {
        CreateNewConnectorNode();
    }

    private void CreateNewConnectorNode()
    {
        GameObject newObj = new("Node");
        newObj.transform.position = transform.position;
        newObj.transform.SetParent(transform.parent);
        WayPointNode newNode = newObj.AddComponent<WayPointNode>();
        AddNewNode(newNode);
        newNode.AddNewNode(this);
        newNode.offset = this.offset;
    }

    public void AddNewNode(WayPointNode node)
    {
        if (node == null || connectedNodes.Contains(node)) return;
        connectedNodes.Add(node);
    }

    /// <summary>
    /// Returns a random connected node that is NOT in visited (if visited provided).
    /// Uses reservoir sampling to avoid extra allocations.
    /// </summary>
    public WayPointNode GetNextNodeRandomly(HashSet<WayPointNode> visited = null)
    {
        // keep list clean
        connectedNodes.RemoveAll(x => x == null);
        int count = connectedNodes.Count;
        if (count == 0) return null;

        WayPointNode selected = null;
        int seen = 0;
        for (int i = 0; i < count; i++)
        {
            var candidate = connectedNodes[i];
            if (candidate == null) continue;
            if (visited != null && visited.Contains(candidate)) continue;

            seen++;
            // reservoir selection: pick current candidate with probability 1/seen
            if (Random.Range(0, seen) == 0)
            {
                selected = candidate;
            }
        }
        return selected;
    }

    /// <summary>
    /// Returns the connected node (not in visited) that is closest to target.
    /// </summary>
    public WayPointNode GetNextNodeDistanceBased(Vector3 target, HashSet<WayPointNode> visited = null)
    {
        WayPointNode best = null;
        float bestSq = float.MaxValue;
        connectedNodes.RemoveAll(x => x == null);

        for (int i = 0; i < connectedNodes.Count; i++)
        {
            var candidate = connectedNodes[i];
            if (candidate == null)
            {
                continue;
            }
            if (visited != null && visited.Contains(candidate))
            {
                continue;
            }

            float sq = (candidate.transform.position - target).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = candidate;
            }
        }
        return best;
    }

    /// <summary>
    /// Convenience wrapper used by callers.
    /// </summary>
    public WayPointNode GetNextNode(bool distanceBased, Vector3 target, HashSet<WayPointNode> visited = null)
    {
        return distanceBased ? GetNextNodeDistanceBased(target, visited) : GetNextNodeRandomly(visited);
    }
}
