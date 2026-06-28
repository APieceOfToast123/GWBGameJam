using UnityEditor;
using UnityEngine;

namespace GWBGameJam
{
    public class LaneCalculatorWindow : EditorWindow
    {
        private MonsterConfig _monsterConfig;
        private LaneWaypointConfig _waypointConfig;
        private LaneCalculatorData _calculatorData;

        private string _statusMessage = "就绪";
        private bool _previewEnabled;
        private Vector3[] _previewPositions;

        private readonly Color[] _laneColors =
        {
            Color.red, new Color(0f, 0.8f, 0f), Color.blue, Color.yellow, Color.cyan
        };

        [MenuItem("GWBGameJam/Lane Waypoint Calculator")]
        public static void ShowWindow()
        {
            GetWindow<LaneCalculatorWindow>("Lane Waypoint Calculator");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GWBGameJam — Lane Waypoint Calculator", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            _monsterConfig   = (MonsterConfig)EditorGUILayout.ObjectField("MonsterConfig",        _monsterConfig,   typeof(MonsterConfig),       false);
            _waypointConfig  = (LaneWaypointConfig)EditorGUILayout.ObjectField("LaneWaypointConfig", _waypointConfig, typeof(LaneWaypointConfig), false);
            _calculatorData  = (LaneCalculatorData)EditorGUILayout.ObjectField("LaneCalculatorData",  _calculatorData, typeof(LaneCalculatorData), false);

            EditorGUILayout.Space(8);

            if (_calculatorData != null && _monsterConfig != null)
            {
                int stepCount = _monsterConfig.MoveStepCount;
                EnsureWaypointArraySize(stepCount);

                EditorGUILayout.LabelField($"Y 坐标列表（共 {stepCount} 个点，从上到下）");

                if (GUILayout.Button("Auto Distribute (从场景 PolygonCollider2D 均分)"))
                    AutoDistribute(stepCount);

                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < _calculatorData.WaypointYPositions.Length; i++)
                    _calculatorData.WaypointYPositions[i] = EditorGUILayout.FloatField($"  [{i}] Y", _calculatorData.WaypointYPositions[i]);

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(_calculatorData);

                EditorGUILayout.Space(4);
                DrawLaneNames();
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _monsterConfig != null && _waypointConfig != null && _calculatorData != null;
            if (GUILayout.Button("Bake", GUILayout.Height(28)))
                Bake();
            GUI.enabled = true;

            bool newPreview = GUILayout.Toggle(_previewEnabled, "Scene Preview", "Button", GUILayout.Height(28));
            if (newPreview != _previewEnabled)
            {
                _previewEnabled = newPreview;
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMessage, MessageType.None);
        }

        private void EnsureWaypointArraySize(int stepCount)
        {
            if (_calculatorData.WaypointYPositions == null || _calculatorData.WaypointYPositions.Length != stepCount)
            {
                _calculatorData.WaypointYPositions = new float[stepCount];
                EditorUtility.SetDirty(_calculatorData);
            }
        }

