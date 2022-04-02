using azuredevops_export_wiki;
using System;
using System.IO;
using Xunit;

namespace AzureDevOps.WikiPDFExport.Test
{
    public class ExportedWikiDocTest
    {
        const string WIKI_PATH = "../../../Tests/Code";
        DirectoryInfo codeWiki = new DirectoryInfo(Path.GetFullPath(WIKI_PATH, Environment.CurrentDirectory));

        [Fact]
        public void givenValidWikiBaseLocation_whenCtor_thenBaseIsFound()
        {
            var exportedWiki = new ExportedWikiDoc(WIKI_PATH);
            Assert.Equal(codeWiki.FullName, exportedWiki.baseDir.FullName);
        }

        [Fact]
        public void givenValidWikiSubLocation_whenCtor_thenBaseIsFound()
        {
            var exportedWiki = new ExportedWikiDoc($"{WIKI_PATH}/Another-Page");
            Assert.Equal(codeWiki.FullName, exportedWiki.baseDir.FullName);
        }

        [Fact]
        public void givenValidWikiSubSubLocation_whenCtor_thenBaseIsFound()
        {
            var exportedWiki = new ExportedWikiDoc($"{WIKI_PATH}/Another-Page/Sub-Page2");
            Assert.Equal(codeWiki.FullName, exportedWiki.baseDir.FullName);
        }

        [Fact]
        public void givenNoAttachmentsFolder_whenCtor_thenExceptionIsThrown()
        {
            Assert.Throws<WikiPdfExportException>(() => new ExportedWikiDoc("./"));
        }

        [Fact]
        public void givenNonExistingLocation_whenCtor_thenExceptionIsThrown()
        {
            Assert.Throws<WikiPdfExportException>(() => new ExportedWikiDoc("X:/this/hopefully/does/not/exist"));
        }

        [Fact]
        public void givenAbsoluteValidLocation_whenCtor_thenSuccessful()
        {
            Assert.NotEmpty(new ExportedWikiDoc(Path.GetFullPath(WIKI_PATH,
                Environment.CurrentDirectory)).baseDir.FullName);
        }

        [Fact]
        public void givenValidDirInfo_whenCtor_thenSuccessful()
        {
            Assert.NotEmpty(new ExportedWikiDoc(codeWiki).baseDir.FullName);
        }

        [Fact]
        public void givenExportDoc_whenGetProperties_thenNoneAreBlank()
        {
            var export = new ExportedWikiDoc(codeWiki);
            AssertExistingValidDirectory(export.exportDir);
            AssertExistingValidDirectory(export.baseDir);
            AssertExistingValidDirectory(export.attachments);
        }

        private static void AssertExistingValidDirectory(DirectoryInfo dir)
        {
            Assert.NotEmpty(dir.FullName);
            Assert.DoesNotMatch("^/s+$", dir.FullName);
            Assert.True(dir.Exists, $"{dir.Name} does not exist");
            Assert.True((dir.Extension.Length == 0 || dir.Extension == dir.Name),
                $"{dir.Name} has extension: {dir.Extension}");
        }
    }
}
