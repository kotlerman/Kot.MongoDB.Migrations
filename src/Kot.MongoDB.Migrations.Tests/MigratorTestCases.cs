using NUnit.Framework;

namespace Kot.MongoDB.Migrations.Tests
{
    internal static class MigratorTestCases
    {
        public static TestCaseData[] NoMigrations = new[]
        {
            new TestCaseData(false, null)
                .SetName("NoMigrations_WithoutLogger"),
            new TestCaseData(true, LogStrings.NoMigrations)
                .SetName("NoMigrations_WithLogger"),
        };

        public static TestCaseData[] ApplyUp = new[]
        {
            new TestCaseData(TransactionScope.None, false, null)
                .SetName("ApplyUp_TransactionScopeNone_WithoutLogger"),
            new TestCaseData(TransactionScope.SingleMigration, false, null)
                .SetName("ApplyUp_TransactionScopeSingleMigration_WithoutLogger"),
            new TestCaseData(TransactionScope.AllMigrations, false, null)
                .SetName("ApplyUp_TransactionScopeAllMigrations_WithoutLogger"),
            new TestCaseData(TransactionScope.None, true, LogStrings.ApplyUpNone)
                .SetName("ApplyUp_TransactionScopeNone_WithLogger"),
            new TestCaseData(TransactionScope.SingleMigration, true, LogStrings.ApplyUpSingle)
                .SetName("ApplyUp_TransactionScopeSingleMigration_WithLogger"),
            new TestCaseData(TransactionScope.AllMigrations, true, LogStrings.ApplyUpAll)
                .SetName("ApplyUp_TransactionScopeAllMigrations_WithLogger"),
        };

        public static TestCaseData[] ApplyDown = new[]
        {
            new TestCaseData(TransactionScope.None, false, null)
                .SetName("ApplyDown_TransactionScopeNone_WithoutLogger"),
            new TestCaseData(TransactionScope.SingleMigration, false, null)
                .SetName("ApplyDown_TransactionScopeSingleMigration_WithoutLogger"),
            new TestCaseData(TransactionScope.AllMigrations, false, null)
                .SetName("ApplyDown_TransactionScopeAllMigrations_WithoutLogger"),
            new TestCaseData(TransactionScope.None, true, LogStrings.ApplyDownNone)
                .SetName("ApplyDown_TransactionScopeNone_WithLogger"),
            new TestCaseData(TransactionScope.SingleMigration, true, LogStrings.ApplyDownSingle)
                .SetName("ApplyDown_TransactionScopeSingleMigration_WithLogger"),
            new TestCaseData(TransactionScope.AllMigrations, true, LogStrings.ApplyDownAll)
                .SetName("ApplyDown_TransactionScopeAllMigrations_WithLogger"),
        };

        public static TestCaseData[] TargetVersionEqualsCurrent = new[]
        {
            new TestCaseData(false, null)
                .SetName("TargetVersionEqualsCurrent_WithoutLogger"),
            new TestCaseData(true, LogStrings.TargetVersionEqualsCurrent)
                .SetName("TargetVersionEqualsCurrent_WithLogger"),
        };

        public static TestCaseData[] FirstMigrationAlreadyApplied = new[]
        {
            new TestCaseData(false, null)
                .SetName("FirstMigrationAlreadyApplied_WithoutLogger"),
            new TestCaseData(true, LogStrings.FirstMigrationAlreadyApplied)
                .SetName("FirstMigrationAlreadyApplied_WithLogger"),
        };

        public static TestCaseData[] RollbackLastMigration = new[]
        {
            new TestCaseData(false, null)
                .SetName("RollbackLastMigration_WithoutLogger"),
            new TestCaseData(true, LogStrings.RollbackLastMigration)
                .SetName("RollbackLastMigration_WithLogger"),
        };

        public static TestCaseData[] MigrationException_NoTransaction = new[]
        {
            new TestCaseData(false, null)
                .SetName("MigrationException_NoTransaction_WithoutLogger"),
            new TestCaseData(true, LogStrings.MigrationExceptionNone)
                .SetName("MigrationException_NoTransaction_WithLogger"),
        };

        public static TestCaseData[] MigrationException_SingleMigrationTransaction = new[]
        {
            new TestCaseData(false, null)
                .SetName("MigrationException_SingleMigrationTransaction_WithoutLogger"),
            new TestCaseData(true, LogStrings.MigrationExceptionSingle)
                .SetName("MigrationException_SingleMigrationTransaction_WithLogger"),
        };

