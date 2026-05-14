using System;

namespace CSD
{
    public sealed class UpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string Version { get; set; } = string.Empty;
        public int VersionCode { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string TargetOs { get; set; } = string.Empty;
        public string TargetArch { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string? Sha256 { get; set; }

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
        }

        public string ReleaseDateFormatted => ReleaseDate.ToString("yyyy-MM-dd HH:mm");
    }
}
