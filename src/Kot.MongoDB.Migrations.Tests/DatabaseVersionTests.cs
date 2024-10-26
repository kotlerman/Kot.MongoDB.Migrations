using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Kot.MongoDB.Migrations.Tests
{
    [TestFixture]
    public class DatabaseVersionTests
    {
        [Test]
        public void CreateVersion_Success()
        {
            // Act
            var version = new DatabaseVersion(1, 2, 3);

            // Assert
            version.Major.Should().Be(1);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
        }

        [TestCaseSource(nameof(ParseVersion_Success_TestCases))]
        public void ParseVersion_Success(string versionString, int expectedMajor, int expectedMinor, int expectedPatch)
        {
            // Act
            var version = new DatabaseVersion(versionString);

            // Assert
            version.Major.Should().Be(expectedMajor);
            version.Minor.Should().Be(expectedMinor);
            version.Patch.Should().Be(expectedPatch);
        }

        [TestCaseSource(nameof(ParseVersion_Failure_ArgumentNull_TestCases))]
        public void ParseVersion_Failure_ArgumentNull(string versionString)
        {
            Assert.Throws<ArgumentNullException>(() => new DatabaseVersion(versionString));
        }

        [TestCaseSource(nameof(ParseVersion_Failure_OutOfRange_TestCases))]
        public void ParseVersion_Failure_OutOfRange(string versionString)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DatabaseVersion(versionString));
        }

        [TestCaseSource(nameof(ParseVersion_Failure_Format_TestCases))]
        public void ParseVersion_Failure_Format(string versionString)
        {
            Assert.Throws<FormatException>(() => new DatabaseVersion(versionString));
        }

        [Test]
        public void ToString_CorrectString()
        {
            // Arrange
            var version = new DatabaseVersion(1, 2, 3);

            // Act
            var actualString = version.ToString();

            // Assert
            actualString.Should().Be("1.2.3");
        }

        [TestCaseSource(nameof(Equals_Generic_TestCases))]
        public void Equals_Generic(DatabaseVersion versionA, DatabaseVersion versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA.Equals(versionB);

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(Equals_Object_TestCases))]
        public void Equals_Object(DatabaseVersion versionA, DatabaseVersion? versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA.Equals(versionB);

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(CompareTo_Generic_TestCases))]
        public void CompareTo_Generic(DatabaseVersion versionA, DatabaseVersion versionB, int expectedResult)
        {
            // Act
            int actualResult = Math.Sign(versionA.CompareTo(versionB));

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(CompareTo_Object_TestCases))]
        public void CompareTo_Object(DatabaseVersion versionA, DatabaseVersion? versionB, int expectedResult)
        {
            // Act
            int actualResult = Math.Sign(versionA.CompareTo(versionB));

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(GetHashCode_SameOnEqual_TestCases))]
        public void GetHashCode_SameOnEqualVersions(DatabaseVersion versionA, DatabaseVersion versionB)
        {
            // Act
            int hashCodeA = versionA.GetHashCode();
            int hashCodeB = versionB.GetHashCode();

            // Assert
            hashCodeA.Should().Be(hashCodeB);
        }

        [Test]
        public void ImplicitFromString_Success()
        {
            // Act
            var version = (DatabaseVersion)"1.2.3";

            // Assert
            version.Major.Should().Be(1);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
        }

        [TestCaseSource(nameof(EqualOperator_TestCases))]
        public void EqualOperator(DatabaseVersion versionA, DatabaseVersion versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA == versionB;

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(NotEqualOperator_TestCases))]
        public void NotEqualOperator(DatabaseVersion versionA, DatabaseVersion versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA != versionB;

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(GreaterOperator_TestCases))]
        public void GreaterOperator(DatabaseVersion versionA, DatabaseVersion versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA > versionB;

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(GreaterOrEqualOperator_TestCases))]
        public void GreaterOrEqualOperator(DatabaseVersion versionA, DatabaseVersion versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA >= versionB;

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(LessOperator_TestCases))]
        public void LessOperator(DatabaseVersion versionA, DatabaseVersion versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA < versionB;

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(LessOrEqualOperator_TestCases))]
        public void LessOrEqualOperator(DatabaseVersion versionA, DatabaseVersion versionB, bool expectedResult)
        {
            // Act
            bool actualResult = versionA <= versionB;

            // Assert
            actualResult.Should().Be(expectedResult);
        }

        private static IEnumerable<TestCaseData> ParseVersion_Success_TestCases() => new[]
        {
            new TestCaseData("0.0.0", 0, 0, 0)
                .SetName("ParseVersion_Success_MinimumVersion"),
            new TestCaseData("1.2.3", 1, 2, 3)
                .SetName("ParseVersion_Success_RegularVersion"),
            new TestCaseData("2147483647.2147483647.2147483647", 2147483647, 2147483647, 2147483647)
                .SetName("ParseVersion_Success_MaximumVersion"),
        };

        private static IEnumerable<TestCaseData> ParseVersion_Failure_ArgumentNull_TestCases() => new[]
        {
            new TestCaseData(null).SetName("ParseVersion_Failure_ArgumentNull_Null"),
            new TestCaseData("").SetName("ParseVersion_Failure_ArgumentNull_Empty"),
        };

        private static IEnumerable<TestCaseData> ParseVersion_Failure_OutOfRange_TestCases() => new[]
        {
            new TestCaseData("-1.0.0").SetName("ParseVersion_Failure_OutOfRange_Major"),
            new TestCaseData("0.-1.0").SetName("ParseVersion_Failure_OutOfRange_Minor"),
            new TestCaseData("0.0.-1").SetName("ParseVersion_Failure_OutOfRange_Patch"),
        };

        private static IEnumerable<TestCaseData> ParseVersion_Failure_Format_TestCases() => new[]
        {
            new TestCaseData("0").SetName("ParseVersion_Failure_Format_OnePart"),
            new TestCaseData("0.0").SetName("ParseVersion_Failure_Format_TwoParts"),
            new TestCaseData("0.0.0.0").SetName("ParseVersion_Failure_Format_FourParts"),
        };

        private static IEnumerable<TestCaseData> Equals_Generic_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), true).SetName("Equals_Generic_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), false).SetName("Equals_Generic_DifferentMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), false).SetName("Equals_Generic_DifferentMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), false).SetName("Equals_Generic_DifferentPatch"),
        };

        private static IEnumerable<TestCaseData> Equals_Object_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), true).SetName("Equals_Object_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), false).SetName("Equals_Object_DifferentMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), false).SetName("Equals_Object_DifferentMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), false).SetName("Equals_Object_DifferentPatch"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), null, false).SetName("Equals_Object_Null"),
        };

        private static IEnumerable<TestCaseData> CompareTo_Generic_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), 0).SetName("CompareTo_Generic_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), -1).SetName("CompareTo_Generic_LessByMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), -1).SetName("CompareTo_Generic_LessByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), -1).SetName("CompareTo_Generic_LessByPatch"),
            new TestCaseData(new DatabaseVersion(9, 2, 3), new DatabaseVersion(1, 2, 3), 1).SetName("CompareTo_Generic_GreaterByMajor"),
            new TestCaseData(new DatabaseVersion(1, 9, 3), new DatabaseVersion(1, 2, 3), 1).SetName("CompareTo_Generic_GreaterByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 9), new DatabaseVersion(1, 2, 3), 1).SetName("CompareTo_Generic_GreaterByPatch"),
        };

        private static IEnumerable<TestCaseData> CompareTo_Object_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), 0).SetName("CompareTo_Object_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), -1).SetName("CompareTo_Object_LessByMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), -1).SetName("CompareTo_Object_LessByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), -1).SetName("CompareTo_Object_LessByPatch"),
            new TestCaseData(new DatabaseVersion(9, 2, 3), new DatabaseVersion(1, 2, 3), 1).SetName("CompareTo_Object_GreaterByMajor"),
            new TestCaseData(new DatabaseVersion(1, 9, 3), new DatabaseVersion(1, 2, 3), 1).SetName("CompareTo_Object_GreaterByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 9), new DatabaseVersion(1, 2, 3), 1).SetName("CompareTo_Object_GreaterByPatch"),
        };

        private static IEnumerable<TestCaseData> GetHashCode_SameOnEqual_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(0, 0, 0), new DatabaseVersion(0, 0, 0)).SetName("GetHashCode_SameOnEqual_0_0_0"),
            new TestCaseData(new DatabaseVersion(1, 0, 0), new DatabaseVersion(1, 0, 0)).SetName("GetHashCode_SameOnEqual_1_0_0"),
            new TestCaseData(new DatabaseVersion(0, 1, 0), new DatabaseVersion(0, 1, 0)).SetName("GetHashCode_SameOnEqual_0_1_0"),
            new TestCaseData(new DatabaseVersion(0, 0, 1), new DatabaseVersion(0, 0, 1)).SetName("GetHashCode_SameOnEqual_0_0_1"),
            new TestCaseData(new DatabaseVersion(1, 1, 1), new DatabaseVersion(1, 1, 1)).SetName("GetHashCode_SameOnEqual_1_1_1"),
            new TestCaseData(
                new DatabaseVersion(2147483647, 2147483647, 2147483647),
                new DatabaseVersion(2147483647, 2147483647, 2147483647))
                .SetName("GetHashCode_SameOnEqual_2147483647_2147483647_2147483647"),
        };

        private static IEnumerable<TestCaseData> EqualOperator_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), true).SetName("EqualOperator_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), false).SetName("EqualOperator_DifferentMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), false).SetName("EqualOperator_DifferentMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), false).SetName("EqualOperator_DifferentPatch"),
        };

        private static IEnumerable<TestCaseData> NotEqualOperator_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), false).SetName("NotEqualOperator_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), true).SetName("NotEqualOperator_DifferentMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), true).SetName("NotEqualOperator_DifferentMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), true).SetName("NotEqualOperator_DifferentPatch"),
        };

        private static IEnumerable<TestCaseData> GreaterOperator_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), false).SetName("GreaterOperator_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), false).SetName("GreaterOperator_LessByMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), false).SetName("GreaterOperator_LessByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), false).SetName("GreaterOperator_LessByPatch"),
            new TestCaseData(new DatabaseVersion(9, 2, 3), new DatabaseVersion(1, 2, 3), true).SetName("GreaterOperator_GreaterByMajor"),
            new TestCaseData(new DatabaseVersion(1, 9, 3), new DatabaseVersion(1, 2, 3), true).SetName("GreaterOperator_GreaterByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 9), new DatabaseVersion(1, 2, 3), true).SetName("GreaterOperator_GreaterByPatch"),
        };

        private static IEnumerable<TestCaseData> GreaterOrEqualOperator_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), true).SetName("GreaterOrEqual_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), false).SetName("GreaterOrEqual_LessByMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), false).SetName("GreaterOrEqual_LessByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), false).SetName("GreaterOrEqual_LessByPatch"),
            new TestCaseData(new DatabaseVersion(9, 2, 3), new DatabaseVersion(1, 2, 3), true).SetName("GreaterOrEqual_GreaterByMajor"),
            new TestCaseData(new DatabaseVersion(1, 9, 3), new DatabaseVersion(1, 2, 3), true).SetName("GreaterOrEqual_GreaterByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 9), new DatabaseVersion(1, 2, 3), true).SetName("GreaterOrEqual_GreaterByPatch"),
        };

        private static IEnumerable<TestCaseData> LessOperator_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), false).SetName("LessOperator_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), true).SetName("LessOperator_LessByMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), true).SetName("LessOperator_LessByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), true).SetName("LessOperator_LessByPatch"),
            new TestCaseData(new DatabaseVersion(9, 2, 3), new DatabaseVersion(1, 2, 3), false).SetName("LessOperator_GreaterByMajor"),
            new TestCaseData(new DatabaseVersion(1, 9, 3), new DatabaseVersion(1, 2, 3), false).SetName("LessOperator_GreaterByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 9), new DatabaseVersion(1, 2, 3), false).SetName("LessOperator_GreaterByPatch"),
        };

        private static IEnumerable<TestCaseData> LessOrEqualOperator_TestCases() => new[]
        {
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 3), true).SetName("LessOrEqual_Equal"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(9, 2, 3), true).SetName("LessOrEqual_LessByMajor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 9, 3), true).SetName("LessOrEqual_LessByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 3), new DatabaseVersion(1, 2, 9), true).SetName("LessOrEqual_LessByPatch"),
            new TestCaseData(new DatabaseVersion(9, 2, 3), new DatabaseVersion(1, 2, 3), false).SetName("LessOrEqual_GreaterByMajor"),
            new TestCaseData(new DatabaseVersion(1, 9, 3), new DatabaseVersion(1, 2, 3), false).SetName("LessOrEqual_GreaterByMinor"),
            new TestCaseData(new DatabaseVersion(1, 2, 9), new DatabaseVersion(1, 2, 3), false).SetName("LessOrEqual_GreaterByPatch"),
        };
    }
}
