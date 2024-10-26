using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Extensions;
using System;

namespace Kot.MongoDB.Migrations.Tests.Extensions
{
    internal static class AssertionsExtensions
    {
        public static EquivalencyAssertionOptions<T> UsingNonStrictDateTimeComparison<T>(this EquivalencyAssertionOptions<T> options)
        {
            return options.Using<DateTime>(x => x.Subject.Should().BeCloseTo(DateTime.UtcNow, 1.Minutes()))
                .When(x => x.CompileTimeType == typeof(DateTime));
        }
    }
}
