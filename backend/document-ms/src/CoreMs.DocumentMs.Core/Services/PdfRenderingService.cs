using CoreMs.Common.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CoreMs.DocumentMs.Core.Services;

/// <summary>
/// Renders HTML content to PDF using Playwright (headless Chromium).
/// Browser instance is lazily initialized and reused across requests.
/// </summary>
[Service(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)]
public class PdfRenderingService : IAsyncDisposable
{
    private readonly ILogger<PdfRenderingService> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public PdfRenderingService(ILogger<PdfRenderingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert HTML content to a PDF byte array.
    /// </summary>
    public async Task<byte[]> RenderHtmlToPdfAsync(string htmlContent, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync();
        var page = await browser.NewPageAsync();

        try
        {
            await page.SetContentAsync(htmlContent, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var pdfBytes = await page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                PrintBackground = true,
                Margin = new Margin
                {
                    Top = "20mm",
                    Bottom = "20mm",
                    Left = "15mm",
                    Right = "15mm"
                }
            });

            return pdfBytes;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsConnected: true }) return _browser;

        await _initLock.WaitAsync();
        try
        {
            if (_browser is { IsConnected: true }) return _browser;

            _logger.LogInformation("Initializing Playwright browser...");
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
            });
            _logger.LogInformation("Playwright browser initialized");

            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _initLock.Dispose();
    }
}
