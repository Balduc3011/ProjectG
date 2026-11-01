// One BlockBuddy doing a looping routine: Idle -> enter -> action -> exit -> Idle.
// Clips are played through a PlayableGraph with crossfades, so no Animator Controller is needed.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BlockGymActor : MonoBehaviour
{
    public enum Role { RingBoxer, BagBoxer, BenchPress, DumbbellCurl, Treadmill }

    [System.Serializable]
    public struct Step
    {
        public string clip;      // clip name, or "Box_Random" for random punches on top of the guard
        public float duration;
        public float fade;       // crossfade into this step (s)
        // time window inside the step during which the prop is in the hands (barFrom <= t < barUntil)
        public float barFrom, barUntil, dbFrom, dbUntil;
        public Step(string c, float d, float fade = 0.35f) { clip = c; duration = d; this.fade = fade; barFrom = barUntil = dbFrom = dbUntil = 0f; }
        public Step Bar(float from, float until) { barFrom = from; barUntil = until; return this; }
        public Step Dumbbells(float from, float until) { dbFrom = from; dbUntil = until; return this; }
        public bool HoldBar(float t) => t >= barFrom && t < barUntil;
        public bool HoldDumbbells(float t) => t >= dbFrom && t < dbUntil;
    }

    public Role role;
    public List<Step> steps = new List<Step>();
    public float blendTime = 0.35f;

    [Header("Props (set by BlockGymScene)")]
    public Transform barbell;
    public Transform dumbbellRight, dumbbellLeft;
    public BlockGymBag bag;

    Dictionary<string, AnimationClip> clips;
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    int activeSlot = -1;
    float blend = 1f, currentFade = 0.35f;
    int stepIndex; float stepTime;
    // random punches
    float punchTimer; string currentPunch; float punchTime; bool punchLanded;
    static readonly string[] punches = { "Box_JabLeft", "Box_CrossRight", "Box_HookLeft", "Box_UppercutRight" };

    Transform handR, handL;
    Vector3 rackPos; Quaternion rackRot;
    Vector3 dbRestR, dbRestL;
    bool wasHoldingBar, wasHoldingDb;

    public void Init(Role r, Dictionary<string, AnimationClip> clipsByName, float startOffset)
    {
        role = r; clips = clipsByName;
        steps = DefaultSteps(role);
        handR = FindDeep(transform, "Hand.R"); handL = FindDeep(transform, "Hand.L");
        if (barbell) { rackPos = barbell.position; rackRot = barbell.rotation; }
        if (dumbbellRight) dbRestR = dumbbellRight.position;
        if (dumbbellLeft) dbRestL = dumbbellLeft.position;

        var animator = GetComponent<Animator>();
        if (!animator) animator = gameObject.AddComponent<Animator>();
        graph = PlayableGraph.Create(name + " routine");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        var output = AnimationPlayableOutput.Create(graph, "anim", animator);
        output.SetSourcePlayable(mixer);
        graph.Play();

        // desync the characters: skip ahead by startOffset
        stepIndex = 0; stepTime = startOffset;
        while (steps.Count > 0 && stepTime >= steps[stepIndex].duration) { stepTime -= steps[stepIndex].duration; stepIndex = (stepIndex + 1) % steps.Count; }
        StartStep(0f);
    }

    // Idle <-> boxing / running only need a crossfade. Lying down on the bench and picking the
    // dumbbells up are real transitions, so they are clips of their own.
    public static List<Step> DefaultSteps(Role role)
    {
        var s = new List<Step>();
        switch (role)
        {
            case Role.RingBoxer:
            case Role.BagBoxer:
                s.Add(new Step("Idle", 2.5f, 0.6f)); s.Add(new Step("Box_Random", 9f, 0.6f));
                break;
            case Role.BenchPress:
                s.Add(new Step("Idle", 2f)); s.Add(new Step("Bench_LieDown", 3.8f).Bar(3.2f, 99f));
                s.Add(new Step("Bench_Press", 3.2f).Bar(0f, 99f)); s.Add(new Step("Bench_GetUp", 3.7f).Bar(0f, 0.6f)); s.Add(new Step("Idle", 1.5f));
                break;
            case Role.DumbbellCurl:
                s.Add(new Step("Idle", 2.2f)); s.Add(new Step("Curl_PickUp", 1.8f).Dumbbells(0.9f, 99f));
                s.Add(new Step("Curl_Loop", 7f).Dumbbells(0f, 99f)); s.Add(new Step("Curl_PutDown", 1.8f).Dumbbells(0f, 0.9f)); s.Add(new Step("Idle", 1f));
                break;
            case Role.Treadmill:
                s.Add(new Step("Idle", 2f, 1.0f)); s.Add(new Step("Run", 8f, 1.0f));
                break;
        }
        return s;
    }

    void StartStep(float fade)
    {
        var step = steps[stepIndex];
        if (step.clip == "Box_Random") { punchTimer = 0f; currentPunch = null; Play("Box_Guard", fade, stepTime); }
        else Play(step.clip, fade, stepTime);
    }

    void Play(string clipName, float fade, float startTime = 0f)
    {
        if (clips == null || !clips.TryGetValue(clipName, out var clip)) { Debug.LogWarning($"BlockGym: missing clip {clipName}"); return; }
        int slot = (activeSlot + 1) % 2;
        var old = mixer.GetInput(slot);
        if (old.IsValid()) { mixer.DisconnectInput(slot); old.Destroy(); }
        var p = AnimationClipPlayable.Create(graph, clip);
        p.SetTime(startTime); p.SetApplyFootIK(false);
        mixer.ConnectInput(slot, p, 0);
        if (activeSlot < 0 || fade <= 0f) { mixer.SetInputWeight(slot, 1f); if (activeSlot >= 0) mixer.SetInputWeight(activeSlot, 0f); blend = 1f; }
        else { mixer.SetInputWeight(slot, 0f); blend = 0f; }
        activeSlot = slot;
    }

    void Update()
    {
        if (steps.Count == 0 || !graph.IsValid()) return;
        float dt = Time.deltaTime;
        stepTime += dt;
        var step = steps[stepIndex];
        if (stepTime >= step.duration)
        {
            stepTime -= step.duration;
            stepIndex = (stepIndex + 1) % steps.Count;
            currentFade = steps[stepIndex].fade > 0f ? steps[stepIndex].fade : blendTime;
            StartStep(currentFade);
            step = steps[stepIndex];
        }
        if (step.clip == "Box_Random") UpdateRandomPunch(dt);
        // crossfade
        if (blend < 1f)
        {
            blend = Mathf.Min(1f, blend + dt / Mathf.Max(0.01f, currentFade));
            float k = blend * blend * (3f - 2f * blend);
            mixer.SetInputWeight(activeSlot, k);
            mixer.SetInputWeight((activeSlot + 1) % 2, 1f - k);
        }
    }

    // guard with a random punch every 1.3 s (the punch clips start and end in the guard pose)
    void UpdateRandomPunch(float dt)
    {
        punchTimer -= dt;
        if (currentPunch != null)
        {
            punchTime += dt;
            if (!punchLanded && punchTime > 0.3f && bag) { bag.Punch(currentPunch == "Box_CrossRight" || currentPunch == "Box_UppercutRight" ? 1.6f : 1f); punchLanded = true; }
            if (punchTime > 1.1f) { currentPunch = null; currentFade = 0.15f; Play("Box_Guard", 0.15f, stepTime); }
        }
        if (punchTimer <= 0f)
        {
            punchTimer = 1.3f;
            if (Random.value < 0.8f) { currentPunch = punches[Random.Range(0, punches.Length)]; punchTime = 0f; punchLanded = false; currentFade = 0.12f; Play(currentPunch, 0.12f); }
        }
    }

    void LateUpdate()
    {
        if (steps.Count == 0) return;
        var step = steps[stepIndex];
        bool holdBar = step.HoldBar(stepTime), holdDb = step.HoldDumbbells(stepTime);
        if (barbell)
        {
            if (holdBar && handR && handL)
            {
                Vector3 mid = (Grip(handR) + Grip(handL)) * 0.5f;
                barbell.position = wasHoldingBar ? Vector3.Lerp(barbell.position, mid, 0.6f) : mid;
                barbell.rotation = Quaternion.Slerp(barbell.rotation, transform.rotation, 0.5f);
            }
            else
            {
                barbell.position = Vector3.Lerp(barbell.position, rackPos, 0.25f);
                barbell.rotation = Quaternion.Slerp(barbell.rotation, rackRot, 0.25f);
            }
            wasHoldingBar = holdBar;
        }
        if (dumbbellRight && dumbbellLeft)
        {
            if (holdDb && handR && handL)
            {
                dumbbellRight.position = Vector3.Lerp(dumbbellRight.position, Grip(handR), wasHoldingDb ? 0.7f : 1f);
                dumbbellLeft.position = Vector3.Lerp(dumbbellLeft.position, Grip(handL), wasHoldingDb ? 0.7f : 1f);
                dumbbellRight.rotation = dumbbellLeft.rotation = transform.rotation;
            }
            else
            {
                dumbbellRight.position = Vector3.Lerp(dumbbellRight.position, dbRestR, 0.3f);
                dumbbellLeft.position = Vector3.Lerp(dumbbellLeft.position, dbRestL, 0.3f);
            }
            wasHoldingDb = holdDb;
        }
    }

    // Hand.* sits at the wrist; the grip is a little further along the bone (LowerArm -> Hand)
    const float GripOffset = 0.06f;
    static Vector3 Grip(Transform hand) => hand.position + (hand.position - hand.parent.position).normalized * GripOffset;

    void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }

    public static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), name); if (r) return r; }
        return null;
    }
}
