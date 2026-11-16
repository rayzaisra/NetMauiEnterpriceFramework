USE [__DB_NAME__];
GO

CREATE OR ALTER PROCEDURE dbo.usp_v2_get_recent_notification
    @UserId nvarchar(100),
    @Top int = 20
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top) n.id AS Id, n.receiver_user_id AS ReceiverUserId, n.reff_id AS ReffId,
           n.is_seen AS IsSeen, n.created_date AS CreatedDate, n.created_by AS CreatedBy,
           t.short_text AS ShortText, t.long_text AS LongText, t.href AS Href
    FROM dbo.tb_notification n
    LEFT JOIN dbo.rf_notification_type t ON t.id = n.notification_type_id
    WHERE n.receiver_user_id = @UserId
      AND ISNULL(n.is_deleted, 0) = 0
    ORDER BY n.created_date DESC;
END;
GO
