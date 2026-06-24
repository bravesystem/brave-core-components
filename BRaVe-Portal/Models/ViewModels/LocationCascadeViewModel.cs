namespace BRaVe_Portal.Models.ViewModels
{
    public class LocationCascadeViewModel
    {
        public List<AdministrativeLevel> AdminLevels { get; set; } = new();
        public Dictionary<int, List<Location>> LocationsByLevel { get; set; } = new();
        public Dictionary<int, int?> SelectedLocationIds { get; set; } = new();
        public string? AddressInfo { get; set; }

        public string getSiteFullAddress()
        {
            var segments = new List<string>();

            foreach (var level in AdminLevels.OrderBy(x => x.Id))
            {
                if (!SelectedLocationIds.TryGetValue(level.Id, out var selectedLocationId) || !selectedLocationId.HasValue)
                {
                    continue;
                }

                if (!LocationsByLevel.TryGetValue(level.Id, out var levelLocations))
                {
                    continue;
                }

                var selectedLocation = levelLocations.FirstOrDefault(x => x.Id == selectedLocationId.Value);
                if (selectedLocation == null)
                {
                    continue;
                }

                var levelName = string.IsNullOrWhiteSpace(level.LevelName) ? $"AdminLevel{level.Id}" : level.LevelName!;
                segments.Add($"{levelName}:{selectedLocation.LocationName}");
            }

            var cleanAddressInfo = string.IsNullOrWhiteSpace(AddressInfo) ? "-" : AddressInfo.Trim();
            segments.Add($"Address: {cleanAddressInfo}");

            return segments.Count > 1 ? string.Join(" / ", segments) : "-";
        }

        public void setAdminArea(AdminArea adminArea)
        {
            if (adminArea != null) {
                SelectedLocationIds = adminArea.SelectedLocationIds;
                AddressInfo = adminArea.AddressInfo;
            }
        }

        public AdminArea getAdminArea()
        {
            return new AdminArea
            {
                SelectedLocationIds= SelectedLocationIds,
                AddressInfo = AddressInfo
            };
        }
    }
}
