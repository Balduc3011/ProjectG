// Demo "brain": loops the current weapon's routine, fires hit / arrow events from the WeaponClip times,
// and swaps to the next weapon every swapInterval seconds (only while in Idle or Run, never in the middle of an attack).
// Routine: Idle 2s -> Run 2.5s (one circle back to the start point) -> attacks[0] -> attacks[1] -> Idle 1s -> skill -> Idle 1.5s
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(BlockBuddyAnimPlayer), typeof(WeaponHolder))]
public class BlockBuddyFighter : MonoBehaviour
{
    enum StepKind { Idle, Run, Attack, Skill }

    struct Step
    {
        public StepKind kind;
        public WeaponClip clip;
        public float duration;
        public Step(StepKind k, WeaponClip c, float d) { kind = k; clip = c; duration = d; }
        public bool IsAction => kind == StepKind.Attack || kind == StepKind.Skill;
    }

    const float IdleRunFade = 0.25f, IntoActionFade = 0.1f, BackToIdleFade = 0.2f;
    const float SwapFade = 0.3f, EquipAt = 0.12f, SwapLabelTime = 1f;
    const float RunRadius = 1.2f, RunTime = 2.5f, TurnBackTime = 0.3f;

    [Tooltip("Swap order: Sword -> Bow -> Spear -> Fist -> Sword ...")]
    public WeaponData[] weapons = new WeaponData[0];
    public WeaponType startWeapon;
    [Tooltip("Skip this far into the routine so the characters are not in sync")]
    public float startOffset;
    public float swapInterval = 30f;

    BlockBuddyAnimPlayer anim;
    WeaponHolder holder;
    readonly List<Step> steps = new List<Step>();
    int stepIndex;
    float stepTime, prevClipTime;
    bool returning;               // one-shot step already fading back to Idle
    Vector3 homePos; Quaternion homeRot;
    float runFrom, runProgress;   // circle progress 0..1; runFrom > 0 when a swap stopped the last Run half way
    float turnTime = -1f; Quaternion turnFrom;
    float swapClock, equipTimer, swapLabelTimer;

    public WeaponData Current { get; private set; }
    public BlockBuddyAnimPlayer Anim => anim;
    public float SwapIn => Mathf.Max(0f, swapInterval - swapClock);
    public bool ShowSwapLabel => swapLabelTimer > 0f;

    void Awake()
    {
        anim = GetComponent<BlockBuddyAnimPlayer>();
        holder = GetComponent<WeaponHolder>();
    }

    void Start()
    {
        homePos = transform.position; homeRot = transform.rotation;
        Current = Find(startWeapon);
        if (!Current) { Debug.LogError($"{name}: no WeaponData for {startWeapon}"); enabled = false; return; }
        holder.Equip(Current);   // sockets were made in WeaponHolder.Awake, the graph starts with the first Play below
        BuildRoutine();
        stepIndex = 0; stepTime = startOffset;
        StartStep(StepKind.Idle);
    }

    void BuildRoutine()
    {
        var d = Current;
        steps.Clear();
        steps.Add(new Step(StepKind.Idle, d.idle, 2f));
        steps.Add(new Step(StepKind.Run, d.run, RunTime));
        steps.Add(Action(StepKind.Attack, d.attacks[0]));
        steps.Add(Action(StepKind.Attack, d.attacks[1 % d.attacks.Length]));   // Bow has one attack: it shoots twice
        steps.Add(new Step(StepKind.Idle, d.idle, 1f));
        steps.Add(Action(StepKind.Skill, d.skill));
        steps.Add(new Step(StepKind.Idle, d.idle, 1.5f));
    }

    static Step Action(StepKind kind, WeaponClip c) => new Step(kind, c, c.clip.length);

    // the rest of a cut-short Run keeps at least 2 fades long, so its fade in / out never overlap
    float RunDuration => Mathf.Max(2f * IdleRunFade, RunTime * (1f - runFrom));
    float Duration(Step s) => s.kind == StepKind.Run ? RunDuration : s.duration;

