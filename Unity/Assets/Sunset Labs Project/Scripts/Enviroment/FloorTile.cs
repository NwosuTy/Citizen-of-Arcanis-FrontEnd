using UnityEngine;

public class FloorTile : MonoBehaviour
{
    [Header("Road Generation Parameters")]
    public bool hasCollapsed;
    public Vector2 tileDimensions;

    [field: Header("Status")]
    [field: SerializeField] public TileType TypeOfTile { get; private set; }
    [field: SerializeField] public TilesBoundary Boundary { get; private set; }

    [Header("Parameters")]
    public TrafficNode leftNode;
    public TrafficNode rightNode;
    public TrafficVehicle leftVehicle;
    public TrafficVehicle rightVehicle;

    public void SetParameters(TilesBoundary boundary)
    {
        Boundary = boundary;
    }

    public void ConnectNode()
    {
        if (GameObjectTool.TryGetComponentInParent(transform, out TrafficManager manager))
        {
            TrafficPathController left = manager.LeftController;
            TrafficPathController right = manager.RightController;

            left.InitializeNode(leftNode);
            right.InitializeNode(rightNode);
            if (leftVehicle != null) leftVehicle.SetController(left);
            if (rightVehicle != null) rightVehicle.SetController(right);
        }
    }
}
