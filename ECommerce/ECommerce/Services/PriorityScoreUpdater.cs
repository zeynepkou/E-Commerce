using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class PriorityScoreUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer _timer;

    public PriorityScoreUpdater(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Timer başlat: 1 saniyede bir çalıştır
        _timer = new Timer(UpdateScores, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }


    private void UpdateScores(object state)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var pendingOrders = dbContext.Orders
                .Where(o => o.Status == "Pending")
                .ToList();

            foreach (var order in pendingOrders)
            {
                order.UpdatePriorityScore();
            }

            dbContext.SaveChanges();
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
