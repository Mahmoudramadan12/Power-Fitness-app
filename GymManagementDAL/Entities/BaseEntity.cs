namespace GymManagementDAL.Entities
{
    /// <summary>
    /// Common audit columns. <see cref="CreatedAt"/> / <see cref="UpdatedAt"/> are stamped
    /// automatically by <c>GymDbContext.SaveChangesAsync</c>, so services never set them by hand.
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
