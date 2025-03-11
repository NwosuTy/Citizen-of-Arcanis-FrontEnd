using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityStandardAssets.Utility
{
    public class WaypointCircuit : MonoBehaviour
    {
        [SerializeField] private bool smoothRoute = true;
        [SerializeField] private float editorVisualisationSubsteps = 100;
        public WaypointList waypointList = new WaypointList();

        private int numPoints;
        private Vector3[] points;
        private float[] distances;

        public float Length { get; private set; }
        public Transform[] Waypoints => waypointList.items;

        private void Awake()
        {
            if (Waypoints.Length > 1)
            {
                CachePositionsAndDistances();
            }
            numPoints = Waypoints.Length;
        }

        public RoutePoint GetRoutePoint(float dist)
        {
            Vector3 p1 = GetRoutePosition(dist);
            Vector3 p2 = GetRoutePosition(dist + 0.1f);
            return new RoutePoint(p1, (p2 - p1).normalized);
        }

        public Vector3 GetRoutePosition(float dist)
        {
            if (Length == 0)
            {
                Length = distances[^1];
            }
            dist = Mathf.Repeat(dist, Length);

            int point = Array.FindIndex(distances, d => d >= dist);
            if (point == -1) return points[^1];

            int p1n = (point - 1 + numPoints) % numPoints;
            int p2n = point;
            float i = Mathf.InverseLerp(distances[p1n], distances[p2n], dist);

            return smoothRoute ? GetSmoothRoutePosition(point, i) : Vector3.Lerp(points[p1n], points[p2n], i);
        }

        private Vector3 GetSmoothRoutePosition(int point, float i)
        {
            int p0n = (point - 2 + numPoints) % numPoints;
            int p1n = (point - 1 + numPoints) % numPoints;
            int p2n = point % numPoints;
            int p3n = (point + 1) % numPoints;

            return CatmullRom(points[p0n], points[p1n], points[p2n], points[p3n], i);
        }

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
        {
            float i2 = i * i;
            float i3 = i2 * i;
            return 0.5f * ((2f * p1) + (-p0 + p2) * i + (2f * p0 - 5f * p1 + 4f * p2 - p3) * i2 + (-p0 + 3f * p1 - 3f * p2 + p3) * i3);
        }

        private void CachePositionsAndDistances()
        {
            int count = Waypoints.Length;
            points = new Vector3[count];
            distances = new float[count];

            float accumulateDistance = 0;
            for (int i = 0; i < count; i++)
            {
                Transform t1 = Waypoints[i];
                Transform t2 = Waypoints[(i + 1) % count];

                if (t1 != null && t2 != null)
                {
                    points[i] = t1.position;
                    distances[i] = accumulateDistance;
                    accumulateDistance += Vector3.Distance(t1.position, t2.position);
                }
            }
        }

        private void OnDrawGizmos()
        {
            DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        private void DrawGizmos(bool selected)
        {
            waypointList.circuit = this;
            if (Waypoints.Length < 2) return;

            numPoints = Waypoints.Length;
            CachePositionsAndDistances();
            Length = distances[^1];

            Gizmos.color = selected ? Color.yellow : new Color(1, 1, 0, 0.5f);
            Vector3 prev = Waypoints[0].position;

            if (smoothRoute)
            {
                for (float dist = 0; dist < Length; dist += Length / editorVisualisationSubsteps)
                {
                    Vector3 next = GetRoutePosition(dist + 1);
                    Gizmos.DrawLine(prev, next);
                    prev = next;
                }
            }
            else
            {
                foreach (var waypoint in Waypoints)
                {
                    if (waypoint != null)
                    {
                        Gizmos.DrawLine(prev, waypoint.position);
                        prev = waypoint.position;
                    }
                }
            }

            // Draw waypoints as red spheres
            Gizmos.color = Color.red;
            foreach (var waypoint in Waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawSphere(waypoint.position, 0.5f);
                }
            }
        }

        [Serializable]
        public class WaypointList
        {
            public WaypointCircuit circuit;
            public Transform[] items = Array.Empty<Transform>();
        }

        public struct RoutePoint
        {
            public Vector3 position;
            public Vector3 direction;

            public RoutePoint(Vector3 position, Vector3 direction)
            {
                this.position = position;
                this.direction = direction;
            }
        }
    }
}


