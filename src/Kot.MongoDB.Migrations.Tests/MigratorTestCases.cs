using NUnit.Framework;

namespace Kot.MongoDB.Migrations.Tests
{
    internal static class MigratorTestCases
    {
        public static TestCaseData[] NoMigrations = new[]
        {
            new TestCaseData(false).SetName("NoMigrations_WithoutLogger"),
            new TestCaseData(true).SetName("NoMigrations_WithLogger"),
        };

        public static TestCaseData[] ApplyUp = new[]
        {
            new TestCaseData(TransactionScope.None, false).SetName("ApplyUp_TransactionScopeNone_WithoutLogger"),
            new TestCaseData(TransactionScope.SingleMigration, false).SetName("ApplyUp_TransactionScopeSingleMigration_WithoutLogger"),
            new TestCaseData(TransactionScope.AllMigrations, false).SetName("ApplyUp_TransactionScopeAllMigrations_WithoutLogger"),
            new TestCaseData(TransactionScope.None, true).SetName("ApplyUp_TransactionScopeNone_WithLogger"),
            new TestCaseData(TransactionScope.SingleMigration, true).SetName("ApplyUp_TransactionScopeSingleMigration_WithLogger"),
            new TestCaseData(TransactionScope.AllMigrations, true).SetName("ApplyUp_TransactionScopeAllMigrations_WithLogger"),
        };

        public static TestCaseData[] ApplyDown = new[]
        {
            new TestCaseData(TransactionScope.None, false).SetName("ApplyDown_TransactionScopeNone_WithoutLogger"),
            new TestCaseData(TransactionScope.SingleMigration, false).SetName("ApplyDown_TransactionScopeSingleMigration_WithoutLogger"),
            new TestCaseData(TransactionScope.AllMigrations, false).SetName("ApplyDown_TransactionScopeAllMigrations_WithoutLogger"),
            new TestCaseData(TransactionScope.None, true).SetName("ApplyDown_TransactionScopeNone_WithLogger"),
            new TestCaseData(TransactionScope.SingleMigration, true).SetName("ApplyDown_TransactionScopeSingleMigration_WithLogger"),
            new TestCaseData(TransactionScope.AllMigrations, true).SetName("ApplyDown_TransactionScopeAllMigrations_WithLogger"),
        };

        public static TestCaseData[] TargetVersionEqualsCurrent = new[]
        {
            new TestCaseData(false).SetName("TargetVersionEqualsCurrent_WithoutLogger"),
            new TestCaseData(true).SetName("TargetVersionEqualsCurrent_WithLogger"),
        };

        public static TestCaseData[] FirstMigrationAlreadyApplied = new[]
        {
            new TestCaseData(false).SetName("FirstMigrationAlreadyApplied_WithoutLogger"),
            new TestCaseData(true).SetName("FirstMigrationAlreadyApplied_WithLogger"),
        };

        public static TestCaseData[] RollbackLastMigration = new[]
        {
            new TestCaseData(false).SetName("RollbackLastMigration_WithoutLogger"),
            new TestCaseData(true).SetName("RollbackLastMigration_WithLogger"),
        };

        public static TestCaseData[] MigrationException_NoTransaction = new[]
        {
            new TestCaseData(false).SetName("MigrationException_NoTransaction_WithoutLogger"),
            new TestCaseData(true).SetName("MigrationException_NoTransaction_WithLogger"),
        };

        public static TestCaseData[] MigrationException_SingleMigrationTransaction = new[]
        {
            new TestCaseData(false).SetName("MigrationException_SingleMigrationTransaction_WithoutLogger"),
            new TestCaseData(true).SetName("MigrationException_SingleMigrationTransaction_WithLogger"),
        };

        public static TestCaseData[] MigrationException_AllMigrationsTransaction_Up = new[]
        {
            new TestCaseData(false).SetName("MigrationException_AllMigrationsTransaction_Up_WithoutLogger"),
            new TestCaseData(true).SetName("MigrationException_AllMigrationsTransaction_Up_WithLogger"),
        };

        public static TestCaseData[] MigrationException_AllMigrationsTransaction_Down = new[]
        {
            new TestCaseData(false).SetName("MigrationException_AllMigrationsTransaction_Down_WithoutLogger"),
            new TestCaseData(true).SetName("MigrationException_AllMigrationsTransaction_Down_WithLogger"),
        };

        public static TestCaseData[] IndexExists = new[]
        {
            new TestCaseData(false).SetName("IndexExists_WithoutLogger"),
            new TestCaseData(true).SetName("IndexExists_WithLogger"),
        };

        public static TestCaseData[] OtherMigrationInProgress_Cancel = new[]
        {
            new TestCaseData(false).SetName("OtherMigrationInProgress_Cancel_WithoutLogger"),
            new TestCaseData(true).SetName("OtherMigrationInProgress_Cancel_WithLogger"),
        };

        public static TestCaseData[] OtherMigrationInProgress_Throw = new[]
        {
            new TestCaseData(false).SetName("OtherMigrationInProgress_Throw_WithoutLogger"),
            new TestCaseData(true).SetName("OtherMigrationInProgress_Throw_WithLogger"),
        };

        public static TestCaseData[] ParallelMigrations_FirstApplied_SecondCancelled = new[]
        {
            new TestCaseData(false).SetName("ParallelMigrations_FirstApplied_SecondCancelled_WithoutLogger"),
            new TestCaseData(true).SetName("ParallelMigrations_FirstApplied_SecondCancelled_WithLogger"),
        };

        public static TestCaseData[] ParallelMigrations_FirstApplied_SecondThrows = new[]
        {
            new TestCaseData(false).SetName("ParallelMigrations_FirstApplied_SecondThrows_WithoutLogger"),
            new TestCaseData(true).SetName("ParallelMigrations_FirstApplied_SecondThrows_WithLogger"),
        };
    }
}