        public static TestCaseData[] MigrationException_AllMigrationsTransaction_Up = new[]
        {
            new TestCaseData(false, null)
                .SetName("MigrationException_AllMigrationsTransaction_Up_WithoutLogger"),
            new TestCaseData(true, LogStrings.MigrationExceptionAllUp)
                .SetName("MigrationException_AllMigrationsTransaction_Up_WithLogger"),
        };

        public static TestCaseData[] MigrationException_AllMigrationsTransaction_Down = new[]
        {
            new TestCaseData(false, null)
                .SetName("MigrationException_AllMigrationsTransaction_Down_WithoutLogger"),
            new TestCaseData(true, LogStrings.MigrationExceptionAllDown)
                .SetName("MigrationException_AllMigrationsTransaction_Down_WithLogger"),
        };

        public static TestCaseData[] IndexExists = new[]
        {
            new TestCaseData(false, null)
                .SetName("IndexExists_WithoutLogger"),
            new TestCaseData(true, LogStrings.IndexExists)
                .SetName("IndexExists_WithLogger"),
        };

        public static TestCaseData[] OtherMigrationInProgress_Cancel = new[]
        {
            new TestCaseData(false, null)
                .SetName("OtherMigrationInProgress_Cancel_WithoutLogger"),
            new TestCaseData(true, LogStrings.OtherMigrationInProgressCancel)
                .SetName("OtherMigrationInProgress_Cancel_WithLogger"),
        };

        public static TestCaseData[] OtherMigrationInProgress_Throw = new[]
        {
            new TestCaseData(false, null)
                .SetName("OtherMigrationInProgress_Throw_WithoutLogger"),
            new TestCaseData(true, LogStrings.OtherMigrationInProgressThrow)
                .SetName("OtherMigrationInProgress_Throw_WithLogger"),
        };

        public static TestCaseData[] ParallelMigrations_FirstApplied_SecondCancelled = new[]
        {
            new TestCaseData(false, null, null)
                .SetName("ParallelMigrations_FirstApplied_SecondCancelled_WithoutLogger"),
            new TestCaseData(true, LogStrings.ParallelMigrationsA, LogStrings.ParallelMigrationsCancelB)
                .SetName("ParallelMigrations_FirstApplied_SecondCancelled_WithLogger"),
        };

        public static TestCaseData[] ParallelMigrations_FirstApplied_SecondThrows = new[]
        {
            new TestCaseData(false, null, null)
                .SetName("ParallelMigrations_FirstApplied_SecondThrows_WithoutLogger"),
            new TestCaseData(true, LogStrings.ParallelMigrationsA, LogStrings.ParallelMigrationsThrowB)
                .SetName("ParallelMigrations_FirstApplied_SecondThrows_WithLogger"),
        };

        static class LogStrings
        {
            public const string NoMigrations = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 0 migrations.\n" +
                "[Information] The DB is up-to-date.\n";

            public const string ApplyUpNone = "[Information] Starting migration. Target version is \"0.0.3\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 3 migrations.\n" +
                "[Information] Upgrading the DB with 3 applicable migrations.\n" +
                "[Debug] Applying all migrations without transactions.\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Information] Applying migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.3\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string ApplyUpSingle = "[Information] Starting migration. Target version is \"0.0.3\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 3 migrations.\n" +
                "[Information] Upgrading the DB with 3 applicable migrations.\n" +
                "[Debug] Applying migrations in separate transactions.\n" +
                "[Debug] Starting a transaction for migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Starting a transaction for migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Starting a transaction for migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Information] Applying migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.3\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string ApplyUpAll = "[Information] Starting migration. Target version is \"0.0.3\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 3 migrations.\n" +
                "[Information] Upgrading the DB with 3 applicable migrations.\n" +
                "[Debug] Applying all migrations in one transaction.\n" +
                "[Debug] Starting a transaction.\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Information] Applying migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.3\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string ApplyDownNone = "[Information] Starting migration. Target version is \"0.0.1\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.3\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 3 migrations.\n" +
                "[Information] Downgrading the DB with 2 applicable migrations.\n" +
                "[Debug] Applying all migrations without transactions.\n" +
                "[Information] Applying migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.1\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string ApplyDownSingle = "[Information] Starting migration. Target version is \"0.0.1\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.3\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 3 migrations.\n" +
                "[Information] Downgrading the DB with 2 applicable migrations.\n" +
                "[Debug] Applying migrations in separate transactions.\n" +
                "[Debug] Starting a transaction for migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Information] Applying migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Starting a transaction for migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.1\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string ApplyDownAll = "[Information] Starting migration. Target version is \"0.0.1\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.3\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 3 migrations.\n" +
                "[Information] Downgrading the DB with 2 applicable migrations.\n" +
                "[Debug] Applying all migrations in one transaction.\n" +
                "[Debug] Starting a transaction.\n" +
                "[Information] Applying migration \"0.0.3\" (\"0.0.3\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.1\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string TargetVersionEqualsCurrent = "[Information] Starting migration. Target version is \"0.0.1\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.1\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 1 migrations.\n" +
                "[Information] The DB is up-to-date.\n";

