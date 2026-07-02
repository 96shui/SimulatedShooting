namespace VRShooting.Common
{
    /// <summary>
    /// 弹药状态 DTO。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public readonly struct AmmoDto
    {
        public int CurrentMagazine { get; init; }
        public int ReserveAmmo { get; init; }
        public int MagazineCapacity { get; init; }
        public bool IsReloading { get; init; }

        public static AmmoDto Empty => default;
    }
}
