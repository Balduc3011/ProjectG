// Weapon sockets on Hand.R / Hand.L, the equipped weapon models (one per hand), the arrow between the hands, flying arrows and hit sparks.
// Weapon models are authored in socket space, so the sockets are made once in bind pose (Awake), before the PlayableGraph runs.
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    const float GripLift = 0.0625f;   // 1 skin px up from the hand bone = middle of the fist
    const float ArrowLife = 2f, SparkLife = 0.4f, SparkSize = 0.07f;
    const float KnuckleDrop = 0.27f;  // gauntlet knuckles: socket.position - socket.up * this

    [Tooltip("Weapon_Arrow.fbx (pivot = nock, +Z = tip)")]
    public GameObject arrowModel;
    [Tooltip("Arrow height above the fists, so it does not go through the arms")]
    public float arrowLift = 0.12f;
    public float arrowSpeed = 18f;
    public float arrowGravity = 3f;

    struct Flyer { public Transform t; public Vector3 velocity; public float life; }

    Transform socketR, socketL, handArrow;
    GameObject modelR, modelL;
    MotionHandle popR, popL;
    bool holdingArrow;
    readonly List<Flyer> arrows = new List<Flyer>();
    readonly List<Flyer> sparks = new List<Flyer>();
    static MaterialPropertyBlock sparkColor;

    public WeaponData Data { get; private set; }

    void Awake()
    {
        socketR = CreateSocket(Hand.Right);
        socketL = CreateSocket(Hand.Left);
    }

    Transform CreateSocket(Hand side)
    {
        Transform hand = FindDeep(transform, side == Hand.Right ? "Hand.R" : "Hand.L");
        if (!hand) { Debug.LogError($"{name}: bone {(side == Hand.Right ? "Hand.R" : "Hand.L")} not found"); return transform; }
        var socket = new GameObject(side == Hand.Right ? "Socket_R" : "Socket_L").transform;
        socket.SetParent(hand, false);
        socket.position = hand.position + transform.up * GripLift;
        socket.rotation = transform.rotation;
        return socket;
    }

    // both old models disappear, the new ones pop 0 -> 1 in their sockets (modelRight -> Socket_R, modelLeft -> Socket_L)
    public void Equip(WeaponData data)
    {
        CancelPop();
        if (modelR) Destroy(modelR);
        if (modelL) Destroy(modelL);
        HoldArrow(false);
        Data = data;
        if (!data) return;
        modelR = Spawn(data.modelRight, socketR, ref popR);
        modelL = Spawn(data.modelLeft, socketL, ref popL);
    }

    // models are authored in socket space: local 0 / identity / scale 1, no offset
    static GameObject Spawn(GameObject prefab, Transform socket, ref MotionHandle pop)
    {
        if (!prefab) return null;
        var model = Instantiate(prefab, socket);
        model.name = prefab.name;
        var t = model.transform;
        t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.zero;
        pop = LMotion.Create(Vector3.zero, Vector3.one, 0.2f).WithEase(Ease.OutBack).BindToLocalScale(t).AddTo(model);
        return model;
    }

    void CancelPop()
    {
        if (popR.IsPlaying()) popR.Cancel();
        if (popL.IsPlaying()) popL.Cancel();
    }

    public void HoldArrow(bool on)
    {
        if (on && !handArrow && arrowModel)
        {
            handArrow = Instantiate(arrowModel, transform).transform;
            handArrow.name = "Arrow_Hand";
        }
        holdingArrow = on;
        if (handArrow && handArrow.gameObject.activeSelf != on) handArrow.gameObject.SetActive(on);
        if (on) PlaceHandArrow();
    }

    // hand arrow off, a flying arrow leaves the bow hand along nock -> rest
    public void ReleaseArrow()
    {
        HoldArrow(false);
        if (!arrowModel) return;
        Vector3 up = transform.up * arrowLift;
        Vector3 nock = socketR.position + up, rest = socketL.position + up;
        Vector3 dir = (rest - nock).normalized;
        var t = Instantiate(arrowModel, rest, Quaternion.LookRotation(dir, transform.up)).transform;
        t.name = "Arrow_Flying";
        arrows.Add(new Flyer { t = t, velocity = dir * arrowSpeed, life = ArrowLife });
    }

    // 5 small blocks burst from the striking part of the weapon in that hand: blade / spear tip, or gauntlet knuckles
    public void HitSpark(Hand hand)
    {
        if (!Data) return;
        Transform socket = hand == Hand.Left ? socketL : socketR;
        Vector3 tip = Data.type == WeaponType.Fist
            ? socket.position - socket.up * KnuckleDrop
            : socket.position + socket.forward * (Data.type == WeaponType.Spear ? 1.3f : 0.9f);
        if (sparkColor == null)
        {
            sparkColor = new MaterialPropertyBlock();
            var c = new Color(1f, 0.85f, 0.3f);
            sparkColor.SetColor("_BaseColor", c); sparkColor.SetColor("_Color", c);
        }
        for (int i = 0; i < 5; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HitSpark";
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.SetPropertyBlock(sparkColor);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var t = go.transform;
            t.SetPositionAndRotation(tip, Random.rotation);
            t.localScale = Vector3.one * SparkSize;
            sparks.Add(new Flyer { t = t, velocity = Random.onUnitSphere * Random.Range(2f, 3.5f), life = SparkLife });
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = arrows.Count - 1; i >= 0; i--)
        {
            var a = arrows[i];
            a.life -= dt;
            if (a.life <= 0f || !a.t) { if (a.t) Destroy(a.t.gameObject); arrows.RemoveAt(i); continue; }
            a.velocity += Vector3.down * (arrowGravity * dt);
            a.t.SetPositionAndRotation(a.t.position + a.velocity * dt, Quaternion.LookRotation(a.velocity, Vector3.up));
            arrows[i] = a;
        }
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var s = sparks[i];
            s.life -= dt;
            if (s.life <= 0f || !s.t) { if (s.t) Destroy(s.t.gameObject); sparks.RemoveAt(i); continue; }
            s.t.position += s.velocity * dt;
            s.t.localScale = Vector3.one * (SparkSize * s.life / SparkLife);
            sparks[i] = s;
        }
    }

    void LateUpdate()
    {
        if (holdingArrow) PlaceHandArrow();
    }

    // between the hands, above the fists, pointing from the drawing hand to the bow hand
    void PlaceHandArrow()
    {
        if (!handArrow) return;
        Vector3 up = transform.up * arrowLift;
        Vector3 nock = socketR.position + up, rest = socketL.position + up;
        handArrow.SetPositionAndRotation(nock, Quaternion.LookRotation(rest - nock, transform.up));
    }

    void OnDestroy()
    {
        for (int i = 0; i < arrows.Count; i++) if (arrows[i].t) Destroy(arrows[i].t.gameObject);
        for (int i = 0; i < sparks.Count; i++) if (sparks[i].t) Destroy(sparks[i].t.gameObject);
    }

    public static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), name); if (r) return r; }
        return null;
    }
}
