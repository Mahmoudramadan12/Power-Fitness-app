using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GymManagementBLL.Common
{
    public static class EnumDisplay
    {
        /// <summary>The <c>[Display(Name)]</c> of an enum value, falling back to its identifier.</summary>
        public static string Name(Enum value) =>
            value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()
            ?? value.ToString();
    }
}
