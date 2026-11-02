// One weapon's clip set. Swapping weapons = swapping this data on the same clip player
// (the idea of an Animator Override Controller, done with Playables).
// BlockBuddyWeapons > Create Weapon Data fills WeaponData_<Type>.asset from Data/BlockBuddyWeapons_Clips.json.
using UnityEngine;

public enum WeaponType { Sword, Bow, Spear, Fist }
public enum Hand { Right, Left, Both }

[System.Serializable]
public class WeaponClip
{
    public AnimationClip clip;
    public float[] hits = new float[0];       // s from clip start: the blow lands
    public Hand[] hitHands = new Hand[0];     // hand that lands hits[i] (Right = Socket_R, Left = Socket_L)
    public Vector2[] arrows = new Vector2[0]; // x = nock, y = release (s from clip start)
}

[CreateAssetMenu(menuName = "BlockBuddyWeapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public WeaponType type;
    public Hand hand;
    public GameObject modelRight;   // goes into Socket_R, may be empty
    public GameObject modelLeft;    // goes into Socket_L, may be empty
    public WeaponClip idle, run;
    public WeaponClip[] attacks = new WeaponClip[0];
    public WeaponClip skill;
}
