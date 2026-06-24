namespace BRaVe_Portal.Models
{
    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public T data { get; set; }
        public string errorMessage { get; set; }
    }
}
