-- Check the actual length of RequestBody and ResponseBody in the database
-- Run this to verify data is not truncated in storage

USE WasfatyTracker;
GO

-- Check request body lengths
SELECT TOP 10
    Id,
    ProcessingHistoryId,
    RequestType,
    RequestedAt,
    LEN(RequestBody) AS RequestBodyLength,
    LEN(RequestHeaders) AS RequestHeadersLength,
    LEFT(RequestBody, 100) AS RequestBodyPreview
FROM APIRequest
ORDER BY RequestedAt DESC;

-- Check response body lengths
SELECT TOP 10
    ar.Id AS RequestId,
    ar.RequestType,
    ar.RequestedAt,
    resp.StatusCode,
    resp.IsSuccess,
    LEN(resp.ResponseBody) AS ResponseBodyLength,
    LEN(resp.ErrorMessage) AS ErrorMessageLength,
    LEFT(resp.ResponseBody, 100) AS ResponseBodyPreview,
    resp.ErrorMessage
FROM APIRequest ar
LEFT JOIN APIResponse resp ON ar.Id = resp.APIRequestId
ORDER BY ar.RequestedAt DESC;

-- Get full error message for latest failed request
SELECT TOP 1
    ar.RequestType,
    ar.RequestedAt,
    resp.StatusCode,
    resp.ErrorMessage,
    resp.DurationMs
FROM APIRequest ar
INNER JOIN APIResponse resp ON ar.Id = resp.APIRequestId
WHERE resp.IsSuccess = 0
ORDER BY ar.RequestedAt DESC;

-- To export large RequestBody to file, use this:
-- SELECT RequestBody FROM APIRequest WHERE Id = <your_id>
-- Then click on the cell and choose "Save Results As..."

-- Validate JSON format and structure for latest Invoice request (SQL Server 2012+ compatible)
SELECT TOP 1
    ar.Id,
    ar.RequestType,
    ar.RequestedAt,
    LEN(ar.RequestBody) AS BodyLength,
    -- Check if starts with array bracket
    CASE 
        WHEN LEFT(ar.RequestBody, 1) = '[' THEN 'Starts with ['
        ELSE 'Does NOT start with ['
    END AS ArrayCheck,
    -- Check for camelCase properties
    CASE 
        WHEN ar.RequestBody LIKE '%"wasfatyInvoiceReference"%' THEN 'Has camelCase'
        WHEN ar.RequestBody LIKE '%"WasfatyInvoiceReference"%' THEN 'Has PascalCase (WRONG)'
        ELSE 'Unknown format'
    END AS PropertyCaseCheck,
    -- Show beginning of request body
    LEFT(ar.RequestBody, 500) AS RequestBodyStart,
    -- Show end of request body to check if complete
    RIGHT(ar.RequestBody, 100) AS RequestBodyEnd,
    -- Show full body (will truncate in grid, but you can export)
    ar.RequestBody AS FullRequestBody
FROM APIRequest ar
WHERE ar.RequestType = 'Invoice'
ORDER BY ar.RequestedAt DESC;

-- Simple check: just show the request body
SELECT TOP 1
    Id,
    RequestedAt,
    LEN(RequestBody) AS Length,
    RequestBody
FROM APIRequest
WHERE RequestType = 'Invoice'
ORDER BY RequestedAt DESC;

-- Get the ACTUAL API error message - THIS IS THE MOST IMPORTANT
SELECT TOP 1
    ar.Id,
    ar.RequestType,
    ar.RequestedAt,
    resp.StatusCode,
    resp.IsSuccess,
    resp.ErrorMessage,
    resp.ResponseBody,
    resp.DurationMs
FROM APIRequest ar
INNER JOIN APIResponse resp ON ar.Id = resp.APIRequestId
WHERE ar.RequestType = 'Invoice'
ORDER BY ar.RequestedAt DESC;
