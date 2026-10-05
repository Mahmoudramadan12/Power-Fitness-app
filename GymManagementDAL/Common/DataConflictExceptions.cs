using System.Text.RegularExpressions;

namespace GymManagementDAL.Common
{
    /// <summary>Base type for database constraint violations the application can recover from.</summary>
    public abstract class DataConflictException : Exception
    {
        protected DataConflictException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>A unique index / primary key was violated (SQL Server errors 2601 and 2627).</summary>
    public sealed partial class DuplicateKeyException : DataConflictException
    {
        public string IndexName { get; }

        public DuplicateKeyException(string sqlMessage, Exception inner) : base(sqlMessage, inner)
        {
            var match = IndexNameRegex().Match(sqlMessage);
            IndexName = match.Success ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>True when the violated index/constraint name mentions <paramref name="column"/> (e.g. "Email").</summary>
        public bool Involves(string column) =>
            IndexName.Contains(column, StringComparison.OrdinalIgnoreCase) ||
            Message.Contains(column, StringComparison.OrdinalIgnoreCase);

        [GeneratedRegex(@"(?:unique index|constraint) '([^']+)'", RegexOptions.IgnoreCase)]
        private static partial Regex IndexNameRegex();
    }

    /// <summary>A foreign key blocked an insert/update/delete (SQL Server error 547).</summary>
    public sealed class ReferenceConstraintException(string sqlMessage, Exception inner)
        : DataConflictException(sqlMessage, inner);
}
