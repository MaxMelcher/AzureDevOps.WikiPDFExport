using System.IO;

namespace AzureDevOps.WikiPDFExport.Test.Helpers
{
    public static class FileComparer
    {
        /// <summary>
        /// Compares the content of two files
        /// </summary>
        /// <param name="expectedFilePath"></param>
        /// <param name="actualFilePath"></param>
        /// <returns>True if content is identical</returns>
        public static bool SameContent(string expectedFilePath, string actualFilePath)
        {
            // HACK works for small stuff but for big stuff better using streams and hash functions, also eats lot of memory
            var expected = File.ReadAllBytes(expectedFilePath);
            var actual = File.ReadAllBytes(actualFilePath);
            if (expected.Length != actual.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i]) return false;
            }
            return true;
        }
    }
}
