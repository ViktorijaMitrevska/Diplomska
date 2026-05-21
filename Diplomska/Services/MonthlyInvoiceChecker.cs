using Diplomska.Models;
using Diplomska.Services;
using Microsoft.Extensions.Hosting;

public class MonthlyInvoiceChecker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MonthlyInvoiceChecker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromHours(6));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (DateTime.Now.Day != 1)
                continue;

            using var scope = _scopeFactory.CreateScope();
            var firestore = scope.ServiceProvider.GetRequiredService<FirestoreService>();

            await CheckUnpaidFromPreviousMonth(firestore);

            if (DateTime.Now.Month == 1)
            {
                await CheckUnpaidFromPreviousYear(firestore);
            }
        }
    }

    private async Task CheckUnpaidFromPreviousMonth(FirestoreService firestore)
    {
        var now = DateTime.Now;

        var startPrevMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        var endPrevMonth = new DateTime(now.Year, now.Month, 1);

        var fakturi = await firestore.GetFakturaAsync();

        var neplateni = fakturi
            .Where(f =>
                !f.DaliEPlateno &&
                f.PriemDate >= startPrevMonth &&
                f.PriemDate < endPrevMonth)
            .ToList();

        if (neplateni.Any())
        {
            // тука оди известувањето
            await SendNotification(firestore, neplateni);
        }
    }
    private async Task CheckUnpaidFromPreviousYear(FirestoreService firestore)
    {
        int previousYear = DateTime.Now.Year - 1;

        var startYear = new DateTime(previousYear, 1, 1);
        var endYear = new DateTime(previousYear + 1, 1, 1);

        var fakturi = await firestore.GetFakturaAsync();

        var neplateni = fakturi
            .Where(f =>
                !f.DaliEPlateno &&
                f.PriemDate >= startYear &&
                f.PriemDate < endYear)
            .ToList();

        if (!neplateni.Any())
            return;

        var notification = new Notification
        {
            Title = "Годишни неплатени фактури",
            Message = $"Имате {neplateni.Count} неплатени фактури од {previousYear} година.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await firestore.AddNotificationAsync(notification);
    }

    private async Task SendNotification(
    FirestoreService firestore,
    List<VleznaFaktura> fakturi)
    {
        var notification = new Notification
        {
            Title = "Неплатени фактури",
            Message = $"Имате {fakturi.Count} неплатени фактури од претходниот месец.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await firestore.AddNotificationAsync(notification);
    }

}

