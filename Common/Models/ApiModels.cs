namespace Employee_History.Common.Models
{
    /// <summary>
    /// Standard response envelope for confirmations and errors:
    /// <c>{ "success": bool, "message": string }</c>.
    /// </summary>
    public class ApiMessage
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public ApiMessage() { }
        public ApiMessage(string message, bool success = true) { Message = message; Success = success; }
    }

    /// <summary>
    /// Standard page envelope: <c>{ data, totalCount, page, pageSize }</c>.
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
