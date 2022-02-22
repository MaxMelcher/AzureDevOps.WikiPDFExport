[System.Serializable]
public class WikiPdfExportException : System.Exception
{
    public WikiPdfExportException() { }
    public WikiPdfExportException(string message) : base(message) { }
    public WikiPdfExportException(string message, System.Exception inner) : base(message, inner) { }
    protected WikiPdfExportException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}