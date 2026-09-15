using BudgetManager.Domain.Enums;

namespace BudgetManager.Domain.Extensions
{
    public static class PermissionExtensions
    {
        private static readonly Permission[] _individualPermissions = [.. Enum.GetValues<Permission>().Where(p => p.IsSinglePermission())];

        public static IReadOnlyList<Permission> GetPermissions(this Permission permissions)
            => [.. _individualPermissions.Where(p => (permissions & p) == p)];

        public static bool IsNone(this Permission permission)
            => permission == Permission.None;

        public static bool IsSinglePermission(this Permission permission)
        {
            var value = (uint)permission;
            return value != 0 && (value & (value - 1)) == 0;
        }

        public static bool IsValid(this Permission permission)
        {
            return (permission & ~Permission.All) == Permission.None;
        }

        public static bool Includes(this Permission permissions, Permission permission)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(permission.IsSinglePermission(), true, nameof(permission));
            return (permissions & permission) == permission;
        }

        public static bool IncludesAll(this Permission permissions, Permission other)
        {
            return (permissions & other) == other;
        }

        public static bool IncludesAny(this Permission permissions, Permission other)
        {
            return (permissions & other) != Permission.None;
        }

        public static void ThrowIfNone(this Permission permission, string? paramName = null)
        {
            if (permission.IsNone())
                throw new ArgumentException("Permission cannot be None.", paramName);
        }

        public static void ThrowIfNotSingle(this Permission permission, string? paramName = null)
        {
            if (!permission.IsSinglePermission())
                throw new ArgumentException("Permission must be a single permission.", paramName);
        }

        public static void ThrowIfNotValid(this Permission permission, string? paramName = null)
        {
            if (!permission.IsValid())
                throw new ArgumentException("Invalid permission.", paramName);
        }
    }
}
