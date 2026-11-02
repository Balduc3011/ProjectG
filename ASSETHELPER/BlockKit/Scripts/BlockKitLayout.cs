// Data classes for a BlockKit layout json (Layouts/*.json) (parsed with JsonUtility).
using System;
using UnityEngine;

[Serializable]
public class BlockKitLayout
{
    [Serializable] public class Pose { public float[] position; public float rotationY; public Vector3 Position => new Vector3(position[0], position[1], position[2]); public Quaternion Rotation => Quaternion.Euler(0f, rotationY, 0f); }
    [Serializable] public class Placement { public string model; public string name; public Pose unity; public Pose fbx; }
    [Serializable] public class Spawn { public string name; public string role; public string target; public Pose unity; public Pose fbx; }

    public string note;
    public string root;
    public Placement[] placements;
    public Spawn[] spawns;

    public static BlockKitLayout Parse(string json) => JsonUtility.FromJson<BlockKitLayout>(json);
}
