using System.Data;

namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal sealed partial class StudioRepository
{
    public Task<DataTable> ReportAsync(ReportKind kind,DateTime from,DateTime to)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        if (to.Date < from.Date || from.Year < 2000 || to.Year > 2099)
            throw new StudioRuleException("Выберите корректный период отчёта.");
        string sql = kind switch
        {
            ReportKind.Rooms => """
                WITH Rented AS (
                    SELECT b.RoomId,t.Total, DATEDIFF(MINUTE,
                        CASE WHEN b.StartsAt>@from THEN b.StartsAt ELSE @from END,
                        CASE WHEN usage.EndsAt<@to THEN usage.EndsAt ELSE @to END) AS Minutes
                    FROM dbo.Bookings b JOIN dbo.BookingTotals t ON t.Id=b.Id
                    CROSS APPLY (SELECT CASE WHEN b.CompletedAt<b.EndsAt THEN b.CompletedAt ELSE b.EndsAt END) usage(EndsAt)
                    WHERE b.Status IN (N'Confirmed',N'Paid',N'InWork',N'Completed')
                      AND b.StartsAt<@to AND usage.EndsAt>@from AND usage.EndsAt>b.StartsAt
                ), Revenue AS (
                    SELECT b.RoomId,SUM(p.Amount) AS Paid FROM dbo.Payments p
                    JOIN dbo.Bookings b ON b.Id=p.BookingId WHERE p.PaidAt>=@from AND p.PaidAt<@to GROUP BY b.RoomId
                )
                SELECT r.Name [Зал],CAST(COALESCE(SUM(x.Minutes),0)/60.0 AS decimal(14,2)) [Часы в аренде],
                    COALESCE(v.Paid,0) [Выручка, ₽],CAST(COALESCE(AVG(x.Total),0) AS decimal(14,2)) [Средний чек, ₽]
                FROM dbo.Rooms r LEFT JOIN Rented x ON x.RoomId=r.Id LEFT JOIN Revenue v ON v.RoomId=r.Id
                GROUP BY r.Name,v.Paid ORDER BY r.Name;
                """,
            ReportKind.Props => """
                WITH Turnover AS (
                    SELECT l.PropId,COUNT(*) AS Issues,SUM(l.Quantity*x.Rate) AS Cost
                    FROM dbo.Loans l JOIN dbo.Reservations x ON x.BookingId=l.BookingId AND x.PropId=l.PropId
                    WHERE l.IssuedAt>=@from AND l.IssuedAt<@to GROUP BY l.PropId
                ), RepairTime AS (
                    SELECT PropId,SUM(DATEDIFF_BIG(SECOND,
                        CASE WHEN StartedAt>@from THEN StartedAt ELSE @from END,
                        CASE WHEN COALESCE(EndedAt,@now)<@to THEN COALESCE(EndedAt,@now) ELSE @to END)) AS Seconds
                    FROM dbo.Repairs WHERE StartedAt<@to AND COALESCE(EndedAt,@now)>@from
                    GROUP BY PropId
                )
                SELECT p.Name [Реквизит],COALESCE(t.Issues,0) [Выдачи],COALESCE(t.Cost,0) [Стоимость аренды, ₽],
                    CAST(COALESCE(r.Seconds,0)/86400.0 AS decimal(14,2)) [Дни в ремонте]
                FROM dbo.Props p LEFT JOIN Turnover t ON t.PropId=p.Id LEFT JOIN RepairTime r ON r.PropId=p.Id
                ORDER BY p.Name;
                """,
            ReportKind.Debt => """
                SELECT c.FullName [Клиент],b.Id [Бронь],t.Total-t.Paid [Остаток, ₽],
                    CONVERT(date,COALESCE(b.DueOn,CONVERT(date,b.EndsAt))) [Срок оплаты],
                    CASE WHEN DATEADD(DAY,1,COALESCE(b.DueOn,CONVERT(date,b.EndsAt)))<=@now
                        THEN DATEADD(DAY,1,COALESCE(b.DueOn,CONVERT(date,b.EndsAt))) END [Дата просрочки]
                FROM dbo.Bookings b JOIN dbo.BookingTotals t ON t.Id=b.Id JOIN dbo.Clients c ON c.Id=b.ClientId
                WHERE b.Status IN (N'Confirmed',N'Paid',N'InWork',N'Completed') AND t.Total>t.Paid
                  AND COALESCE(b.DueOn,CONVERT(date,b.EndsAt))>=@from AND COALESCE(b.DueOn,CONVERT(date,b.EndsAt))<@to
                ORDER BY [Срок оплаты],c.FullName;
                """,
            ReportKind.Quality => """
                SELECT s.FullName [Фотограф],COUNT(b.Id) [Завершено],
                    CAST(AVG(CASE WHEN m.ReadyAt IS NULL THEN NULL WHEN m.ReadyAt<b.EndsAt THEN 0.0
                        ELSE DATEDIFF_BIG(SECOND,b.EndsAt,m.ReadyAt)/3600.0 END) AS decimal(14,2)) [Обработка, ч],
                    CAST(COALESCE(100.0*SUM(CONVERT(int,b.QualityIssue))/NULLIF(COUNT(b.Id),0),0)
                        AS decimal(5,2)) [Возврат или пересъёмка, %]
                FROM dbo.Staff s LEFT JOIN dbo.Bookings b ON b.PhotographerId=s.Id AND b.Status=N'Completed'
                    AND b.CompletedAt>=@from AND b.CompletedAt<@to
                OUTER APPLY(SELECT MAX(ReadyAt) AS ReadyAt FROM dbo.Materials WHERE BookingId=b.Id AND Kind=N'Final') m
                WHERE s.Role=N'Photo' GROUP BY s.FullName,s.Id ORDER BY s.FullName;
                """,
            ReportKind.Conflicts => """
                SELECT N'Отмена' [Событие],CancelReason [Причина или ресурс],COUNT(*) [Количество]
                FROM dbo.Bookings WHERE CancelledAt>=@from AND CancelledAt<@to GROUP BY CancelReason
                UNION ALL
                SELECT N'Конфликт зала',r.Name,COUNT(*) FROM dbo.Conflicts c JOIN dbo.Rooms r ON r.Id=c.RoomId
                WHERE c.RoomConflict=1 AND c.LoggedAt>=@from AND c.LoggedAt<@to GROUP BY r.Name
                UNION ALL
                SELECT N'Конфликт фотографа',s.FullName,COUNT(*) FROM dbo.Conflicts c JOIN dbo.Staff s ON s.Id=c.PhotographerId
                WHERE c.PhotoConflict=1 AND c.LoggedAt>=@from AND c.LoggedAt<@to GROUP BY s.FullName,s.Id
                ORDER BY [Событие],[Причина или ресурс];
                """,
            _ => throw new StudioRuleException("Выберите отчёт.")
        };
        return Read(db => db.TableAsync(sql,("@from",from.Date),("@to",to.Date.AddDays(1)),("@now",Now())));
    }
}
