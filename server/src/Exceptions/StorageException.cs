namespace server.src.Exceptions;

public class StorageException : Exception
{
    public StorageException(string message, string objectName, Exception innerException) : base(message, innerException)
    {
        ObjectName = objectName;
        Data["object"] = objectName;
    }

    public string ObjectName { get; }
}
