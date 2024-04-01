namespace Unido
{
    public enum DownloadStatus
    {
        Undefined,
        NotStarted,
        Started,
        SendingHeadRequest,
        SendingGetRequest,
        DownloadingContent,
        Completed,
        Cancelled,
        Failed,
    }
}
