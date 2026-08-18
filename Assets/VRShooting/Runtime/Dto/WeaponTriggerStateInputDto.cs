namespace VRShooting.Common
{
    /// <summary>
    /// Trigger 模拟量与边沿状态。参见 docs/接口文档/06-武器与弹药服务.md。
    /// </summary>
    public readonly struct WeaponTriggerStateInputDto
    {
        public string SessionId { get; init; }
        public float Value01 { get; init; }
        public bool Pressed { get; init; }
        public bool Held { get; init; }
        public bool Released { get; init; }
    }
}
