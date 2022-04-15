using System;

namespace Kot.MongoDB.Migrations
{
    public struct DatabaseVersion : IEquatable<DatabaseVersion>, IComparable<DatabaseVersion>, IComparable
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public DatabaseVersion(int major, int minor, int patch)
        {
            EnsureVersionNonNegative(major, minor, patch);
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public DatabaseVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentNullException(nameof(version));
            }

            string[] parts = version.Split('.');

            if (parts.Length != 3)
            {
                throw new FormatException("Invalid version format. Expected format is \"[Major].[Minor].[Patch]\".");
            }

            Major = int.Parse(parts[0]);
            Minor = int.Parse(parts[1]);
            Patch = int.Parse(parts[2]);
            EnsureVersionNonNegative(Major, Minor, Patch);
        }

        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        public bool Equals(DatabaseVersion other) => (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch);

        public override bool Equals(object obj) => obj is DatabaseVersion version && Equals(version);

        public override int GetHashCode() => (Major, Minor, Patch).GetHashCode();

        public int CompareTo(DatabaseVersion other)
        {
            int majorDiff = Major - other.Major;

            if (majorDiff != 0)
            {
                return majorDiff;
            }

            int minorDiff = Minor - other.Minor;

            if (minorDiff != 0)
            {
                return minorDiff;
            }

            return Patch - other.Patch;
        }

        public int CompareTo(object obj) => CompareTo((DatabaseVersion)obj);

        public static bool operator ==(DatabaseVersion left, DatabaseVersion right) => left.Equals(right);

        public static bool operator !=(DatabaseVersion left, DatabaseVersion right) => !(left == right);

        public static bool operator >(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) > 0;

        public static bool operator <(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) < 0;

        public static bool operator >=(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) >= 0;

        public static bool operator <=(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) <= 0;

        public static implicit operator DatabaseVersion(string value) => new DatabaseVersion(value);

        private static void EnsureVersionNonNegative(int major, int minor, int patch)
        {
            const string errorMessage = "Value must be non-negative.";

            if (major < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major), major, errorMessage);
            }

            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minor), minor, errorMessage);
            }

            if (patch < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(patch), patch, errorMessage);
            }
        }
    }
}
