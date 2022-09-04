using System;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Represents a database version.
    /// </summary>
    public struct DatabaseVersion : IEquatable<DatabaseVersion>, IComparable<DatabaseVersion>, IComparable
    {
        /// <summary>
        /// Major version.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Minor version.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Patch version.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Instantiates a new instance of <see cref="DatabaseVersion"/>.
        /// </summary>
        /// <param name="major">Major version.</param>
        /// <param name="minor">Minor version.</param>
        /// <param name="patch">Patch version.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="major"/>, <paramref name="minor"/> or
        /// <paramref name="patch"/> is less than zero.</exception>
        public DatabaseVersion(int major, int minor, int patch)
        {
            EnsureVersionNonNegative(major, minor, patch);
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="DatabaseVersion"/>.
        /// </summary>
        /// <param name="version">String representation of a version in the following format: "[Major].[Minor].[Patch]".</param>
        /// <exception cref="ArgumentNullException"><paramref name="version"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="FormatException"><paramref name="version"/> is in invalid format.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Major, minor or patch is less than zero.</exception>
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

        /// <summary>
        /// Returns the string representation of the database version.
        /// </summary>
        /// <returns>String representation of the database version.</returns>
        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Indicates whether the current version equals to another version.
        /// </summary>
        /// <param name="other"></param>
        /// <returns><see langword="true"/> if the current version is equal to the <paramref name="other"/> parameter;
        /// otherwise, <see langword="false"/>.</returns>
        public bool Equals(DatabaseVersion other) => (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DatabaseVersion version && Equals(version);

        /// <inheritdoc/>
        public override int GetHashCode() => (Major, Minor, Patch).GetHashCode();

        /// <summary>
        /// Compares the current version with another version and returns an integer that indicates whether the current version precedes,
        /// follows, or occurs in the same position in the sort order as the other version.
        /// </summary>
        /// <param name="other">A version to compare with this version.</param>
        /// <returns>A value that indicates the relative order of the versions being compared.</returns>
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

        /// <summary>
        /// Compares the current version with another version and returns an integer that indicates whether the current version precedes,
        /// follows, or occurs in the same position in the sort order as the other version.
        /// </summary>
        /// <param name="obj">A version to compare with this version.</param>
        /// <returns>A value that indicates the relative order of the versions being compared.</returns>
        public int CompareTo(object obj) => CompareTo((DatabaseVersion)obj);

        /// <summary>
        /// Determines whether two specified versions have the same value.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><see langword="true"/> if the value of <paramref name="left"/> is the same
        /// as the value of <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(DatabaseVersion left, DatabaseVersion right) => left.Equals(right);

        /// <summary>
        /// Determines whether two specified versions have different values.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><see langword="true"/> if the value of <paramref name="left"/> is different
        /// from the value of <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(DatabaseVersion left, DatabaseVersion right) => !(left == right);

        /// <summary>
        /// Determines whether one version is greater than the other.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><see langword="true"/> if the value of <paramref name="left"/> is greater
        /// than the value of <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) > 0;

        /// <summary>
        /// Determines whether one version is less than the other.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><see langword="true"/> if the value of <paramref name="left"/> is less
        /// than the value of <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) < 0;

        /// <summary>
        /// Determines whether one version is greater or equal to the other.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><see langword="true"/> if the value of <paramref name="left"/> is greater or equal
        /// to the value of <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) >= 0;

        /// <summary>
        /// Determines whether one version is less or equal to the other.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><see langword="true"/> if the value of <paramref name="left"/> is less or equal
        /// to the value of <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(DatabaseVersion left, DatabaseVersion right) => left.CompareTo(right) <= 0;

        /// <summary>
        /// Defines an implicit conversion of a given string to a database version.
        /// </summary>
        /// <param name="value">A string to implicitly convert.</param>
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
