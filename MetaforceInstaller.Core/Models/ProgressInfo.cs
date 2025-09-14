namespace MetaforceInstaller.Core.Models;

public class ProgressInfo
{
    public int PercentageComplete { get; set; }
    public long BytesTransferred { get; set; }
    public long TotalBytes { get; set; }
    public string? Message { get; set; }
    public string? CurrentFile { get; set; }
    public ProgressType Type { get; set; }
}

public enum ProgressType
{
    Installation,
    FileCopy,
    Extraction,
    General
}
