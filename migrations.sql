IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918015209_InitialMigrate'
)
BEGIN
    CREATE TABLE [Plans] (
        [Id] int NOT NULL IDENTITY,
        [PlanName] nvarchar(max) NOT NULL,
        [PlanDetails] nvarchar(max) NOT NULL,
        [Price] float NULL,
        [Discount] int NULL,
        [DateArchive] int NULL,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918015209_InitialMigrate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [ContactNumber] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [Password] nvarchar(max) NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        [DateArchived] datetime2 NULL,
        [DateModified] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918015209_InitialMigrate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918015209_InitialMigrate', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918022654_FixUser'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'ContactNumber');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [Users] DROP COLUMN [ContactNumber];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918022654_FixUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918022654_FixUser', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918025848_PassConfirm'
)
BEGIN
    ALTER TABLE [Users] ADD [ConfirmPassword] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918025848_PassConfirm'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918025848_PassConfirm', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918030903_ConfirmPassword'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'ConfirmPassword');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Users] DROP COLUMN [ConfirmPassword];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918030903_ConfirmPassword'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918030903_ConfirmPassword', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918061741_SignUpEmployer'
)
BEGIN
    CREATE TABLE [EmployerDetails] (
        [Id] int NOT NULL IDENTITY,
        [UsersId] int NOT NULL,
        [isTrainingCenter] int NOT NULL,
        [isEmployer] int NOT NULL,
        [EmployerName] nvarchar(max) NOT NULL,
        [pPlansId] int NOT NULL,
        [BusinessName] nvarchar(max) NOT NULL,
        [BusinessDescription] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_EmployerDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployerDetails_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918061741_SignUpEmployer'
)
BEGIN
    CREATE TABLE [Images] (
        [Id] int NOT NULL IDENTITY,
        [UsersId] int NOT NULL,
        [Image] varbinary(max) NOT NULL,
        [ImageDetails] nvarchar(max) NOT NULL,
        [isArchive] datetime2 NULL,
        CONSTRAINT [PK_Images] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Images_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918061741_SignUpEmployer'
)
BEGIN
    CREATE INDEX [IX_EmployerDetails_UsersId] ON [EmployerDetails] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918061741_SignUpEmployer'
)
BEGIN
    CREATE INDEX [IX_Images_UsersId] ON [Images] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918061741_SignUpEmployer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918061741_SignUpEmployer', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918065337_EmployerFix'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployerDetails]') AND [c].[name] = N'Email');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [EmployerDetails] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [EmployerDetails] DROP COLUMN [Email];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918065337_EmployerFix'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployerDetails]') AND [c].[name] = N'EmployerName');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [EmployerDetails] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [EmployerDetails] DROP COLUMN [EmployerName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918065337_EmployerFix'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployerDetails]') AND [c].[name] = N'isTrainingCenter');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [EmployerDetails] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [EmployerDetails] ALTER COLUMN [isTrainingCenter] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918065337_EmployerFix'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployerDetails]') AND [c].[name] = N'isEmployer');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [EmployerDetails] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [EmployerDetails] ALTER COLUMN [isEmployer] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918065337_EmployerFix'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918065337_EmployerFix', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918080313_Payment'
)
BEGIN
    ALTER TABLE [EmployerDetails] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918080313_Payment'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [Amount] float NULL,
        [PaymentDetails] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [UsersId] int NOT NULL,
        [DueDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [EmployersId] int NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918080313_Payment'
)
BEGIN
    CREATE INDEX [IX_Payments_UsersId] ON [Payments] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918080313_Payment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918080313_Payment', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918081358_FixUserDetails'
)
BEGIN
    DROP INDEX [IX_EmployerDetails_UsersId] ON [EmployerDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918081358_FixUserDetails'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployerDetails_UsersId] ON [EmployerDetails] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250918081358_FixUserDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250918081358_FixUserDetails', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922065314_Address'
)
BEGIN
    ALTER TABLE [EmployerDetails] ADD [Address] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922065314_Address'
)
BEGIN
    ALTER TABLE [EmployerDetails] ADD [Latitude] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922065314_Address'
)
BEGIN
    ALTER TABLE [EmployerDetails] ADD [Longitude] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922065314_Address'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922065314_Address', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922072513_Subscription'
)
BEGIN
    CREATE TABLE [Subscription] (
        [Id] int NOT NULL IDENTITY,
        [UsersId] int NOT NULL,
        [PlansId] int NOT NULL,
        [Expiration] datetime2 NOT NULL,
        CONSTRAINT [PK_Subscription] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Subscription_Plans_PlansId] FOREIGN KEY ([PlansId]) REFERENCES [Plans] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Subscription_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922072513_Subscription'
)
BEGIN
    CREATE INDEX [IX_Subscription_PlansId] ON [Subscription] ([PlansId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922072513_Subscription'
)
BEGIN
    CREATE INDEX [IX_Subscription_UsersId] ON [Subscription] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922072513_Subscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922072513_Subscription', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922073747_pPlansRemove'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployerDetails]') AND [c].[name] = N'pPlansId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [EmployerDetails] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [EmployerDetails] DROP COLUMN [pPlansId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922073747_pPlansRemove'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922073747_pPlansRemove', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922074015_fixSubscription'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Subscription]') AND [c].[name] = N'Expiration');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Subscription] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Subscription] ALTER COLUMN [Expiration] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922074015_fixSubscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922074015_fixSubscription', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922081332_FixSomething'
)
BEGIN
    ALTER TABLE [Subscription] ADD [EmployerDetailsId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922081332_FixSomething'
)
BEGIN
    CREATE INDEX [IX_Subscription_EmployerDetailsId] ON [Subscription] ([EmployerDetailsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922081332_FixSomething'
)
BEGIN
    ALTER TABLE [Subscription] ADD CONSTRAINT [FK_Subscription_EmployerDetails_EmployerDetailsId] FOREIGN KEY ([EmployerDetailsId]) REFERENCES [EmployerDetails] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922081332_FixSomething'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922081332_FixSomething', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922104250_ChangeImagesToFile'
)
BEGIN
    DROP TABLE [Images];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922104250_ChangeImagesToFile'
)
BEGIN
    CREATE TABLE [Files] (
        [Id] int NOT NULL IDENTITY,
        [UsersId] int NOT NULL,
        [File] varbinary(max) NOT NULL,
        [ImageDetails] nvarchar(max) NOT NULL,
        [ContentType] nvarchar(max) NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [isArchive] datetime2 NULL,
        CONSTRAINT [PK_Files] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Files_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922104250_ChangeImagesToFile'
)
BEGIN
    CREATE INDEX [IX_Files_UsersId] ON [Files] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922104250_ChangeImagesToFile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922104250_ChangeImagesToFile', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922105655_AddToManyInEmployerDetails'
)
BEGIN
    ALTER TABLE [Files] ADD [EmployerDetailsId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922105655_AddToManyInEmployerDetails'
)
BEGIN
    CREATE INDEX [IX_Files_EmployerDetailsId] ON [Files] ([EmployerDetailsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922105655_AddToManyInEmployerDetails'
)
BEGIN
    ALTER TABLE [Files] ADD CONSTRAINT [FK_Files_EmployerDetails_EmployerDetailsId] FOREIGN KEY ([EmployerDetailsId]) REFERENCES [EmployerDetails] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922105655_AddToManyInEmployerDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922105655_AddToManyInEmployerDetails', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923100734_StatusInSubscription'
)
BEGIN
    ALTER TABLE [Subscription] ADD [Status] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923100734_StatusInSubscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250923100734_StatusInSubscription', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923111351_PaymentDetails'
)
BEGIN
    CREATE TABLE [PaymentDetails] (
        [Id] int NOT NULL IDENTITY,
        [UsersId] int NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [EmployerDetailsId] int NULL,
        CONSTRAINT [PK_PaymentDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentDetails_EmployerDetails_EmployerDetailsId] FOREIGN KEY ([EmployerDetailsId]) REFERENCES [EmployerDetails] ([Id]),
        CONSTRAINT [FK_PaymentDetails_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923111351_PaymentDetails'
)
BEGIN
    CREATE INDEX [IX_PaymentDetails_EmployerDetailsId] ON [PaymentDetails] ([EmployerDetailsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923111351_PaymentDetails'
)
BEGIN
    CREATE INDEX [IX_PaymentDetails_UsersId] ON [PaymentDetails] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923111351_PaymentDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250923111351_PaymentDetails', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923124936_PaymentDetailsName'
)
BEGIN
    ALTER TABLE [PaymentDetails] ADD [Name] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923124936_PaymentDetailsName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250923124936_PaymentDetailsName', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    ALTER TABLE [Files] DROP CONSTRAINT [FK_Files_EmployerDetails_EmployerDetailsId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    ALTER TABLE [PaymentDetails] DROP CONSTRAINT [FK_PaymentDetails_EmployerDetails_EmployerDetailsId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    ALTER TABLE [Subscription] DROP CONSTRAINT [FK_Subscription_EmployerDetails_EmployerDetailsId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    DROP INDEX [IX_Subscription_EmployerDetailsId] ON [Subscription];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    DROP INDEX [IX_PaymentDetails_EmployerDetailsId] ON [PaymentDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    DROP INDEX [IX_Files_EmployerDetailsId] ON [Files];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Subscription]') AND [c].[name] = N'EmployerDetailsId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Subscription] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Subscription] DROP COLUMN [EmployerDetailsId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaymentDetails]') AND [c].[name] = N'EmployerDetailsId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [PaymentDetails] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [PaymentDetails] DROP COLUMN [EmployerDetailsId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Files]') AND [c].[name] = N'EmployerDetailsId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Files] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Files] DROP COLUMN [EmployerDetailsId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250923133220_FixUsersToEmployerDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250923133220_FixUsersToEmployerDetails', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250924123934_FixPaymentGateway'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'DueDate');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [Payments] DROP COLUMN [DueDate];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250924123934_FixPaymentGateway'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'EmployersId');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Payments] DROP COLUMN [EmployersId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250924123934_FixPaymentGateway'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250924123934_FixPaymentGateway', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250924140605_AddCreatedAtInSubscription'
)
BEGIN
    ALTER TABLE [Subscription] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250924140605_AddCreatedAtInSubscription'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250924140605_AddCreatedAtInSubscription', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929075050_ChangeBytetoStringFiles'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Files]') AND [c].[name] = N'File');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Files] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [Files] ALTER COLUMN [File] nvarchar(max) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929075050_ChangeBytetoStringFiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250929075050_ChangeBytetoStringFiles', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094204_JobRequirementsTbl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250929094204_JobRequirementsTbl', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE TABLE [Jobs] (
        [Id] int NOT NULL IDENTITY,
        [UsersId] int NOT NULL,
        [JobTitle] nvarchar(200) NOT NULL,
        [SalaryMin] int NOT NULL,
        [SalaryMax] int NOT NULL,
        [ExpirationDate] datetime2 NOT NULL,
        [JobDescription] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Jobs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Jobs_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE TABLE [JobBenefits] (
        [Id] int NOT NULL IDENTITY,
        [Benefit] nvarchar(max) NOT NULL,
        [JobsId] int NOT NULL,
        CONSTRAINT [PK_JobBenefits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobBenefits_Jobs_JobsId] FOREIGN KEY ([JobsId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE TABLE [JobMedia] (
        [Id] int NOT NULL IDENTITY,
        [FilePath] nvarchar(max) NOT NULL,
        [FileType] nvarchar(max) NOT NULL,
        [JobsId] int NOT NULL,
        CONSTRAINT [PK_JobMedia] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobMedia_Jobs_JobsId] FOREIGN KEY ([JobsId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE TABLE [JobRequirements] (
        [Id] int NOT NULL IDENTITY,
        [Requirement] nvarchar(max) NOT NULL,
        [JobsId] int NOT NULL,
        CONSTRAINT [PK_JobRequirements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobRequirements_Jobs_JobsId] FOREIGN KEY ([JobsId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE INDEX [IX_JobBenefits_JobsId] ON [JobBenefits] ([JobsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE INDEX [IX_JobMedia_JobsId] ON [JobMedia] ([JobsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE INDEX [IX_JobRequirements_JobsId] ON [JobRequirements] ([JobsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    CREATE INDEX [IX_Jobs_UsersId] ON [Jobs] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929094920_JobRequirementsTblUpdate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250929094920_JobRequirementsTblUpdate', N'9.0.9');
END;

COMMIT;
GO

