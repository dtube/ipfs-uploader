namespace Uploader.Core.Models
{
    internal enum ProcessStep
    {
        Init,
        Waiting,
        Canceled,
        Started,
        Error,
        Success
    }
}