// TextMesh above the head, always facing the camera. Line 1: weapon (or "→ <Weapon>" for 1 s after a swap),
// line 2: clip playing, line 3: countdown to the next swap.
using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class FighterLabel : MonoBehaviour
{
    public BlockBuddyFighter fighter;

    TextMesh text;
    Transform cam;
    WeaponType lastWeapon;
    AnimationClip lastClip;
    int lastSeconds = -1;
    bool lastFlash;

    void Awake()
    {
        text = GetComponent<TextMesh>();
        if (!fighter) fighter = GetComponentInParent<BlockBuddyFighter>();
        if (!text.font)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }
    }

    void LateUpdate()
    {
        if (!cam && Camera.main) cam = Camera.main.transform;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
        if (!fighter || !fighter.Current) return;

        // rebuild the string only when something shown changed
        var weapon = fighter.Current.type;
        var clip = fighter.Anim.Clip;
        int seconds = Mathf.CeilToInt(fighter.SwapIn);
        bool flash = fighter.ShowSwapLabel;
        if (weapon == lastWeapon && clip == lastClip && seconds == lastSeconds && flash == lastFlash) return;
        lastWeapon = weapon; lastClip = clip; lastSeconds = seconds; lastFlash = flash;
        text.text = (flash ? "→ " : "") + weapon + "\n" + (clip ? clip.name : "-") + "\nswap in " + seconds + "s";
    }
}
