using System.Globalization;

namespace BRaVe_Portal.Models.DTOs
{
    public class TenantAndRolesDto
    {
        //public bool HasData { get; set; }
        public int TenantId { get; set; }
        public IReadOnlyList<int> Roles { get; set; } = Array.Empty<int>();

        public UserRoleInfo UserProfile { get; set; } 

        public string TenantToString()
        {
            return TenantId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
