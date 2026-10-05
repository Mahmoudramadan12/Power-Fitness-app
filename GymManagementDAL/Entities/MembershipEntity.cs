namespace GymManagementDAL.Entities
{
    /// <summary>
    /// A member's subscription to a plan.
    /// <para>
    /// <see cref="BaseEntity.CreatedAt"/> is persisted as the <c>StartDate</c> column: it is the day the
    /// membership starts, which can be in the future for pre-paid memberships.
    /// A membership is active while <c>StartDate &lt;= now &lt; EndDate</c>.
    /// </para>
    /// </summary>
    public class MembershipEntity : BaseEntity
    {
        public DateTime EndDate { get; set; }

        /// <summary>Price charged at the time of purchase (plan prices can change later).</summary>
        public decimal PricePaid { get; set; }

        public int MemberId { get; set; }
        public MemberEntity Member { get; set; } = default!;

        public int PlanId { get; set; }
        public PlanEntity Plan { get; set; } = default!;

        /// <summary>Alias that makes call sites read naturally.</summary>
        public DateTime StartDate => CreatedAt;

        public bool IsActiveAt(DateTime now) => StartDate <= now && EndDate > now;
        public bool IsUpcomingAt(DateTime now) => StartDate > now;
        public bool IsExpiredAt(DateTime now) => EndDate <= now;
    }
}
