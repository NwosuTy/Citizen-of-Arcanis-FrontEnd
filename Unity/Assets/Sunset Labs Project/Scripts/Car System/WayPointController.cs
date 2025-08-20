using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class WayPointController : MonoBehaviour
{
    public WayPointNode[] Nodes { get; private set; }
    public ReservationGrid ReservationGrid { get; private set; }

    [Header("Parameters")]
    [SerializeField] private bool smoothRoute = true;
    [SerializeField] private List<WayPointNode> wayPointNodes = new();
    [field: SerializeField] public Transform DirectorsSpawnPoint { get; private set; }

    private void Awake()
    {
        GetWayPointNodes();
        ReservationGrid = new ReservationGrid();
    }

    public void RenameNodes()
    {
        for (int i = 0; i < wayPointNodes.Count; i++)
        {
            if (wayPointNodes[i] != null)
                wayPointNodes[i].name = "Node " + i.ToString("000");
        }
    }

    public void GetWayPointNodes()
    {
        Nodes = GetComponentsInChildren<WayPointNode>();
        wayPointNodes = new(Nodes.Length);
        foreach (var n in Nodes) wayPointNodes.Add(n);
    }

    public WayPointNode GetNode(Transform requester, WayPointNode excludeNode, out WayPointNode startNode)
    {
        List<WayPointNode> validList = new();
        startNode = (excludeNode != null) ? excludeNode : GetClosestNodeInFrontOfObject(requester, 180f);
        foreach (var node in Nodes)
        {
            if(node == startNode || node == excludeNode)
            {
                continue;
            }
            
            //Ensures Connected Started Nodes Arent Selected
            if(startNode.ConnectedNodes.Contains(node))
            {
                continue;
            }
            validList.Add(node);
        }
        if (validList.Count == 0) return null;
        int random = Random.Range(0, validList.Count);
        return validList[random];
    }

    public WayPointNode GetClosestNodeInFrontOfObject(Transform requester, float detectAngle)
    {
        Vector3 position = requester.position;
        List<WayPointNode> nodeInSight = new();

        for (int i = 0; i < wayPointNodes.Count; ++i)
        {
            var candidate = wayPointNodes[i];
            if (candidate == null)
            {
                continue;
            }
            Vector3 direction = (candidate.transform.position - position);
            float viewAngle = Vector3.Angle(direction, requester.forward);
            if (viewAngle < detectAngle / 2)
            {
                nodeInSight.Add(candidate);
            }
        }
        int allCount = nodeInSight.Count;
        return (allCount > 0) ? GetClosestNode(requester, nodeInSight) : GetClosestNode(requester, wayPointNodes);
    }

    private WayPointNode GetClosestNode(Transform requester, List<WayPointNode> list)
    {
        WayPointNode closestNode = null;
        float maxDistance = float.MaxValue;
        Vector3 position = requester.position;

        for (int i = 0; i < list.Count; i++)
        {
            Transform node = list[i].transform;
            float distance = Vector3.SqrMagnitude(node.position - position);
            if (distance < maxDistance)
            {
                maxDistance = distance;
                closestNode = list[i];
            }
        }
        return closestNode;
    }

    private void OnDrawGizmosSelected()
    {
        wayPointNodes.RemoveAll(x => x == null);
    }
}