            public const string FirstMigrationAlreadyApplied = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.1\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 2 migrations.\n" +
                "[Information] Upgrading the DB with 1 applicable migrations.\n" +
                "[Debug] Applying all migrations without transactions.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.2\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string RollbackLastMigration = "[Information] Starting migration. Target version is \"0.0.1\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.2\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 2 migrations.\n" +
                "[Information] Downgrading the DB with 1 applicable migrations.\n" +
                "[Debug] Applying all migrations without transactions.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.1\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string MigrationExceptionNone = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 1 migrations.\n" +
                "[Information] Upgrading the DB with 1 applicable migrations.\n" +
                "[Debug] Applying all migrations without transactions.\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (UP).\n";

            public const string MigrationExceptionSingle = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 2 migrations.\n" +
                "[Information] Upgrading the DB with 2 applicable migrations.\n" +
                "[Debug] Applying migrations in separate transactions.\n" +
                "[Debug] Starting a transaction for migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Commiting the transaction.\n" +
                "[Debug] Starting a transaction for migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] There was an error while applying the migration. Aborting the transaction.\n" +
                "System.Exception: Exception of type 'System.Exception' was thrown.*";

            public const string MigrationExceptionAllUp = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 2 migrations.\n" +
                "[Information] Upgrading the DB with 2 applicable migrations.\n" +
                "[Debug] Applying all migrations in one transaction.\n" +
                "[Debug] Starting a transaction.\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] There was an error while applying the migrations. Aborting the transaction.\n" +
                "System.Exception: Exception of type 'System.Exception' was thrown.*";

            public const string MigrationExceptionAllDown = "[Information] Starting migration. Target version is \"0.0.0\".\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.2\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 2 migrations.\n" +
                "[Information] Downgrading the DB with 2 applicable migrations.\n" +
                "[Debug] Applying all migrations in one transaction.\n" +
                "[Debug] Starting a transaction.\n" +
                "[Information] Applying migration \"0.0.2\" (\"0.0.2\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Deleting history entry.\n" +
                "[Debug] History entry deleted.\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (DOWN).\n" +
                "[Debug] There was an error while applying the migrations. Aborting the transaction.\n" +
                "System.Exception: Exception of type 'System.Exception' was thrown.*";

            public const string IndexExists = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 0 migrations.\n" +
                "[Information] The DB is up-to-date.\n";

            public const string OtherMigrationInProgressCancel = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Failed to acquire DB lock.\n" +
                "[Information] Other migration in progress detected. Cancelling current run.\n";

            public const string OtherMigrationInProgressThrow = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Failed to acquire DB lock.\n" +
                "[Error] Other migration in progress detected.\n";

            public const string ParallelMigrationsA = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 1 migrations.\n" +
                "[Information] Upgrading the DB with 1 applicable migrations.\n" +
                "[Debug] Applying all migrations without transactions.\n" +
                "[Information] Applying migration \"0.0.1\" (\"0.0.1\").\n" +
                "[Debug] Applying the migration (UP).\n" +
                "[Debug] Migration applied.\n" +
                "[Debug] Writing history entry.\n" +
                "[Debug] History entry saved.\n" +
                "[Debug] Verifying DB version after migration.\n" +
                "[Information] The DB was migrated to version \"0.0.1\".\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";

            public const string ParallelMigrationsCancelB = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Failed to acquire DB lock.\n" +
                "[Information] Other migration in progress detected. Cancelling current run.\n";

            public const string ParallelMigrationsThrowB = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Failed to acquire DB lock.\n" +
                "[Error] Other migration in progress detected.\n";
        }
    }
}
