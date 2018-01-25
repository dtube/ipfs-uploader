namespace Uploader.Models
{
    public enum ProcessStep
    {
        Init,
        Waiting,
        Canceled,
        Started,
        Error,
        Success
    }
}