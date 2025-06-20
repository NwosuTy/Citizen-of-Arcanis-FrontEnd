using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.VisualScripting;

[CustomEditor(typeof(WayPointNode))]
public class WayPointNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        WayPointNode node = (WayPointNode)target;

        CreateNodeButton("Left Connector", node, WayPointNode.NodeDirection.Left);
        CreateNodeButton("Right Connector", node, WayPointNode.NodeDirection.Right);
        CreateNodeButton("Forward Connector", node, WayPointNode.NodeDirection.Forward);
    }

    private void CreateNodeButton(string text, WayPointNode node, WayPointNode.NodeDirection dir)
    {
        if (GUILayout.Button(text))
        {
            node.CreateNewNode(dir);
        }
    }
}

[CustomEditor(typeof(WayPointController))]
public class WayPointControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        WayPointController controller = (WayPointController)target;
        if (GUILayout.Button("Get All Nodes")) { controller.GetWayPointNodes(); }
        if (GUILayout.Button("Rename Nodes")) { controller.RenameNodes(); }
    }
}

[CustomEditor(typeof(WheelManager))]
public class WheelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        WheelManager wheel = (WheelManager)target;
        if(GUILayout.Button("Get Wheel Collider")) { wheel.GetWheelColliders(); }
    }
}

//Properties

[CustomPropertyDrawer(typeof(EvenOrOddAttribute))]
public class IntEvenOrOddDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EvenOrOddAttribute ioe = (EvenOrOddAttribute)attribute;

        BoundInt b = ioe.boundary;
        int value = property.intValue;

        int newValue = EditorGUI.IntSlider(position, label, value, b.minValue, b.maxValue);
        if (ioe.isEven && newValue % 2 != 0)
        {
            newValue = Mathf.RoundToInt((float)newValue / 2) * 2;
            if (newValue < b.minValue) { newValue = b.minValue; }
            if (newValue > b.maxValue) { newValue = b.maxValue; }
        }
        else if (ioe.isEven != true && newValue % 2 == 0)
        {
            newValue -= (newValue == b.maxValue) ? +1 : -1;
            if (newValue < b.minValue) { newValue = b.minValue; }
            if (newValue > b.maxValue) { newValue = b.maxValue; }
        }
        property.intValue = newValue;
        EditorGUI.EndProperty();
    }
}

public class VehicleSetUpWindow : EditorWindow
{
    //Vehicle Object
    private GameObject vehicleObject;

    //Wheel Parameters
    private Transform rearLeft;
    private Transform rearRight;
    private Transform frontLeft;
    private Transform frontRight;
    [EvenOrOdd(2, 8)] private int wheelCount;

    private WheelManager wm;
    private bool showGizmos;
    private Vector2 previewDir = new(120, -20);
    private PreviewRenderUtility previewUtility;

    [MenuItem("Tools/Vehicle Setup")]
    public static void ShowWindow()
    {
        GetWindow<VehicleSetUpWindow>("Vehicle Setup Window");
    }

    private void OnEnable()
    {
        PreparePreview();
    }

