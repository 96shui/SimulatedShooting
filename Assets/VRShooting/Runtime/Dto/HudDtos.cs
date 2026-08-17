using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRShooting.Common
{
    public readonly struct HudDto
    {
        public string SessionId { get; init; }
        public TrainingMode Mode { get; init; }
        public HudType HudType { get; init; }
        public AmmoDto Ammo { get; init; }
        public PlayerStatusDto Player { get; init; }
        public MiniMapDto MiniMap { get; init; }
        public IReadOnlyList<HudTextLineDto> TextLines { get; init; }
        public IReadOnlyList<HudPromptDto> Prompts { get; init; }
        public bool CanShoot { get; init; }
        public WeaponFireSequenceStateDto? FireSequence { get; init; }

        public static HudDto Empty => new HudDto
        {
            SessionId = string.Empty,
            Mode = TrainingMode.None,
            HudType = HudType.Zeroing,
            TextLines = Array.Empty<HudTextLineDto>(),
            Prompts = Array.Empty<HudPromptDto>()
        };
    }

    public readonly struct HudTextLineDto
    {
        public string Key { get; init; }
        public string Label { get; init; }
        public string Value { get; init; }
        public HudSeverity Severity { get; init; }
    }

    public readonly struct HudPromptDto
    {
        public string PromptId { get; init; }
        public string Text { get; init; }
        public bool IsInteractive { get; init; }
        public bool IsEnabled { get; init; }
    }

    public readonly struct MiniMapDto
    {
        public bool Visible { get; init; }
        public string MapId { get; init; }
        public IReadOnlyList<MapMarkerDto> Markers { get; init; }
        public IReadOnlyList<MapAreaDto> Areas { get; init; }

        public static MiniMapDto Hidden => new MiniMapDto
        {
            Visible = false,
            MapId = string.Empty,
            Markers = Array.Empty<MapMarkerDto>(),
            Areas = Array.Empty<MapAreaDto>()
        };
    }

    public readonly struct MapMarkerDto
    {
        public string MarkerId { get; init; }
        public MarkerType Type { get; init; }
        public Vector2 NormalizedPosition { get; init; }
        public string Label { get; init; }
    }

    public readonly struct MapAreaDto
    {
        public string AreaId { get; init; }
        public string Label { get; init; }
        public Rect NormalizedRect { get; init; }
        public HudSeverity Severity { get; init; }
    }
}
