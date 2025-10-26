namespace MatchingService.Domain.ValueObjects
{
    /// <summary>
    /// 地理位置值对象
    /// </summary>
    public record Location
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }
        public string? Province { get; init; }
        public string? Country { get; init; }

        public Location(double latitude, double longitude, string? address = null, string? city = null, string? province = null, string? country = null)
        {
            Latitude = latitude;
            Longitude = longitude;
            Address = address;
            City = city;
            Province = province;
            Country = country;
        }

        /// <summary>
        /// 计算两点之间的距离（单位：公里）
        /// </summary>
        public double CalculateDistance(Location other)
        {
            const double earthRadius = 6371; // 地球半径（公里）
            
            var lat1Rad = ToRadians(Latitude);
            var lat2Rad = ToRadians(other.Latitude);
            var deltaLatRad = ToRadians(other.Latitude - Latitude);
            var deltaLonRad = ToRadians(other.Longitude - Longitude);

            var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadius * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        /// <summary>
        /// 检查是否在指定范围内
        /// </summary>
        public bool IsWithinRange(Location other, double maxDistanceKm)
        {
            return CalculateDistance(other) <= maxDistanceKm;
        }
    }
}
