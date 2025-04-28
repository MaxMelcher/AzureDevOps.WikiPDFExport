[System.Serializable]
internal class WikiPdfExportException : System.Exception
{
	public WikiPdfExportException() { }
	public WikiPdfExportException(string message) : base(message) { }
	public WikiPdfExportException(string message, System.Exception inner) : base(message, inner) { }
}
