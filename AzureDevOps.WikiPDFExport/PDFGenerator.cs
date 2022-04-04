using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.IO;
using System.Threading.Tasks;

namespace azuredevops_export_wiki
{
    internal class PDFGenerator
    {
        const int MAX_PAGE_SIZE = 100_000_000;
        private ILogger _logger;
        private Options _options;

        internal PDFGenerator(Options options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

#if HTML_IN_MEMORY
        internal async Task<string> ConvertHTMLToPDFAsync(string html)
#else
        internal async Task<string> ConvertHTMLToPDFAsync(SelfDeletingTemporaryFile tempHtmlFile)
#endif
        {
            _logger.Log("Converting HTML to PDF");
            var output = _options.Output;

            if (string.IsNullOrEmpty(output))
            {
                output = Path.Combine(Directory.GetCurrentDirectory(), "export.pdf");
            }

            if (string.IsNullOrEmpty(_options.ChromeExecutablePath))
            {
                _logger.Log("No Chrome path defined, downloading...");
                _ = await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                _logger.Log("Chrome ready.");
            }

            var launchOptions = new LaunchOptions
            {
                ExecutablePath = _options.ChromeExecutablePath ?? string.Empty,
                Headless = true, //set to false for easier debugging
                Args = new[] { "--no-sandbox", "--single-process" }, //required to launch in linux
                Devtools = false,
                Timeout = _options.ChromeTimeout * 1000
            };

            // TODO add logging to Puppeteer
            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            {
                var page = await browser.NewPageAsync();
#if HTML_IN_MEMORY
                _logger.Log($"Sending {html.Length:N0} bytes to Chrome...");
                if (html.Length > MAX_PAGE_SIZE)
                {
                    _logger.Log($"Operation may fail due to total size, try --exclude-paths to limit the number of pages", LogLevel.Warning);
                }
                await page.SetContentAsync(html);
                _logger.Log($"HTML page filled.");
#else
                var f = new FileInfo(tempHtmlFile.FilePath);
                _logger.Log($"Asking Chrome to open {f.Length:N0} bytes page at {tempHtmlFile.FilePath}...");
                if (f.Length > MAX_PAGE_SIZE)
                {
                    _logger.Log($"This may take a few minutes, given the file size.");
                }
                await page.GoToAsync($"file://{tempHtmlFile.FilePath}", launchOptions.Timeout);
                _logger.Log($"HTML file loaded.");
#endif

                //todo load header/footer template from file
                var pdfoptions = new PdfOptions();
                if (!string.IsNullOrEmpty(_options.HeaderTemplate)
                    || !string.IsNullOrEmpty(_options.FooterTemplate)
                    || !string.IsNullOrEmpty(_options.HeaderTemplatePath)
                    || !string.IsNullOrEmpty(_options.FooterTemplatePath))
                {

                    string footerTemplate = null;
                    string headerTemplate = null;
                    if (!string.IsNullOrEmpty(_options.HeaderTemplate))
                    {
                        headerTemplate = _options.HeaderTemplate;
                    }
                    else if (!string.IsNullOrEmpty(_options.HeaderTemplatePath))
                    {
                        headerTemplate = File.ReadAllText(_options.HeaderTemplatePath);
                    }

                    if (!string.IsNullOrEmpty(_options.FooterTemplate))
                    {
                        footerTemplate = _options.FooterTemplate;
                    }
                    else if (!string.IsNullOrEmpty(_options.FooterTemplatePath))
                    {
                        footerTemplate = File.ReadAllText(_options.FooterTemplatePath);
                    }

                    pdfoptions = new PdfOptions()
                    {
                        PrintBackground = true,
                        PreferCSSPageSize = false,
                        DisplayHeaderFooter = true,
                        MarginOptions = {
                        Top = "80px",
                        Bottom = "100px",
                        //left and right do not have an impact
                        Left = "100px",
                        Right = "100px"
                    },

                        Format = PuppeteerSharp.Media.PaperFormat.A4
                    };

                    pdfoptions.FooterTemplate = footerTemplate;
                    pdfoptions.HeaderTemplate = headerTemplate;

                }
                else
                {
                    pdfoptions.PrintBackground = _options.PrintBackground;
                }


                _logger.Log($"Generating PDF document...");
                await page.PdfAsync(output, pdfoptions);
                await browser.CloseAsync();
                _logger.Log($"PDF document is ready.");
            }

            _logger.Log($"PDF created at: {output}");
            return output;
        }
    }
}
