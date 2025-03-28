using System;

namespace Crystallic;

[Flags] // Marked so bitwise operations can be performed
public enum Part
{
    /// <summary>
    ///     Accurate
    /// </summary>
    Hips = 0,

    /// <summary>
    ///     Accurate
    /// </summary>
    RightUpperLeg = 1,

    /// <summary>
    ///     Accurate
    /// </summary>
    LeftUpperLeg = 2,

    /// <summary>
    ///     Accurate
    /// </summary>
    RightLowerLeg = 3,

    /// <summary>
    ///     Accurate
    /// </summary>
    LeftLowerLeg = 4,

    /// <summary>
    ///     This is Spine1
    /// </summary>
    Spine = 7,

    /// <summary>
    ///     This is Spine
    /// </summary>
    Chest = 8,

    /// <summary>
    ///     Just below the head, moves with Spine1
    /// </summary>
    Neck = 10,

    /// <summary>
    ///     Accurate
    /// </summary>
    Head = 11,

    /// <summary>
    ///     Used, but it's inaccurate, 180 degrees inverted relative to Unity. There is no proper left shoulder bone, but my
    ///     UtilsOld file contains a GetChildByNameRecursive method if you need to access the transform itself.
    /// </summary>
    RightShoulder = 13,

    /// <summary>
    ///     Accurate
    /// </summary>
    RightUpperArm = 14,

    /// <summary>
    ///     Accurate
    /// </summary>
    LeftUpperArm = 15,

    /// <summary>
    ///     Accurate
    /// </summary>
    RightLowerArm = 16,

    /// <summary>
    ///     Accurate
    /// </summary>
    LeftLowerArm = 17
}