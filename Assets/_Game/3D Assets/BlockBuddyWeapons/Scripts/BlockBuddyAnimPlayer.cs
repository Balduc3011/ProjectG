// Plays one AnimationClip at a time on the Animator through a PlayableGraph: 2-input mixer, smoothstep crossfade
// (same as BlockGymActor.Play). No Animator Controller. Looping clips run freely; one-shot clips hold their last frame.
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class BlockBuddyAnimPlayer : MonoBehaviour
{
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    AnimationClipPlayable current;
    int activeSlot = -1;
    float blend = 1f, fadeTime;

    public AnimationClip Clip { get; private set; }
    public float Time => current.IsValid() ? (float)current.GetTime() : 0f;
    public float Length => Clip ? Clip.length : 0f;
    public bool IsDone => Clip && !Clip.isLooping && Time >= Length;
    public bool IsFading => blend < 1f;

    // Created on the first Play, so anything that needs the bind pose (weapon sockets) must be set up before that.
    void CreateGraph()
    {
        var animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        graph = PlayableGraph.Create(name + " anim");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        var output = AnimationPlayableOutput.Create(graph, "anim", animator);
        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    public void Play(AnimationClip clip, float fade)
    {
        if (!clip) return;
        if (!graph.IsValid()) CreateGraph();
        int slot = (activeSlot + 1) % 2;
        var old = mixer.GetInput(slot);
        if (old.IsValid()) { mixer.DisconnectInput(slot); old.Destroy(); }
        current = AnimationClipPlayable.Create(graph, clip);
        current.SetApplyFootIK(false);
        mixer.ConnectInput(slot, current, 0);
        if (activeSlot < 0 || fade <= 0f) { mixer.SetInputWeight(slot, 1f); if (activeSlot >= 0) mixer.SetInputWeight(activeSlot, 0f); blend = 1f; }
        else { mixer.SetInputWeight(slot, 0f); blend = 0f; }
        activeSlot = slot; fadeTime = fade; Clip = clip;
    }

    void Update()
    {
        if (!graph.IsValid()) return;
        // one-shot clip finished: stop its time at the end so it holds the last frame
        if (IsDone && current.GetSpeed() > 0) { current.SetTime(Length); current.SetSpeed(0); }
        if (blend < 1f)
        {
            blend = Mathf.Min(1f, blend + UnityEngine.Time.deltaTime / Mathf.Max(0.01f, fadeTime));
            float k = blend * blend * (3f - 2f * blend);
            mixer.SetInputWeight(activeSlot, k);
            mixer.SetInputWeight((activeSlot + 1) % 2, 1f - k);
        }
    }

    void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
}
