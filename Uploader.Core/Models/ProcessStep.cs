namespace Uploader.Core.Models
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