--Version:7.0.0
--Description: Updates database to use components new name ColumnForbidder

UPDATE ProcessTask 
set ProcessTaskType = REPLACE(ProcessTaskType,'ColumnBlacklister','ColumnForbidder')
WHERE
ProcessTaskType like '%ColumnBlacklister%'