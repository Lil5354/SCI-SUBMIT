-- Script để rollback database về trạng thái ban đầu
-- Xóa các cột và bảng đã thêm từ các thay đổi trước đó

USE [SciSubmit];
GO

-- BƯỚC 1: Xóa index trước (nếu có)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Conferences_CreatedByAdminId' AND object_id = OBJECT_ID(N'[dbo].[Conferences]'))
BEGIN
    DROP INDEX [IX_Conferences_CreatedByAdminId] ON [dbo].[Conferences];
    PRINT 'Đã xóa index IX_Conferences_CreatedByAdminId';
END
GO

-- BƯỚC 2: Xóa foreign key constraint nếu có
IF EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[Conferences]')
    AND name LIKE '%CreatedByAdminId%'
)
BEGIN
    DECLARE @FKName NVARCHAR(128);
    SELECT @FKName = name 
    FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[Conferences]')
    AND name LIKE '%CreatedByAdminId%';
    
    EXEC('ALTER TABLE [dbo].[Conferences] DROP CONSTRAINT [' + @FKName + ']');
    PRINT 'Đã xóa foreign key constraint: ' + @FKName;
END
GO

-- BƯỚC 3: Xóa cột CreatedByAdminId từ bảng Conferences
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Conferences]') 
    AND name = 'CreatedByAdminId'
)
BEGIN
    ALTER TABLE [dbo].[Conferences] DROP COLUMN [CreatedByAdminId];
    PRINT 'Đã xóa cột CreatedByAdminId từ bảng Conferences';
END
ELSE
BEGIN
    PRINT 'Cột CreatedByAdminId không tồn tại trong bảng Conferences';
END
GO

-- BƯỚC 4: Xóa bảng AdminConferenceQuota nếu tồn tại
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminConferenceQuotas')
BEGIN
    -- Xóa foreign key constraints trước
    DECLARE @sql NVARCHAR(MAX) = '';
    SELECT @sql = @sql + 'ALTER TABLE [dbo].[AdminConferenceQuotas] DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('dbo.AdminConferenceQuotas');
    
    IF @sql <> ''
    BEGIN
        EXEC sp_executesql @sql;
        PRINT 'Đã xóa foreign key constraints từ AdminConferenceQuotas';
    END
    
    -- Xóa bảng
    DROP TABLE [dbo].[AdminConferenceQuotas];
    PRINT 'Đã xóa bảng AdminConferenceQuotas';
END
ELSE
BEGIN
    PRINT 'Bảng AdminConferenceQuotas không tồn tại';
END
GO

PRINT '========================================';
PRINT 'Rollback database hoàn tất!';
PRINT '========================================';
GO
