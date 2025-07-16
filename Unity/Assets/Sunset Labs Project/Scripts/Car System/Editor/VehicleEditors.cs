using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
    private Vector2 scrollPosition;
    private enum WheelFocus { None, F, R, FL, FR, RR1, RR2, RL1, RL2 }

    //Vehicle Object
    private GameObject vehicleObject;
    private WheelCollider selectedCollider;

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
    
    private Vector3 wheelPosition;
    private float wheelMass = 20.0f;
    private float wheelRadius = 0.4f;
    private WheelFocus wheelFocusStatus = WheelFocus.None;

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
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.LabelField("Vehicle Setup", EditorStyles.boldLabel);
        vehicleObject = (GameObject)EditorGUILayout.ObjectField("Vehicle GameObject", vehicleObject, typeof(GameObject), true);

        if (vehicleObject == null)
        {
            EditorGUILayout.HelpBox("Select a GameObject with your vehicle.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        showGizmos = EditorGUILayout.Toggle("Show Gizmos", showGizmos);
        DrawPreview();

        MenuDisplay();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Component Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Required Components"))
        {
            AddComponents();
        }
        EditorGUILayout.EndScrollView();
    }

    private void MenuDisplay()
    {
        if(wheelFocusStatus == WheelFocus.None)
        {
            VehicleBodyMenu();
            return;
        }
        WheelMenu();
    }

    private void WheelMenu()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Editing {wheelFocusStatus} Wheel", EditorStyles.boldLabel);

        wheelMass = EditorGUILayout.FloatField("Wheel Mass", wheelMass);
        wheelRadius = EditorGUILayout.FloatField("Wheel Radius", wheelRadius);
        wheelPosition = EditorGUILayout.Vector3Field("Local Position", wheelPosition);

        if(GUILayout.Button("Apply Changes"))
        {
            ChangeSelecteWheelProperties();
        }
        if(GUILayout.Button("Back To Vehicle Preview"))
        {
            selectedCollider = null;
            wheelFocusStatus = WheelFocus.None;
        }
    }

    private void ChangeSelecteWheelProperties()
    {
        selectedCollider.mass = wheelMass;
        selectedCollider.radius = wheelRadius;
        selectedCollider.center = wheelPosition;
    }

    private void VehicleBodyMenu()
    {
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
        if (wm != null && vehicleObject != null)
        {
            if (GUILayout.Button("Set Wheel Manager"))
            {
                wm.SetWheelTransforms(frontLeft, frontRight, rearRight, rearLeft);
                wm.GetWheelColliders();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Adjust Wheel Individually", EditorStyles.boldLabel);

            SelectWheel("Adjust Front Left", WheelFocus.FL, wm.FL_Wheel);
            SelectWheel("Adjust Rear Left", WheelFocus.RL1, wm.BL_Wheel);
            SelectWheel("Adjust Rear Right", WheelFocus.RR1, wm.BR_Wheel);
            SelectWheel("Adjust Front Right", WheelFocus.FR, wm.FR_Wheel);
        }
    }

    private void SelectWheel(string buttonLabel, WheelFocus wheelFocus, WheelCollider collider)
    {
        if(GUILayout.Button(buttonLabel))
        {
            selectedCollider = collider;
            wheelFocusStatus = wheelFocus;
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

        Camera camera = previewUtility.camera;
        previewUtility.BeginPreview(previewRect, GUIStyle.none);
        camera.clearFlags = CameraClearFlags.Color;

        camera.backgroundColor = Color.gray;
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

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        Quaternion rot = Quaternion.Euler(previewDir.y, previewDir.x, 0);
        float distance = (selectedCollider != null) ? 1.5f : bounds.extents.magnitude * 2f;
        Vector3 center = (selectedCollider != null) ? selectedCollider.transform.position : bounds.center;

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
            Mesh discMesh = GenerateWheelMesh(0.4f);
            Material material = new(Shader.Find("Unlit/Color"))
            {
                color = Color.green
            };

            foreach (WheelCollider wc in wm.WheelColliders)
            {
                if(wc != null)
                {
                    Matrix4x4 matrix = Matrix4x4.TRS
                    (
                        wc.transform.position,
                        Quaternion.LookRotation(t.forward),
                        Vector3.one
                    );
                    previewUtility.DrawMesh(discMesh, matrix, material, 0);
                }
            }
        }
        camera.Render();
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
            rb.constraints = RigidbodyConstraints.None;
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
            string message = "Assign all wheel transforms before creating WheelManager.";
            Debug.LogWarning(message);
            EditorGUILayout.HelpBox(message, MessageType.Info);
            return;
        }

        wm = vehicleObject.GetComponentInChildren<WheelManager>();
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

    private Mesh GenerateWheelMesh(float radius, int segments = 32)
    {
        Mesh mesh = new();
        List<int> triangles = new();
        List<Vector3> vertices = new()
        {
            Vector3.zero
        };
        float angleStep = 360f / segments;

        for(int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
        }

        for(int i = 1; i < segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }
}