namespace UnityStandardAssets.Utility.Inspector
{
#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(WaypointCircuit.WaypointList))]
    public class WaypointListDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty items = property.FindPropertyRelative("items");
            WaypointCircuit circuit = property.serializedObject.targetObject as WaypointCircuit;

            if (circuit == null)
            {
                Debug.LogError("WaypointListDrawer: Could not retrieve WaypointCircuit!");
                return;
            }

            float y = position.y;
            if (items == null || items.arraySize == 0)
            {
                DrawAddButton(position.x, ref y, position.width, items);
            }
            else
            {
                DrawWaypointList(position.x, ref y, position.width, items);
            }

            DrawUtilityButtons(position.x, ref y, position.width, property, circuit);
            EditorGUI.EndProperty();
        }


        private void DrawWaypointList(float x, ref float y, float width, SerializedProperty items)
        {
            string[] titles = { "Transform", "^", "v", "-" };
            float[] widths = { 0.7f, 0.1f, 0.1f, 0.1f };

            for (int i = -1; i < items.arraySize; i++)
            {
                float rowX = x;
                for (int n = 0; n < titles.Length; n++)
                {
                    float w = widths[n] * width;
                    Rect rect = new Rect(rowX, y, w, LineHeight);
                    rowX += w;

                    if (i == -1)
                    {
                        EditorGUI.LabelField(rect, titles[n]);
                    }
                    else if (i < items.arraySize)
                    {
                        DrawWaypointRow(items, i, n, rect);
                    }
                }
                y += LineHeight + Spacing;
            }
        }

        private void DrawWaypointRow(SerializedProperty items, int i, int n, Rect rect)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            switch (n)
            {
                case 0:
                    item.objectReferenceValue = EditorGUI.ObjectField(rect, item.objectReferenceValue, typeof(Transform), true);
                    break;
                case 1:
                    if (GUI.Button(rect, "^") && i > 0) items.MoveArrayElement(i, i - 1);
                    break;
                case 2:
                    if (GUI.Button(rect, "v") && i < items.arraySize - 1) items.MoveArrayElement(i, i + 1);
                    break;
                case 3:
                    if (GUI.Button(rect, "-")) items.DeleteArrayElementAtIndex(i);
                    break;
            }
        }

        private void DrawAddButton(float x, ref float y, float width, SerializedProperty items)
        {
            Rect addButtonRect = new Rect(x, y, width, LineHeight);
            if (GUI.Button(addButtonRect, "+ Add Waypoint"))
            {
                items.InsertArrayElementAtIndex(items.arraySize);
            }
            y += LineHeight + Spacing;
        }

        private void DrawUtilityButtons(float x, ref float y, float width, SerializedProperty property, WaypointCircuit circuit)
        {
            if (DrawButton(x, ref y, width, "Assign All Children") && circuit != null)
            {
                AssignAllChildren(property, circuit);
            }
            if (DrawButton(x, ref y, width, "Auto Rename"))
            {
                AutoRename(property);
            }
        }

        private bool DrawButton(float x, ref float y, float width, string label)
        {
            Rect buttonRect = new Rect(x, y, width, LineHeight);
            bool pressed = GUI.Button(buttonRect, label);
            y += LineHeight + Spacing;
            return pressed;
        }

        private void AssignAllChildren(SerializedProperty property, WaypointCircuit circuit)
        {
            WaypointCircuit.WaypointList waypointList = circuit.waypointList;
            List<Transform> children = circuit.transform.Cast<Transform>().OrderBy(t => t.name).ToList();

            if (children.Count == 0)
            {
                return;
            }
            waypointList.items = children.ToArray();

            EditorUtility.SetDirty(circuit);
            property.serializedObject.ApplyModifiedProperties();
        }


        private void AutoRename(SerializedProperty property)
        {
            SerializedProperty items = property.FindPropertyRelative("items");
            for (int i = 0; i < items.arraySize; i++)
            {
                SerializedProperty item = items.GetArrayElementAtIndex(i);
                if (item.objectReferenceValue != null)
                {
                    item.objectReferenceValue.name = $"Waypoint {i:000}";
                }
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty items = property.FindPropertyRelative("items");
            return 40 + (items.arraySize * (LineHeight + Spacing)) + (LineHeight + Spacing);
        }
    }

#endif
}