        private void DrawLaneNames()
        {
            if (_calculatorData.LaneRootNames == null) return;
            EditorGUILayout.LabelField("球道 GameObject 名称");
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < _calculatorData.LaneRootNames.Length; i++)
                _calculatorData.LaneRootNames[i] = EditorGUILayout.TextField($"  Lane [{i}]", _calculatorData.LaneRootNames[i]);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_calculatorData);
        }

        private void AutoDistribute(int stepCount)
        {
            float yMin = float.MaxValue;
            float yMax = float.MinValue;

            foreach (string laneName in _calculatorData.LaneRootNames)
            {
                GameObject go = GameObject.Find(laneName);
                if (go == null) continue;

                var col = go.GetComponentInChildren<PolygonCollider2D>();
                if (col == null) continue;

                foreach (Vector2 p in col.points)
                {
                    Vector3 world = col.transform.TransformPoint(p);
                    if (world.y < yMin) yMin = world.y;
                    if (world.y > yMax) yMax = world.y;
                }
            }

            if (yMin == float.MaxValue)
            {
                _statusMessage = "Auto Distribute 失败：未在场景中找到任何球道的 PolygonCollider2D";
                return;
            }

            float margin = (yMax - yMin) * 0.05f;
            float top    = yMax - margin;
            float bottom = yMin + margin;

            for (int i = 0; i < stepCount; i++)
                _calculatorData.WaypointYPositions[i] = stepCount > 1
                    ? Mathf.Lerp(top, bottom, (float)i / (stepCount - 1))
                    : (top + bottom) * 0.5f;

            EditorUtility.SetDirty(_calculatorData);
            _statusMessage = $"Auto Distribute 完成  Y: {top:F2} → {bottom:F2}";
            Repaint();
        }

        private void Bake()
        {
            int laneCount = _calculatorData.LaneRootNames?.Length ?? 0;
            int stepCount = _monsterConfig.MoveStepCount;

            if (laneCount == 0)
            {
                _statusMessage = "Bake 失败：LaneRootNames 为空";
                return;
            }

            // Pre-validate all lanes before writing anything
            for (int l = 0; l < laneCount; l++)
            {
                string name = _calculatorData.LaneRootNames[l];
                GameObject go = GameObject.Find(name);
                if (go == null)
                {
                    _statusMessage = $"Bake 失败：场景中未找到 \"{name}\"";
                    Debug.LogError($"[LaneCalculator] Bake 中止：场景中未找到 \"{name}\"");
                    return;
                }
                if (go.GetComponentInChildren<PolygonCollider2D>() == null)
                {
                    _statusMessage = $"Bake 失败：\"{name}\" 下未找到 PolygonCollider2D";
                    Debug.LogError($"[LaneCalculator] Bake 中止：\"{name}\" 下未找到 PolygonCollider2D");
                    return;
                }
            }

            if (_waypointConfig.Lanes == null || _waypointConfig.Lanes.Length != laneCount)
                _waypointConfig.Lanes = new LaneWaypoints[laneCount];

            _previewPositions = new Vector3[laneCount * stepCount];
            int previewIdx = 0;

            for (int l = 0; l < laneCount; l++)
            {
                GameObject go  = GameObject.Find(_calculatorData.LaneRootNames[l]);
                PolygonCollider2D col = go.GetComponentInChildren<PolygonCollider2D>();

                Vector2[] worldVerts = new Vector2[col.points.Length];
                for (int v = 0; v < col.points.Length; v++)
                    worldVerts[v] = col.transform.TransformPoint(col.points[v]);

                // 4-vertex trapezoid: sort by X → left pair (smaller X) / right pair (larger X)
                System.Array.Sort(worldVerts, (a, b) => a.x.CompareTo(b.x));

                Vector2 leftBottom = worldVerts[0], leftTop    = worldVerts[1];
                Vector2 rightBottom = worldVerts[2], rightTop  = worldVerts[3];

                // Sort each edge pair by Y ascending (bottom < top)
                if (leftBottom.y  > leftTop.y)    (leftBottom,  leftTop)   = (leftTop,  leftBottom);
                if (rightBottom.y > rightTop.y)   (rightBottom, rightTop)  = (rightTop, rightBottom);

                if (_waypointConfig.Lanes[l] == null)
                    _waypointConfig.Lanes[l] = new LaneWaypoints();
                _waypointConfig.Lanes[l].Positions = new Vector2[stepCount];

                for (int i = 0; i < stepCount; i++)
                {
                    float y   = _calculatorData.WaypointYPositions[i];
                    float xl  = HorizontalIntersect(leftBottom,  leftTop,  y, l, i, "left");
                    float xr  = HorizontalIntersect(rightBottom, rightTop, y, l, i, "right");
                    Vector2 mid = new Vector2((xl + xr) * 0.5f, y);
                    _waypointConfig.Lanes[l].Positions[i] = mid;
                    _previewPositions[previewIdx++] = mid;
                }
            }

            _waypointConfig.RecordedStepCount = stepCount;
            EditorUtility.SetDirty(_waypointConfig);
            AssetDatabase.SaveAssets();

            _statusMessage = $"Bake 完成 [{System.DateTime.Now:HH:mm:ss}] — {laneCount} 条球道 × {stepCount} 步 = {laneCount * stepCount} 个点位";
            SceneView.RepaintAll();
        }

        private float HorizontalIntersect(Vector2 bottom, Vector2 top, float y, int lane, int step, string side)
        {
            float dy = top.y - bottom.y;
            if (Mathf.Abs(dy) < 0.0001f) return bottom.x;

            float t = (y - bottom.y) / dy;
            if (t < -0.01f || t > 1.01f)
                Debug.LogWarning($"[LaneCalculator] Lane {lane} Step {step} {side} 边 Y={y:F2} 超出范围 (t={t:F2})，结果可能不准");

            return bottom.x + Mathf.Clamp01(t) * (top.x - bottom.x);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_previewEnabled || _previewPositions == null) return;

            int stepCount = _monsterConfig != null ? _monsterConfig.MoveStepCount : 8;

            for (int i = 0; i < _previewPositions.Length; i++)
            {
                int lane = i / stepCount;
                Handles.color = _laneColors[Mathf.Min(lane, _laneColors.Length - 1)];
                Handles.SphereHandleCap(0, _previewPositions[i], Quaternion.identity, 0.12f, EventType.Repaint);
            }
        }
    }
}
