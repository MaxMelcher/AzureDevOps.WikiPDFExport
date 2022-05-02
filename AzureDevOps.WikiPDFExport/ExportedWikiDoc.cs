

using System;
using System.IO;

namespace azuredevops_export_wiki
{
    /// <summary>
    /// Immutable class representing information about an export from an Azure Devops
    /// Wiki
    /// </summary>
    public class ExportedWikiDoc
    {

        /// <summary>
        /// Create a wiki export from a path to the base of the export (not the wiki)
        /// </summary>
        public ExportedWikiDoc(String exportBase) : this(new DirectoryInfo(exportBase)) { }


        /// <summary>
        /// Create a wiki export from a path to the base of the export (not the wiki)
        /// </summary>
        public ExportedWikiDoc(DirectoryInfo exportBase)
        {
            if (!exportBase.Exists)
            {
                throw new WikiPdfExportException($"The wiki export location {exportBase} does not exist");
            }
            this.exportDir = exportBase;
            this.baseDir = FindNearestParentAttachmentsDirectory(exportBase);
        }

        /// <summary>
        /// Search from the given existing directory upwards until a folder containing
        /// an 'attachment' folder is identified.
        /// </summary>
        /// <returns>
        /// A valid existing directory which contains an attachments folder
        /// </returns>
        /// <throws>
        /// WikiPdfExportException if an attachments folder is not found before hitting the root.
        /// </throws>
        public static DirectoryInfo FindNearestParentAttachmentsDirectory(DirectoryInfo exportBase)
        {
            DirectoryInfo[] attDirs = exportBase.GetDirectories("./.attachments");
            if (attDirs != null && attDirs.Length > 0)
            {
                return attDirs[0].Parent;
            }
            if (null != exportBase.Parent)
            {
                return FindNearestParentAttachmentsDirectory(exportBase.Parent);
            }

            //return the base path and hope for the best
            return exportBase;
        }

        public DirectoryInfo exportDir { get; }
        public DirectoryInfo baseDir { get; }
        public string exportPath()
        {
            return exportDir.FullName;
        }
        public string basePath()
        {
            return baseDir.FullName;
        }
    }
}
