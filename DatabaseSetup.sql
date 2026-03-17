-- =============================================
-- WASFATY TRACKER - LOCAL DATABASE SETUP
-- Run this script on your LOCAL SQL Server (localhost)
-- Database: WasfatyTracker
-- =============================================

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'WasfatyTracker')
BEGIN
    CREATE DATABASE WasfatyTracker;
END
GO

USE WasfatyTracker;
GO

-- =============================================
-- Table: ProcessingHistory
-- Tracks which dates have been processed
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessingHistory')
BEGIN
    CREATE TABLE ProcessingHistory (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProcessingDate DATETIME NOT NULL,
        Status NVARCHAR(50) NOT NULL, -- 'Success', 'Failed', 'Partial', 'Processing'
        TotalInvoices INT NOT NULL DEFAULT 0,
        SuccessCount INT NOT NULL DEFAULT 0,
        FailedCount INT NOT NULL DEFAULT 0,
        SkippedCount INT NOT NULL DEFAULT 0,
        StartedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CompletedAt DATETIME NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ResponseMessage NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_ProcessingHistory_ProcessingDate ON ProcessingHistory(ProcessingDate DESC);
    CREATE INDEX IX_ProcessingHistory_Status ON ProcessingHistory(Status);
END
GO

-- =============================================
-- Table: APIRequest
-- Stores all API requests made
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'APIRequest')
BEGIN
    CREATE TABLE APIRequest (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProcessingHistoryId INT NULL,
        RequestType NVARCHAR(50) NOT NULL, -- 'Authentication', 'Invoice'
        RequestUrl NVARCHAR(500) NOT NULL,
        RequestMethod NVARCHAR(10) NOT NULL, -- 'POST', 'GET', etc.
        RequestHeaders NVARCHAR(MAX) NULL,
        RequestBody NVARCHAR(MAX) NULL,
        RequestedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_APIRequest_ProcessingHistory FOREIGN KEY (ProcessingHistoryId) 
            REFERENCES ProcessingHistory(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_APIRequest_ProcessingHistoryId ON APIRequest(ProcessingHistoryId);
    CREATE INDEX IX_APIRequest_RequestType ON APIRequest(RequestType);
    CREATE INDEX IX_APIRequest_RequestedAt ON APIRequest(RequestedAt DESC);
END
GO

-- =============================================
-- Table: APIResponse
-- Stores all API responses received
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'APIResponse')
BEGIN
    CREATE TABLE APIResponse (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        APIRequestId INT NOT NULL,
        StatusCode INT NOT NULL,
        IsSuccess BIT NOT NULL DEFAULT 0,
        ResponseHeaders NVARCHAR(MAX) NULL,
        ResponseBody NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ResponseReceivedAt DATETIME NOT NULL DEFAULT GETDATE(),
        DurationMs INT NULL, -- Response time in milliseconds
        CONSTRAINT FK_APIResponse_APIRequest FOREIGN KEY (APIRequestId) 
            REFERENCES APIRequest(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_APIResponse_APIRequestId ON APIResponse(APIRequestId);
    CREATE INDEX IX_APIResponse_StatusCode ON APIResponse(StatusCode);
    CREATE INDEX IX_APIResponse_IsSuccess ON APIResponse(IsSuccess);
END
GO

-- =============================================
-- Table: InvoiceDetails
-- Stores processed invoice details for reference
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceDetails')
BEGIN
    CREATE TABLE InvoiceDetails (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProcessingHistoryId INT NOT NULL,
        WasfatyInvoiceReference NVARCHAR(50) NOT NULL,
        Alias NVARCHAR(20) NULL,
        InvoiceDateTime DATETIME NOT NULL,
        CustomerName NVARCHAR(200) NULL,
        CustomerPhone NVARCHAR(20) NULL,
        CustomerId NVARCHAR(50) NULL,
        ProcessingStatus NVARCHAR(50) NOT NULL, -- 'Created', 'Skipped', 'Failed'
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_InvoiceDetails_ProcessingHistory FOREIGN KEY (ProcessingHistoryId) 
            REFERENCES ProcessingHistory(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_InvoiceDetails_Reference ON InvoiceDetails(WasfatyInvoiceReference);
    CREATE INDEX IX_InvoiceDetails_ProcessingHistoryId ON InvoiceDetails(ProcessingHistoryId);
    CREATE INDEX IX_InvoiceDetails_Status ON InvoiceDetails(ProcessingStatus);
END
GO

-- =============================================
-- View: ProcessingSummary
-- Quick overview of processing history
-- =============================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'ProcessingSummary')
    DROP VIEW ProcessingSummary;
GO

CREATE VIEW ProcessingSummary AS
SELECT 
    ph.ProcessingDate,
    ph.Status,
    ph.TotalInvoices,
    ph.SuccessCount,
    ph.FailedCount,
    ph.SkippedCount,
    ph.StartedAt,
    ph.CompletedAt,
    DATEDIFF(SECOND, ph.StartedAt, ISNULL(ph.CompletedAt, GETDATE())) AS DurationSeconds,
    COUNT(ar.Id) AS TotalAPIRequests,
    ph.ResponseMessage
FROM ProcessingHistory ph
LEFT JOIN APIRequest ar ON ph.Id = ar.ProcessingHistoryId
GROUP BY 
    ph.ProcessingDate, ph.Status, ph.TotalInvoices, ph.SuccessCount, 
    ph.FailedCount, ph.SkippedCount, ph.StartedAt, ph.CompletedAt, ph.ResponseMessage;
GO

PRINT 'Database setup completed successfully!';
PRINT 'Tables created: ProcessingHistory, APIRequest, APIResponse, InvoiceDetails';
PRINT 'View created: ProcessingSummary';
GO
