using PuppeteerSharp;
using System.IO;
using System.Threading.Tasks;

namespace azuredevops_export_wiki
{
    internal class PDFGenerator
    {
        private ILogger _logger;
        private Options _options;

        internal PDFGenerator(Options options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        internal async Task<string> ConvertHTMLToPDFAsync(string html)
        {
            _logger.Log("Converting HTML to PDF");
            var output = _options.Output;

            if (output == null)
            {
                output = Path.Combine(Directory.GetCurrentDirectory(), "export.pdf");
            }

            if (string.IsNullOrEmpty(_options.ChromeExecutablePath))
            {
                RevisionInfo revisionInfo = await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            }

            var launchOptions = new LaunchOptions
            {
                ExecutablePath = _options.ChromeExecutablePath ?? string.Empty,
                Headless = true, //set to false for easier debugging
                Args = new[] { "--no-sandbox", "--single-process" }, //required to launch in linux
                Devtools = false
            };

            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            {
                var page = await browser.NewPageAsync();
                await page.SetContentAsync(html);

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


                await page.PdfAsync(output, pdfoptions);
                await browser.CloseAsync();
            }

            _logger.Log($"PDF created at: {output}");
            return output;
        }
    }
}
