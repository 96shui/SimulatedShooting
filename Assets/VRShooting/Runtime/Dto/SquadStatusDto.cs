using System;
using System.Collections.Generic;

namespace VRShooting.Common
{
    /// <summary>
    /// 小队状态 DTO。参见 docs/接口文档/07-队友与战术指令.md。
    /// </summary>
    public readonly struct SquadStatusDto
    {
        public IReadOnlyList<SquadMemberDto> Members { get; init; }
        public bool CommandMenuAvailable { get; init; }

        public static SquadStatusDto Empty => new SquadStatusDto
        {
            Members = Array.Empty<SquadMemberDto>(),
            CommandMenuAvailable = false
        };
    }
}
