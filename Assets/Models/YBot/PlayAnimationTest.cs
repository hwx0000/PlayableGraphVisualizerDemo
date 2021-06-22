using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

// 确保同物体上有Animator组件，但是不需要给该Animator赋值Animator Controller
[RequireComponent(typeof(Animator))]
public class PlayAnimationTest : MonoBehaviour
{
    public AnimationClip clip;//注意这里要像使用Animation组件一样，从外部指定AnimationClip进来
    PlayableGraph playableGraph;

    void Start()
    {
        // 创建一个PlayableGraph
        playableGraph = PlayableGraph.Create("PlayAnimationSample");
        // 基于playableGraph创建一个动画类型的Output节点，名字是Animation，目标对象是物体上的Animator组件
        AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", GetComponent<Animator>());
        // 基于本组件已经索引好的AnimationClip创建一个AnimationClipPlayable
        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        // 将playable连接到output
        playableOutput.SetSourcePlayable(clipPlayable);
        // 播放这个graph
        playableGraph.Play();
    }

    void OnDisable()
    {
        // 销毁所有的Playables和PlayableOutputs
        playableGraph.Destroy();
    }
}
