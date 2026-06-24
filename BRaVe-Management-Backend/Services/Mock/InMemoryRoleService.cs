//using BRaVe_Management_Backend.Interfaces;

//namespace BRaVe_Management_Backend.Services.Mock
//{
//    public class InMemoryRoleService : IRoleService
//    {
//        private static readonly Dictionary<string, string[]> _roles = new(StringComparer.OrdinalIgnoreCase)
//        {
//            ["aguillaume@iom.int"] = ["Admin", "DA"],
//            ["vndwiga@iom.int"] = ["Admin", "PM"]
//        };

//        public Task<IReadOnlyList<string>> GetRolesAsync(string email, CancellationToken ct = default)
//        => Task.FromResult<IReadOnlyList<string>>(_roles.TryGetValue(email, out var r) ? r : ["NoAuth"]); //Array.Empty<string>());

//    }
//}
