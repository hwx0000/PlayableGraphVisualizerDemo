using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;

namespace GraphVisualizer
{
    public class PlayableGraphVisualizerWindow : EditorWindow, IHasCustomMenu
    {
        private IGraphRenderer m_Renderer;
        private IGraphLayout m_Layout;

        private List<PlayableGraph> m_Graphs;
        private PlayableGraph m_CurrentGraph;
        private GraphSettings m_GraphSettings;

#region Configuration

        private static readonly float s_ToolbarHeight = 17f;
        private static readonly float s_DefaultMaximumNormalizedNodeSize = 0.8f;
        private static readonly float s_DefaultMaximumNodeSizeInPixels = 100.0f;
        private static readonly float s_DefaultAspectRatio = 1.5f;

#endregion

        private PlayableGraphVisualizerWindow()
        {
            m_GraphSettings.maximumNormalizedNodeSize = s_DefaultMaximumNormalizedNodeSize;
            m_GraphSettings.maximumNodeSizeInPixels = s_DefaultMaximumNodeSizeInPixels;
            m_GraphSettings.aspectRatio = s_DefaultAspectRatio;
            m_GraphSettings.showInspector = true;
            m_GraphSettings.showLegend = true;
        }

        [MenuItem("Window/Analysis/PlayableGraph Visualizer")]
        public static void ShowWindow()
        {
            GetWindow<PlayableGraphVisualizerWindow>("PlayableGraph Visualizer");
        }

        // 绘制下拉菜单栏，并返回选中的PlayableGraph(默认是返回第一个)
        private PlayableGraph GetSelectedGraphInToolBar(List<PlayableGraph> graphs, PlayableGraph currentGraph)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(position.width));

            List<string> options = new List<string>(graphs.Count);
            foreach (var graph in graphs)
            {
                string name = graph.GetEditorName();
                options.Add(name.Length != 0 ? name : "[Unnamed]");
            }

            int currentSelection = graphs.IndexOf(currentGraph);
            // 在这里绘制下拉菜单，默认是展示第一个
            int newSelection = EditorGUILayout.Popup(currentSelection != -1 ? currentSelection : 0, options.ToArray(), GUILayout.Width(200));

            Debug.Log(newSelection);
            PlayableGraph selectedGraph = new PlayableGraph();
            if (newSelection != -1)
                selectedGraph = graphs[newSelection];

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            return selectedGraph;
        }

        private static void ShowMessage(string msg)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label(msg);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        void Update()
        {
            // If in Play mode, refresh the graph each update.
            if (EditorApplication.isPlaying)
                Repaint();
        }

        void OnInspectorUpdate()
        {
            // If not in Play mode, refresh the graph less frequently.
            if (!EditorApplication.isPlaying)
                Repaint();
        }

        // 把底层的graphs拿过来，而且保证m_Graphs随时与底层保持一致
        void OnEnable()
        {
            m_Graphs = new List<PlayableGraph>(UnityEditor.Playables.Utility.GetAllGraphs());

            // 添加回调函数，使m_Graphs随时保持与底层的graphs同步
            UnityEditor.Playables.Utility.graphCreated += OnGraphCreated;
            UnityEditor.Playables.Utility.destroyingGraph += OnDestroyingGraph;
        }

        void OnGraphCreated(PlayableGraph graph)
        {
            if (!m_Graphs.Contains(graph))
                m_Graphs.Add(graph);
        }

        void OnDestroyingGraph(PlayableGraph graph)
        {
            m_Graphs.Remove(graph);
        }

        void OnDisable()
        {
            UnityEditor.Playables.Utility.graphCreated -= OnGraphCreated;
            UnityEditor.Playables.Utility.destroyingGraph -= OnDestroyingGraph;
        }

        void OnGUI()
        {
            // Early out if there is no graphs.
            var selectedGraphs = GetGraphList();
            if (selectedGraphs.Count == 0)
            {
                ShowMessage("No PlayableGraph in the scene");
                return;
            }

            // 绘制下拉选择菜单，返回选中的PlayableGraph作为m_CurrentGraph
            GUILayout.BeginVertical();
            m_CurrentGraph = GetSelectedGraphInToolBar(selectedGraphs, m_CurrentGraph);
            GUILayout.EndVertical();

            if (!m_CurrentGraph.IsValid())
            {
                ShowMessage("Selected PlayableGraph is invalid");
                return;
            }

            // PlayableGraphVisualizer是用于绘制PlayableGraph的东西，类似于GraphView
            var graph = new PlayableGraphVisualizer(m_CurrentGraph);
            // Refresh函数会做两件事：一是把graph里的List<Node> m_Nodes给清空
            // 二是会去遍历PlayableGraph里的每一个PlayableOutput，为其建立Node对应的Node Tree
            graph.Refresh();

            if (graph.IsEmpty())
            {
                ShowMessage("Selected PlayableGraph is empty");
                return;
            }

            //Reingold和Tilford发明了一种图的layout算法
            if (m_Layout == null)
                // 简单的构造函数，默认从左到右排列
                m_Layout = new ReingoldTilford();

            // CalculateLayout是个虚函数，实际上调用的是ReingoldTilford类算法的CalculateLayout函数
            m_Layout.CalculateLayout(graph);

            var graphRect = new Rect(0, s_ToolbarHeight, position.width, position.height - s_ToolbarHeight);

            if (m_Renderer == null)
                m_Renderer = new DefaultGraphRenderer();

            m_Renderer.Draw(m_Layout, graphRect, m_GraphSettings);
        }

        // 有一个Runtime下的单例GraphVisualizerClient也维护了这个Graph，当
        // 当该graph为空时，会与m_Graphs同步
        private List<PlayableGraph> GetGraphList()
        {
            var selectedGraphs = new List<PlayableGraph>();
            // GraphVisualizerClient是一个静态的单例对象
            foreach (var clientGraph in GraphVisualizerClient.GetGraphs())
            {
                if (clientGraph.IsValid())
                    selectedGraphs.Add(clientGraph);
            }

            if (selectedGraphs.Count == 0)
                selectedGraphs = m_Graphs.ToList();

            return selectedGraphs;
        }

#region Custom_Menu

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Inspector"), m_GraphSettings.showInspector, ToggleInspector);
            menu.AddItem(new GUIContent("Legend"), m_GraphSettings.showLegend, ToggleLegend);
        }

        void ToggleInspector()
        {
            m_GraphSettings.showInspector = !m_GraphSettings.showInspector;
        }

        void ToggleLegend()
        {
            m_GraphSettings.showLegend = !m_GraphSettings.showLegend;
        }

#endregion
    }
}
