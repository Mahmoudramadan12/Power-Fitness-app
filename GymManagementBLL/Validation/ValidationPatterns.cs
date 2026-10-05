namespace GymManagementBLL.Validation
{
    public static class ValidationPatterns
    {
        // Unicode letters so Arabic (and any other) names are accepted.
        public const string Name = @"^[\p{L}][\p{L}\s.'\-]*$";
        public const string City = @"^[\p{L}][\p{L}\s.'\-]*$";
        public const string Street = @"^[\p{L}\p{N}][\p{L}\p{N}\s.,'\-/#]*$";
        public const string EgyptianMobile = @"^(010|011|012|015)\d{8}$";
        public const string BloodType = @"^(A|B|AB|O)[+-]$";
    }
}