    void StartStep(StepKind prev)
    {
        var s = steps[stepIndex];
        returning = false; prevClipTime = -1f;
        switch (s.kind)
        {
            case StepKind.Idle:
                // after an attack / skill the Idle is already playing (fade started 0.2 s before the clip end)
                if (anim.Clip != s.clip.clip) anim.Play(s.clip.clip, prev == StepKind.Run ? IdleRunFade : BackToIdleFade);
                if (prev == StepKind.Run) { turnFrom = transform.rotation; turnTime = 0f; }
                break;
            case StepKind.Run:
                anim.Play(s.clip.clip, IdleRunFade);
                break;
            default:
                anim.Play(s.clip.clip, IntoActionFade);
                break;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (SwapKeyPressed()) SwapNow();

        if (equipTimer > 0f) { equipTimer -= dt; if (equipTimer <= 0f) holder.Equip(Current); }
        if (swapLabelTimer > 0f) swapLabelTimer -= dt;
        swapClock += dt;
        var kind = steps[stepIndex].kind;
        if (swapClock >= swapInterval && equipTimer <= 0f && !anim.IsFading && (kind == StepKind.Idle || kind == StepKind.Run)) SwapWeapon();

        stepTime += dt;
        while (stepTime >= Duration(steps[stepIndex]))
        {
            var prev = steps[stepIndex];
            stepTime -= Duration(prev);
            if (prev.kind == StepKind.Run) { runFrom = 0f; transform.SetPositionAndRotation(homePos, transform.rotation); }
            stepIndex = (stepIndex + 1) % steps.Count;
            StartStep(prev.kind);
        }

        var step = steps[stepIndex];
        if (step.kind == StepKind.Run) UpdateRun();
        if (turnTime >= 0f)
        {
            turnTime += dt;
            transform.rotation = Quaternion.Slerp(turnFrom, homeRot, Mathf.Clamp01(turnTime / TurnBackTime));
            if (turnTime >= TurnBackTime) turnTime = -1f;
        }
        if (step.IsAction)
        {
            var next = steps[(stepIndex + 1) % steps.Count];
            if (!returning && next.kind == StepKind.Idle && stepTime >= step.duration - BackToIdleFade)
            {
                returning = true;
                anim.Play(next.clip.clip, BackToIdleFade);
            }
        }
        UpdateEvents(step);
    }

    // one circle (radius RunRadius) that starts and ends at the home point, heading along the tangent.
    // It starts straight ahead and curves to the right.
    void UpdateRun()
    {
        runProgress = Mathf.Min(1f, runFrom + (1f - runFrom) * stepTime / RunDuration);
        float a = runProgress * 2f * Mathf.PI;
        Vector3 right = homeRot * Vector3.right, fwd = homeRot * Vector3.forward;
        Vector3 center = homePos + right * RunRadius;
        Vector3 pos = center + (fwd * Mathf.Sin(a) - right * Mathf.Cos(a)) * RunRadius;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd * Mathf.Cos(a) + right * Mathf.Sin(a), homeRot * Vector3.up));
    }

    // compare the clip time of the last frame and this frame with hits[] / arrows[]
    void UpdateEvents(Step step)
    {
        bool hold = false;
        if (step.IsAction && anim.Clip == step.clip.clip)
        {
            float t = anim.Time;
            var hits = step.clip.hits;
            var hitHands = step.clip.hitHands;
            for (int i = 0; i < hits.Length; i++)
                if (prevClipTime < hits[i] && t >= hits[i]) holder.HitSpark(i < hitHands.Length ? hitHands[i] : Hand.Right);
            var arrows = step.clip.arrows;
            for (int i = 0; i < arrows.Length; i++)
            {
                if (t >= arrows[i].x && t < arrows[i].y) hold = true;
                if (prevClipTime < arrows[i].y && t >= arrows[i].y) holder.ReleaseArrow();
            }
            prevClipTime = t;
        }
        holder.HoldArrow(hold);
    }

    void SwapWeapon()
    {
        var old = Current;
        var next = weapons[(IndexOf(old) + 1) % weapons.Length];
        // cut a Run short: stand still here, the next Run finishes the circle back to the home point
        if (steps[stepIndex].kind == StepKind.Run) runFrom = runProgress;
        Current = next;
        anim.Play(next.idle.clip, SwapFade);
        equipTimer = EquipAt;   // the model swaps 0.12 s into the fade
        BuildRoutine();
        stepIndex = 0; stepTime = 0f; returning = false; prevClipTime = -1f;
        swapClock = 0f; swapLabelTimer = SwapLabelTime;
        Debug.Log($"{name}: {old.type} -> {next.type} at {Time.time:F1}s");
    }

    // swap at the next Idle / Run
    [ContextMenu("Swap now")]
    public void SwapNow() { swapClock = swapInterval; }

    static bool SwapKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.N);
#endif
    }

    WeaponData Find(WeaponType type)
    {
        for (int i = 0; i < weapons.Length; i++) if (weapons[i] && weapons[i].type == type) return weapons[i];
        return null;
    }

    int IndexOf(WeaponData data)
    {
        for (int i = 0; i < weapons.Length; i++) if (weapons[i] == data) return i;
        return -1;
    }
}
