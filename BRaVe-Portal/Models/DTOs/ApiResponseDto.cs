namespace BRaVe_Portal.Models.DTOs
{
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; } = default!;
    }
}
