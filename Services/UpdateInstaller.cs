using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;




namespace CSD.Services
{
    public sealed class UpdateInstaller
    {
        private static readonly HttpClient _httpClient = new();

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;
        public event EventHandler<string>? StatusChanged;

        public async Task<(bool Success, string? ErrorMessage)> DownloadAndInstallAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default)
        {
            string? tempDir = null;
            try
            {
                OnStatusChanged("正在下载更新包...");

                tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                var zipPath = Path.Combine(tempDir, "update.zip");
                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                long downloadedBytes = 0;

                using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (int)((downloadedBytes * 100) / totalBytes);
                            OnDownloadProgressChanged(progress, downloadedBytes, totalBytes);
                        }
                    }
                }

                OnStatusChanged("下载完成，正在校验文件...");

                if (!string.IsNullOrWhiteSpace(updateInfo.Sha256))
                {
                    if (!await VerifySha256Async(zipPath, updateInfo.Sha256))
                    {
                        var errorMsg = "文件校验失败，SHA256不匹配";
                        OnStatusChanged(errorMsg);
                        return (false, errorMsg);
                    }
                    OnStatusChanged("文件校验通过");
                }

                OnStatusChanged("正在解压更新包...");

                ZipFile.ExtractToDirectory(zipPath, extractDir);

                OnStatusChanged("正在查找安装文件...");

                var sourceDir = FindSourceDirectory(extractDir);
                if (string.IsNullOrEmpty(sourceDir))
                {
                    var errorMsg = "压缩包损坏：未找到CSD.exe或publish文件夹";
                    OnStatusChanged(errorMsg);
                    return (false, errorMsg);
                }

                var appDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                if (string.IsNullOrEmpty(appDir))
                {
                    var errorMsg = "无法获取程序目录";
                    OnStatusChanged(errorMsg);
                    return (false, errorMsg);
                }

                OnStatusChanged("正在安装更新文件...");

                var exePath = Path.Combine(appDir, "CSD.exe");
                var currentExeExists = File.Exists(exePath);

                if (currentExeExists)
                {
                    // 将所有文件（包括exe）复制到临时目录，通过批处理脚本在程序退出后替换
                    var batchScript = CreateRestartScript(appDir, sourceDir);

                    OnStatusChanged("更新完成，正在重启程序...");

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{batchScript}\"",
                        WorkingDirectory = appDir,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    Process.Start(startInfo);
                }
                else
                {
                    CopyDirectoryContents(sourceDir, appDir, excludeFiles: null);

                    OnStatusChanged("更新完成，正在启动程序...");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(appDir, "CSD.exe"),
                        WorkingDirectory = appDir
                    });
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                var errorMsg = $"更新失败：{ex.Message}";
                OnStatusChanged(errorMsg);
                return (false, errorMsg);
            }
            finally
            {
                // 不在此处删除临时目录，批处理脚本会在复制完成后自行清理
            }
        }

        private string CreateRestartScript(string appDir, string sourceDir)
        {
            var scriptPath = Path.Combine(appDir, "_update.bat");
            var logPath = Path.Combine(appDir, "_update.log");

            // 收集所有需要复制的文件
            var copyCommands = new StringBuilder();
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(appDir, fileName);
                var label = $"copy_{fileName.Replace(".", "_").Replace("-", "_")}";
                // 使用重试机制：最多等待30秒
                copyCommands.AppendLine($@"
echo Copying {fileName}... >> ""{logPath}""
set ""target={destFile}""
set ""src={file}""
set retries=0
:{label}
copy /y ""%src%"" ""%target%"" >> ""{logPath}"" 2>&1
if %errorlevel% neq 0 (
    set /a retries+=1
    if %retries% lss 30 (
        timeout /t 1 /nobreak >nul
        goto {label}
    )
    echo Failed to copy {fileName} after 30 retries >> ""{logPath}""
)");
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(appDir, dirName);
                copyCommands.AppendLine($"echo Copying directory {dirName}... >> \"{logPath}\"");
                copyCommands.AppendLine($"xcopy /e /y /i \"{subDir}\" \"{destSubDir}\" >> \"{logPath}\" 2>&1");
            }

            var batchContent = $@"
@echo off
echo Update script started at %date% %time% >> ""{logPath}""
timeout /t 3 /nobreak >nul
{copyCommands}
echo Starting CSD.exe... >> ""{logPath}""
start """" ""{Path.Combine(appDir, "CSD.exe")}""
echo Update completed at %date% %time% >> ""{logPath}""
echo Cleaning up... >> ""{logPath}""
del /f /q ""{logPath}"" 2>nul
rd /s /q ""{sourceDir}"" 2>nul
del /f /q ""%~f0""
";
            File.WriteAllText(scriptPath, batchContent, new UTF8Encoding(false));
            return scriptPath;
        }

        private string? FindSourceDirectory(string extractDir)
        {
            if (File.Exists(Path.Combine(extractDir, "CSD.exe")))
            {
                return extractDir;
            }

            var publishDir = Directory.GetDirectories(extractDir, "publish", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (publishDir != null && File.Exists(Path.Combine(publishDir, "CSD.exe")))
            {
                return publishDir;
            }

            var subDirs = Directory.GetDirectories(extractDir);
            foreach (var subDir in subDirs)
            {
                if (File.Exists(Path.Combine(subDir, "CSD.exe")))
                {
                    return subDir;
                }

                var nestedPublish = Directory.GetDirectories(subDir, "publish", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (nestedPublish != null && File.Exists(Path.Combine(nestedPublish, "CSD.exe")))
                {
                    return nestedPublish;
                }
            }

            return null;
        }

        private void CopyDirectoryContents(string sourceDir, string destDir, string[]? excludeFiles)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                if (excludeFiles != null && excludeFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var destFile = Path.Combine(destDir, fileName);
                var retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        File.Copy(file, destFile, overwrite: true);
                        break;
                    }
                    catch (IOException)
                    {
                        retries--;
                        if (retries == 0) throw;
                        Thread.Sleep(100);
                    }
                }
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectoryContents(subDir, destSubDir, excludeFiles);
            }
        }

        private async Task<bool> VerifySha256Async(string filePath, string expectedHash)
        {
            using var sha256 = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var hashBytes = await sha256.ComputeHashAsync(stream);

            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return hashString.Equals(expectedHash.ToLowerInvariant());
        }

        private void OnDownloadProgressChanged(int percentage, long downloadedBytes, long totalBytes)
        {
            DownloadProgressChanged?.Invoke(this, new DownloadProgressEventArgs(percentage, downloadedBytes, totalBytes));
        }

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }
    }

    public sealed class DownloadProgressEventArgs : EventArgs
    {
        public int Percentage { get; }
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }

        public DownloadProgressEventArgs(int percentage, long downloadedBytes, long totalBytes)
        {
            Percentage = percentage;
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
        }
    }
}
