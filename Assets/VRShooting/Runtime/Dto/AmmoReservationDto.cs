namespace VRShooting.Common
{
    /// <summary>
    /// 弹药预留事务。P2 起射两发必须原子预留。
    /// </summary>
    public readonly struct AmmoReservationDto
    {
        public string SessionId { get; init; }
        public string ReservationId { get; init; }
        public int ReservedAmount { get; init; }
        public int RemainingReservedAmount { get; init; }

        public static AmmoReservationDto Empty => default;
    }
}
