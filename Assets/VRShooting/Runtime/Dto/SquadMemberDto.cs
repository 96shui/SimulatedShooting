using UnityEngine;

namespace VRShooting.Common
{
    public enum SquadMemberRole
    {
        Player = 0,
        TeammateTwo,
        TeammateThree
    }

    public enum SquadMemberState
    {
        Normal = 0,
        Following,
        CoveringReload,
        SuppressingFire,
        ThrowingGrenade,
        MovingForward,
        HoldingPosition,
        Down
    }

    /// <summary>
    /// 队友成员 DTO。P1 仅占位，完整定义见 docs/接口文档/07-队友与战术指令.md。
    /// </summary>
    public readonly struct SquadMemberDto
    {
        public string MemberId { get; init; }
        public SquadMemberRole Role { get; init; }
        public SquadMemberState State { get; init; }
        public Vector3 WorldPosition { get; init; }
        public float Health { get; init; }
    }
}
