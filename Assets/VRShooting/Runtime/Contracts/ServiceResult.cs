namespace VRShooting.Contracts
{
    /// <summary>
    /// 通用服务返回结构。参见 docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    public readonly struct ServiceResult<T>
    {
        public bool Success { get; init; }
        public T Data { get; init; }
        public ErrorCode ErrorCode { get; init; }
        public string Message { get; init; }

        public static ServiceResult<T> Ok(T data, string message = "")
        {
            return new ServiceResult<T>
            {
                Success = true,
                Data = data,
                ErrorCode = ErrorCode.None,
                Message = message ?? string.Empty
            };
        }

        public static ServiceResult<T> Fail(ErrorCode errorCode, string message = "", T data = default)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Data = data,
                ErrorCode = errorCode,
                Message = message ?? string.Empty
            };
        }
    }
}