    private void OnDisable()
    {
        previewUtility.Cleanup();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Vehicle Setup", EditorStyles.boldLabel);
        vehicleObject = (GameObject)EditorGUILayout.ObjectField("Vehicle GameObject", vehicleObject, typeof(GameObject), true);

        if (vehicleObject == null)
        {
            EditorGUILayout.HelpBox("Select a GameObject with your vehicle.", MessageType.Info);
            return;
        }
        showGizmos = EditorGUILayout.Toggle("Show Gizmos", showGizmos);
        DrawPreview();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wheel Setup", EditorStyles.boldLabel);

        string[] options = new[] { "Two", "Four", "Six", "Eight" };
        wheelCount = 2 * (EditorGUILayout.Popup("Wheel Count", (wheelCount / 2) - 1, options) + 1);

        rearLeft = (Transform)EditorGUILayout.ObjectField("Rear Left", rearLeft, typeof(Transform), true);
        rearRight = (Transform)EditorGUILayout.ObjectField("Rear Right", rearRight, typeof(Transform), true);
        frontLeft = (Transform)EditorGUILayout.ObjectField("Front Left", frontLeft, typeof(Transform), true);
        frontRight = (Transform)EditorGUILayout.ObjectField("Front Right", frontRight, typeof(Transform), true);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create and Assign WheelManager"))
        {
            CreateWheelManager();
        }
        if(wm != null && vehicleObject != null)
        {
            if(GUILayout.Button("Set Wheel Manager"))
            {
                wm.SetWheelTransforms(frontLeft, frontRight, rearRight, rearLeft);
                wm.GetWheelColliders();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Component Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Required Components"))
        {
            AddComponents();
        }
    }

    private void DrawPreview()
    {
        if(vehicleObject == null)
        {
            return;
        }
        Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true));
        HandleControl(previewRect);

        previewUtility.BeginPreview(previewRect, GUIStyle.none);
        previewUtility.camera.clearFlags = CameraClearFlags.Color;

        previewUtility.camera.backgroundColor = Color.gray;
        List<Renderer> renderers = new(vehicleObject.GetComponentsInChildren<Renderer>(true));
        if(vehicleObject.TryGetComponent<Renderer>(out Renderer rootRenderer))
        {
            renderers.Add(rootRenderer);
        }

        int count = renderers.Count;
        if(count == 0)
        {
            return;
        }
        Bounds bounds = renderers[0].bounds;
        Camera camera = previewUtility.camera;

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        Vector3 center = bounds.center;
        float distance = bounds.extents.magnitude * 2f;
        Quaternion rot = Quaternion.Euler(previewDir.y , previewDir.x, 0);

        Vector3 dir = rot * Vector3.forward;
        Vector3 camPos = center - dir * distance;
 
        Transform t = camera.transform;
        t.SetPositionAndRotation(camPos, Quaternion.LookRotation(center - camPos));

        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100.0f;

        List<MeshFilter> meshFilters = new List<MeshFilter>(vehicleObject.GetComponentsInChildren<MeshFilter>(true));
        if(vehicleObject.TryGetComponent<MeshFilter>(out MeshFilter rootFilter))
        {
            meshFilters.Add(rootFilter);
        }

        foreach(MeshFilter mesh in meshFilters)
        {
            if (mesh.sharedMesh != null)
            {
                Renderer r = mesh.GetComponent<Renderer>();
                Material sharedMat = r.sharedMaterial;
                Material material = (sharedMat != null) ? sharedMat : new Material(Shader.Find("Standard"));
                previewUtility.DrawMesh(mesh.sharedMesh, mesh.transform.localToWorldMatrix, material, 0);
            }
        }

        if(showGizmos && wm != null)
        {
            Handles.color = Color.green;
            foreach(WheelCollider wc in wm.WheelColliders)
            {
                if(wc != null)
                {
                    Handles.DrawWireDisc(wc.transform.position, Vector3.up, wc.radius);
                }
            }
        }
        previewUtility.camera.Render();
        Texture resultRender = previewUtility.EndPreview();
        GUI.DrawTexture(previewRect, resultRender, ScaleMode.StretchToFill, false);
    }

    private void HandleControl(Rect previewRect)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        Event evt = Event.current;
        if (evt.type == EventType.MouseDrag && previewRect.Contains(evt.mousePosition))
        {
            if (evt.button == 0)
            {
                previewDir -= evt.delta * 0.5f;
                evt.Use();
                Repaint();
            }
        }
    }

    private void AddComponents()
    {
        VehicleManager vm = vehicleObject.GetComponent<VehicleManager>();
        if(vm == null) { vehicleObject.AddComponent<VehicleManager>(); }

        if (vehicleObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.mass = 300 * wheelCount;
        }
    }

    private void PreparePreview()
    {
        previewUtility = new()
        {
            cameraFieldOfView = 60.0f, 
        };
        previewUtility.camera.transform.SetPositionAndRotation(new Vector3(0, -1, 5), Quaternion.Euler(20, 0, 0));
        previewUtility.lights[0].intensity = 1.4f;
        previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
        previewUtility.lights[1].intensity = 1.4f;        
    }

    private void CreateWheelManager()
    {
        if (frontLeft == null || frontRight == null || rearLeft == null || rearRight == null)
        {
            Debug.LogWarning("Assign all wheel transforms before creating WheelManager.");
            return;
        }

        if(wm != null)
        {
            Destroy(wm);
        }

        Transform vehicleTrans = vehicleObject.transform;
        if(vehicleObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.mass = 300 * wheelCount;
        }
        GameObject wheels = new("Wheel Transforms");
        Transform t = wheels.transform;
        t.SetParent(vehicleTrans);
        
        rearLeft.SetParent(t);
        rearRight.SetParent(t);
        frontLeft.SetParent(t);
        frontRight.SetParent(t);

        GameObject wheelManager = new("Wheel Manager");
        wheelManager.transform.SetParent(vehicleTrans);

        t.SetParent(wheelManager.transform);
        wm = wheelManager.AddComponent<WheelManager>();
    }
}