using UnityEditor;

[CustomEditor(typeof(TrafficPathController))]
public class TrafficPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        TrafficPathController pathController = (TrafficPathController)target;

        EditorHelper.CreateButton("Create Node", pathController.CreateNode);
        EditorHelper.CreateButton("Create Path", pathController.CreatePath);
        EditorHelper.CreateButton("Rename Nodes", pathController.RenameNodes);
    }
}

[CustomEditor(typeof(TrafficNode))]
public class TrafficNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        TrafficNode node = (TrafficNode)target;

        EditorHelper.CreateButton("Create Connector Node", node.CreateNewConnectorNode);
    }
}
