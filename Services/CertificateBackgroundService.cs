using Ecoex_Academy_Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

public class CertificateBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CertificateBackgroundService> _logger;

    public CertificateBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CertificateBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Certificate Background Service started."
        );

        TimeZoneInfo indiaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "India Standard Time"
                    : "Asia/Kolkata"
            );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DateTime utcNow = DateTime.UtcNow;

                DateTime istNow =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        utcNow,
                        indiaTimeZone
                    );

                _logger.LogInformation(
                    "Certificate scheduler checking IST time: {Time}",
                    istNow
                );

                int? courseId = null;

                // 13 September 2026 - 3:00 PM IST
                // Course ID = 2
                if (istNow.Date == new DateTime(2026, 9, 13).Date &&
                    istNow.Hour == 15 &&
                    istNow.Minute == 05)
                {
                    courseId = 2;
                }

                // 20 September 2026 - 4:00 PM IST
                // Course ID = 1
                else if (
                    istNow.Date == new DateTime(2026, 9, 20).Date &&
                    istNow.Hour == 15 &&
                    istNow.Minute == 05)
                {
                    courseId = 1;
                }

                if (courseId.HasValue)
                {
                    _logger.LogInformation(
                        "Certificate scheduled time reached. CourseId: {CourseId}, IST: {Time}",
                        courseId.Value,
                        istNow
                    );

                    using IServiceScope scope =
                        _scopeFactory.CreateScope();

                    var certificateService =
                        scope.ServiceProvider
                            .GetRequiredService<ICertificateServices>();

                    await certificateService.SendCertificatesAsync(
                        courseId.Value,
                        stoppingToken
                    );

                    await Task.Delay(
                        TimeSpan.FromMinutes(1),
                        stoppingToken
                    );
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in Certificate Background Service."
                );
            }

            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken
            );
        }
        _logger.LogInformation(
            "Certificate Background Service stopped."
        );
    }
}