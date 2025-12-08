namespace TaoSlideTotNghiep.DTOs.Requests
{
    #region Records

    /// <summary>
    /// Base download request.
    /// </summary>
    public abstract record DownloadRequest(string Url, string FilePath) : Request(RequestType.Download), IFilePathBased;

    #endregion
}