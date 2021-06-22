using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GraphVisualizer
{
    // 用于绘制PlayableGraph的类，类似GraphView
    public class PlayableGraphVisualizer : Graph
    {
        private PlayableGraph m_PlayableGraph;
        // 有一个List<Node>m_Nodes继承于Graph，存储了图中的所有Playable代表的可视化节点

        public PlayableGraphVisualizer(PlayableGraph playableGraph)
        {
            m_PlayableGraph = playableGraph;
        }

        protected override void Populate()
        {
            if (!m_PlayableGraph.IsValid())
                return;

            // 遍历每一个PlayableGraph里的PlayableOutput，感觉每一个PlayableOutput就像一个树的Root
            int outputs = m_PlayableGraph.GetOutputCount();
            for (int i = 0; i < outputs; i++)
            {
                // output类型为PlayableOutput
                var output = m_PlayableGraph.GetOutput(i);
                if(output.IsOutputValid())
                {
                    // 为其new一个PlayableOutputNode, 然后调用AddNodeHierarchy函数
                    // AddNodeHierarchy函数会从PlayableOutputNode开始，将其作为Root节点
                    // 深度递归的将其子节点都加入到m_Nodes里
                    AddNodeHierarchy(CreateNodeFromPlayableOutput(output));
                }
            }
        }

        // 这是一个重要的要实现的接口，它定义了图中节点如何决定父子关系的
        protected override IEnumerable<Node> GetChildren(Node node)
        {
            // 1. 如果是PlayableOutput，则其子节点为GetInputsFromPlayableOutputNode
            // 2. 如果是PlayableNode，则其子节点为GetInputsFromPlayableNode
            // 两个函数内部都是根据，Playable，new出来一系列的node，用于图形化表示

            // Children are the Playable Inputs.
            if (node is PlayableNode)
                return GetInputsFromPlayableNode((Playable)node.content);
            if(node is PlayableOutputNode)
                return GetInputsFromPlayableOutputNode((PlayableOutput)node.content);

            return new List<Node>();     
        }

        private List<Node> GetInputsFromPlayableNode(Playable h)
        {
            var inputs = new List<Node>();
            if (h.IsValid())
            {
                for (int port = 0; port < h.GetInputCount(); ++port)
                {
                    Playable playable = h.GetInput(port);
                    if (playable.IsValid())
                    {
                        float weight = h.GetInputWeight(port);
                        Node node = CreateNodeFromPlayable(playable, weight);
                        inputs.Add(node);
                    }
                }
            }
            return inputs;
        }

        private List<Node> GetInputsFromPlayableOutputNode(PlayableOutput h)
        {
            var inputs = new List<Node>();
            if (h.IsOutputValid())
            {            
                Playable playable = h.GetSourcePlayable();
                if (playable.IsValid())
                {
                    Node node = CreateNodeFromPlayable(playable, 1);
                    inputs.Add(node);
                }
            }
            return inputs;
        }

        private PlayableNode CreateNodeFromPlayable(Playable h, float weight)
        {
            var type = h.GetPlayableType();
            if (type == typeof(AnimationClipPlayable))
                return new AnimationClipPlayableNode(h, weight);
            if (type == typeof(AnimationLayerMixerPlayable))
                return new AnimationLayerMixerPlayableNode(h, weight);
            return new PlayableNode(h, weight);
        }

        private PlayableOutputNode CreateNodeFromPlayableOutput(PlayableOutput h)
        {
            return new PlayableOutputNode(h);
        }
    }
}
