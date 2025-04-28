using System;
using System.IO;

namespace AzureDevOps.WikiPdfExport;

internal class SelfDeletingTemporaryFile : IDisposable
{
	private const int SIZE_THRESHOLD = 10_000_000; // TODO made up number, should measure
	private bool disposedValue;
	private readonly FileInfo fileInfo;
	public string FilePath => fileInfo.FullName;

	internal SelfDeletingTemporaryFile(long hintSize = 0, string? extension = null)
	{
		var path = Path.GetTempFileName();
		if (extension is not null)
		{
			var newPath = Path.ChangeExtension(path, extension);
			File.Move(path, newPath);
			path = newPath;
		}
		fileInfo = new FileInfo(path);
		if (hintSize <= SIZE_THRESHOLD)
		{
			fileInfo.Attributes = FileAttributes.Temporary;
		}
	}

	internal void KeepAs(string newPath, bool overwrite = true)
	{
		var targetPath = Path.GetFullPath(newPath);
		if (overwrite && File.Exists(targetPath))
		{
			File.Delete(targetPath);
		}
		fileInfo.MoveTo(targetPath);
		disposedValue = true; // no need to dispose i.e. delete anymore
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				fileInfo.Delete();
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
