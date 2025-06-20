using UnityEngine;
using System.Collections.Generic;

public class WayPointController : MonoBehaviour
{
    public WayPointNode[] Nodes { get; private set; }

    [Header("Parameters")]
    [SerializeField] private bool smoothRoute = true;
    [SerializeField] private List<WayPointNode> wayPointNodes = new();

    private void Awake()
    {
        GetWayPointNodes();
    }

    public void RenameNodes()
    {
        for (int i = 0; i < wayPointNodes.Count; i++)
        {
            wayPointNodes[i].name = "Node " + i.ToString("000");
        }
    }

    public void GetWayPointNodes()
    {
        Nodes = GetComponentsInChildren<WayPointNode>();
        wayPointNodes = new(Nodes);
    }

    public WayPointNode GetClosestNodeToObject(Transform targetObj, float detectAngle)
    {
        WayPointNode nextNode = null;
        float maxDistance = float.MaxValue;

        Vector3 position = targetObj.position;
        List<WayPointNode> nodeInSight = new();

        for (int i = 0; i < wayPointNodes.Count; ++i)
        {
            Transform node = wayPointNodes[i].transform;
            Vector3 direction = (node.position - position).normalized;

            float viewAngle = Vector3.Angle(direction, targetObj.forward);
            if (viewAngle < detectAngle / 2)
            {
                nodeInSight.Add(wayPointNodes[i]);
            }
        }

        for(int i = 0; i < nodeInSight.Count; i++)
        {
            Transform node = nodeInSight[i].transform;
            float distance = Vector3.SqrMagnitude(node.position - position);

            if (distance < maxDistance)
            {
                maxDistance = distance;
                nextNode = nodeInSight[i];
            }
        }
        return nextNode;
    }

    private void OnDrawGizmosSelected()
    {
        wayPointNodes.RemoveAll(x => x == null);
    }
}
