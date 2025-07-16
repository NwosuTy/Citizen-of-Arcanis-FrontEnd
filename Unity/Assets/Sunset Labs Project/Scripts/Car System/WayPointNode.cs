using UnityEngine;
using System.Collections.Generic;

public class WayPointNode : MonoBehaviour
{
    public enum NodeDirection
    {
        Left,
        Right,
        Forward
    }
    private List<int> indexList = new List<int>();

    [Header("Parameters")]
    public Vector2 offset;
    [SerializeField] private List<WayPointNode> connectedNodes = new();

    private void OnDrawGizmosSelected()
    {
        DisplayNodeGizmos();
    }

    public WayPointNode GetNextNodeRandomly()
    {
        return connectedNodes[Random.Range(0, connectedNodes.Count)];
    }

    public WayPointNode GetNextNodeRandomly(WayPointNode exclude)
    {
        indexList.Clear();
        for(int i = 0; i < connectedNodes.Count;  i++)
        {
            if(connectedNodes[i] == exclude)
            {
                continue;
            }
            indexList.Add(i);
        }
        int rnd = Random.Range(0, indexList.Count);
        return connectedNodes[indexList[rnd]];
    }

    public WayPointNode GetNextNodeDistanceBased(Vector3 target)
    {
        WayPointNode nextNode = null;
        float maxDistance = float.MaxValue;

        for(int i = 0; i < connectedNodes.Count; ++i)
        {
            Transform node = connectedNodes[i].transform;
            float distance = Vector3.SqrMagnitude(node.position - target);

            if(distance < maxDistance)
            {
                maxDistance = distance;
                nextNode = connectedNodes[i];
            }
        }
        return nextNode;
    }

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

    public void CreateNewNode(NodeDirection dir)
    {
        Vector3 offset = GetOffset(dir);
        CreateNewConnectorNode(offset);
    }

    private Vector3 GetOffset(NodeDirection dir)
    {
        if(NodeDirection.Left == dir)
        {
            return new Vector3(-offset.x, 0, 0);
        }
        else if(NodeDirection.Right == dir)
        {
            return new Vector3(offset.x, 0, 0);
        }
        return new Vector3(0, 0, offset.y);
    }

    private void CreateNewConnectorNode(Vector3 offset)
    {
        GameObject newObj = new()
        {
            name = "Node"
        };
        newObj.transform.position = transform.position + offset;

        newObj.transform.SetParent(transform.parent);
        WayPointNode newNode = newObj.AddComponent<WayPointNode>();

        AddNewNode(newNode);
        newNode.AddNewNode(this);
        newNode.offset = this.offset;
    }

    public void AddNewNode(WayPointNode node)
    {
        if(connectedNodes.Contains(node))
        {
            return;
        }
        connectedNodes.Add(node);
    }
}
