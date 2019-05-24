using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DinkToPdf;

namespace azuredevops_export_wiki
{
    class Program
    {
        static void Main(string[] args)
        {
            var timer = Stopwatch.StartNew();
            Console.WriteLine("Reading .order file");
            var path = @"C:\git\Melcher.it.wiki";

            var files = ReadOrderFiles(path);


            StringBuilder sb = new StringBuilder();
            for(var i = 0; i < files.Count; i++)
            {
                var file = new FileInfo(files[i]);
                Console.WriteLine($"file {file.Name}");
                var htmlfile = file.FullName.Replace(".md", ".html");

                var md = File.ReadAllText(file.FullName);

                var html = CommonMark.CommonMarkConverter.Convert(md);
                
                if (i +1 < files.Count)
                {
                    html = "<div style='page-break-after: always;'>" + html + "</div>";
                }

                Console.WriteLine(html);
                sb.Append(html);
            }

            var converter = new BasicConverter(new PdfTools());
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Out = $"C:\\git\\Melcher.it.wiki\\out.pdf"
                },
                Objects = {
                    new ObjectSettings() {
                        PagesCount = true,
                        HtmlContent = sb.ToString(),
                        WebSettings = { DefaultEncoding = "utf-8" },
                        HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true, Spacing = 2.812},
                        FooterSettings = { Left = $"{DateTime.Now.ToString("g")}"}
                    }
                }
            };

            byte[] pdf = converter.Convert(doc);
        }

        private static List<string> ReadOrderFiles(string path)
        {
            var directory = new DirectoryInfo(path);
            var orderFiles = directory.GetFiles(".order", SearchOption.AllDirectories);

            var result = new List<string>();
            foreach (var orderFile in orderFiles)
            {
                var orders = File.ReadAllLines(orderFile.FullName);

                foreach (var order in orders)
                {
                    result.Add($"{orderFile.Directory.FullName}\\{order}.md");
                }
            }

            return result;
        }
    }
}
