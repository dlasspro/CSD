using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CSD.Services
{
    public class GpuData
    {
        public string Name { get; set; } = "未知";
        public double VramGb { get; set; }
        public string DriverVersion { get; set; } = "未知";
    }

    public class DriveData
    {
        public string Name { get; set; } = "未知";
        public string MediaType { get; set; } = "未知";
        public double TotalGb { get; set; }
    }

    public class LogicalDriveData
    {
        public string Letter { get; set; } = "";
        public double TotalGb { get; set; }
        public double AvailableGb { get; set; }
    }

    public class CpuData
    {
        public string Model { get; set; } = "未知";
        public int Cores { get; set; }
        public int LogicalProcessors { get; set; }
        public string Frequency { get; set; } = "未知";
        public uint L2CacheKb { get; set; }
        public uint L3CacheKb { get; set; }
    }

    public class DisplayData
    {
        public string Resolution { get; set; } = "未知";
        public int RefreshRate { get; set; }
    }

    public class PerformanceInfo
    {
        // Motherboard
        public string Motherboard { get; set; } = "未知";
        public string BiosVersion { get; set; } = "未知";

        // CPU
        public List<CpuData> Cpus { get; set; } = new();

        // RAM
        public double RamTotalGb { get; set; }
        public double RamAvailableGb { get; set; }
        public uint RamSpeed { get; set; }
        public int RamSlotsUsed { get; set; }
        public string RamType { get; set; } = "未知";

        // GPU
        public List<GpuData> Gpus { get; set; } = new();

        // Storage
        public List<DriveData> Drives { get; set; } = new();
        public List<LogicalDriveData> LogicalDrives { get; set; } = new();

        // Display
        public List<DisplayData> Displays { get; set; } = new();

        // Software
        public string OsVersion { get; set; } = "未知";
        public string OsArchitecture { get; set; } = "未知";
        public string BrowserInfo { get; set; } = "未知";
        public int RunningServicesCount { get; set; }
        public int TotalProcessesCount { get; set; }
        public double BackgroundProcessRatio { get; set; }

        // App
        public double AppMemoryMb { get; set; }
        public double AppCpuUsage { get; set; }

        // Score
        public int Score { get; set; }
        public string Rating { get; set; } = "";
        public string RatingDescription { get; set; } = "";
    }

    public static class PerformanceService
    {
        public static PerformanceInfo GetPerformanceInfo()
        {
            var info = new PerformanceInfo();
            try
            {
                GetHardwareInfo(info);
                GetSoftwareInfo(info);
                CalculateScore(info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting performance info: {ex.Message}");
            }
            return info;
        }

        private static void GetHardwareInfo(PerformanceInfo info)
        {
            // Motherboard
            try
            {
                using var searcher = new ManagementObjectSearcher("select Manufacturer, Product from Win32_BaseBoard");
                foreach (var obj in searcher.Get())
                {
                    info.Motherboard = $"{obj["Manufacturer"]} {obj["Product"]}".Trim();
                    break;
                }
                using var searcherBios = new ManagementObjectSearcher("select SMBIOSBIOSVersion from Win32_BIOS");
                foreach (var obj in searcherBios.Get())
                {
                    info.BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? "未知";
                    break;
                }
            }
            catch { }

            // CPU
            try
            {
                using var searcher = new ManagementObjectSearcher("select Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize from Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    var cpu = new CpuData();
                    cpu.Model = obj["Name"]?.ToString() ?? "未知";
                    cpu.Cores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                    cpu.LogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                    cpu.Frequency = (Convert.ToDouble(obj["MaxClockSpeed"] ?? 0) / 1000.0).ToString("0.00") + " GHz";
                    cpu.L2CacheKb = Convert.ToUInt32(obj["L2CacheSize"] ?? 0);
                    cpu.L3CacheKb = Convert.ToUInt32(obj["L3CacheSize"] ?? 0);
                    info.Cpus.Add(cpu);
                }
            }
            catch { }

            // RAM (Capacity/Available)
            try
            {
                using var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize, FreePhysicalMemory from Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    info.RamTotalGb = Convert.ToDouble(obj["TotalVisibleMemorySize"] ?? 0) / 1024.0 / 1024.0;
                    info.RamAvailableGb = Convert.ToDouble(obj["FreePhysicalMemory"] ?? 0) / 1024.0 / 1024.0;
                    break;
                }
            }
            catch { }

            // RAM (Details)
            try
            {
                using var searcher = new ManagementObjectSearcher("select Speed, SMBIOSMemoryType, MemoryType from Win32_PhysicalMemory");
                foreach (var obj in searcher.Get())
                {
                    info.RamSlotsUsed++;
                    if (info.RamSpeed == 0) info.RamSpeed = Convert.ToUInt32(obj["Speed"] ?? 0);
                    int smbiosType = Convert.ToInt32(obj["SMBIOSMemoryType"] ?? 0);
                    int memType = Convert.ToInt32(obj["MemoryType"] ?? 0);
                    int typeCode = smbiosType > 0 ? smbiosType : memType;

                    if (typeCode == 20) info.RamType = "DDR";
                    else if (typeCode == 21) info.RamType = "DDR2";
                    else if (typeCode == 24) info.RamType = "DDR3";
                    else if (typeCode == 26) info.RamType = "DDR4";
                    else if (typeCode == 34 || typeCode == 35) info.RamType = "DDR5";
                }
                if (info.RamType == "未知") info.RamType = "未知类型";
            }
            catch { }

            // GPU (Multiple)
            try
            {
                using var searcher = new ManagementObjectSearcher("select Name, AdapterRAM, DriverVersion from Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var gpu = new GpuData();
                    gpu.Name = obj["Name"]?.ToString() ?? "未知";
                    long vram = Convert.ToInt64(obj["AdapterRAM"] ?? 0);
                    gpu.VramGb = vram / 1024.0 / 1024.0 / 1024.0;
                    if (gpu.VramGb < 0) gpu.VramGb = 0; // sometimes returns negative if > 2GB on old WMI
                    gpu.DriverVersion = obj["DriverVersion"]?.ToString() ?? "未知";
                    info.Gpus.Add(gpu);
                }
            }
            catch { }

            // Storage (Physical)
            try
            {
                using var searcher = new ManagementObjectSearcher(@"\\.\root\microsoft\windows\storage", "select FriendlyName, Size, MediaType from MSFT_PhysicalDisk");
                foreach (var obj in searcher.Get())
                {
                    var drive = new DriveData();
                    drive.Name = obj["FriendlyName"]?.ToString() ?? "未知";
                    drive.TotalGb = Convert.ToUInt64(obj["Size"] ?? 0) / 1024.0 / 1024.0 / 1024.0;
                    int mediaType = Convert.ToInt32(obj["MediaType"] ?? 0);
                    drive.MediaType = mediaType == 4 ? "SSD" : mediaType == 3 ? "HDD" : mediaType == 5 ? "NVMe/SCM" : "未知";
                    info.Drives.Add(drive);
                }
            }
            catch
            {
                // Fallback
                try
                {
                    using var searcher = new ManagementObjectSearcher("select Model, Size from Win32_DiskDrive");
                    foreach (var obj in searcher.Get())
                    {
                        var drive = new DriveData();
                        drive.Name = obj["Model"]?.ToString() ?? "未知";
                        drive.TotalGb = Convert.ToUInt64(obj["Size"] ?? 0) / 1024.0 / 1024.0 / 1024.0;
                        drive.MediaType = "未知";
                        info.Drives.Add(drive);
                    }
                }
                catch { }
            }

            // Storage (Logical)
            try
            {
                foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
                {
                    info.LogicalDrives.Add(new LogicalDriveData
                    {
                        Letter = d.Name,
                        TotalGb = d.TotalSize / 1024.0 / 1024.0 / 1024.0,
                        AvailableGb = d.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0
                    });
                }
            }
            catch { }

            // Display
            try
            {
                // To properly support multiple monitors, we use WMI DesktopMonitor or monitor info
                using var searcher = new ManagementObjectSearcher("select ScreenWidth, ScreenHeight from Win32_DesktopMonitor");
                foreach (var obj in searcher.Get())
                {
                    int width = Convert.ToInt32(obj["ScreenWidth"] ?? 0);
                    int height = Convert.ToInt32(obj["ScreenHeight"] ?? 0);
                    if (width > 0 && height > 0)
                    {
                        info.Displays.Add(new DisplayData 
                        { 
                            Resolution = $"{width} x {height}", 
                            RefreshRate = 60 // Win32_DesktopMonitor doesn't reliably report refresh rate, we fallback to default 60 or leave empty.
                        });
                    }
                }
                
                // fallback/enrichment using VideoController which might duplicate or give better refresh rates
                if (info.Displays.Count == 0)
                {
                    using var vcSearcher = new ManagementObjectSearcher("select CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate from Win32_VideoController");
                    foreach (var obj in vcSearcher.Get())
                    {
                        int width = Convert.ToInt32(obj["CurrentHorizontalResolution"] ?? 0);
                        int height = Convert.ToInt32(obj["CurrentVerticalResolution"] ?? 0);
                        int refresh = Convert.ToInt32(obj["CurrentRefreshRate"] ?? 0);
                        if (width > 0 && height > 0)
                        {
                            info.Displays.Add(new DisplayData 
                            { 
                                Resolution = $"{width} x {height}", 
                                RefreshRate = refresh 
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private static void GetSoftwareInfo(PerformanceInfo info)
        {
            // OS
            try
            {
                using var searcher = new ManagementObjectSearcher("select Caption, Version, OSArchitecture from Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    info.OsVersion = $"{obj["Caption"]} ({obj["Version"]})";
                    info.OsArchitecture = obj["OSArchitecture"]?.ToString() ?? "未知";
                    break;
                }
            }
            catch
            {
                info.OsVersion = RuntimeInformation.OSDescription;
            }

            // Default Browser
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
                if (key != null)
                {
                    var progId = key.GetValue("ProgId")?.ToString();
                    if (progId != null)
                    {
                        if (progId.Contains("Chrome")) info.BrowserInfo = "Google Chrome";
                        else if (progId.Contains("Firefox")) info.BrowserInfo = "Mozilla Firefox";
                        else if (progId.Contains("MSEdge")) info.BrowserInfo = "Microsoft Edge";
                        else info.BrowserInfo = progId;
                    }
                }
            }
            catch { }

            // Processes and Services
            try
            {
                var processes = Process.GetProcesses();
                info.TotalProcessesCount = processes.Length;
                // Roughly estimate background processes
                int session0Processes = processes.Count(p => p.SessionId == 0);
                info.BackgroundProcessRatio = info.TotalProcessesCount > 0 ? (double)session0Processes / info.TotalProcessesCount : 0;

                using var searcher = new ManagementObjectSearcher("select State from Win32_Service where State='Running'");
                info.RunningServicesCount = searcher.Get().Count;
            }
            catch { }

            // App Usage
            try
            {
                using var currentProcess = Process.GetCurrentProcess();
                info.AppMemoryMb = currentProcess.WorkingSet64 / 1024.0 / 1024.0;
                info.AppCpuUsage = 0; 
            }
            catch { }
        }

        private static void CalculateScore(PerformanceInfo info)
        {
            double score = 0;

            // CPU (Max 30)
            int totalLogicalProcessors = info.Cpus.Sum(c => c.LogicalProcessors);
            double avgFreq = info.Cpus.Count > 0 ? info.Cpus.Average(c => c.Frequency.Contains("GHz") ? double.Parse(c.Frequency.Replace("GHz", "").Trim()) : 0) : 0;
            score += Math.Min(30, totalLogicalProcessors * 2 + avgFreq * 2);

            // RAM (Max 25)
            score += Math.Min(25, info.RamTotalGb * 1.5);

            // GPU (Max 15)
            double maxVram = info.Gpus.Count > 0 ? info.Gpus.Max(g => g.VramGb) : 0;
            score += Math.Min(15, maxVram * 2 + 5); // baseline 5

            // Storage (Max 15)
            bool hasSsd = info.Drives.Any(d => d.MediaType == "SSD" || d.MediaType == "NVMe/SCM");
            score += hasSsd ? 10 : 2;
            double totalAvailable = info.LogicalDrives.Sum(d => d.AvailableGb);
            score += Math.Min(5, totalAvailable > 50 ? 5 : totalAvailable / 10);

            // Software/OS state (Max 15)
            double swScore = 15;
            swScore -= info.BackgroundProcessRatio * 10; // penalty for too many background processes
            if (info.RamAvailableGb < 2) swScore -= 5;
            score += Math.Max(0, swScore);

            info.Score = (int)Math.Clamp(Math.Round(score), 0, 100);

            if (info.Score <= 20)
            {
                info.Rating = "性能极差";
                info.RatingDescription = "无法流畅运行基础功能";
            }
            else if (info.Score <= 40)
            {
                info.Rating = "性能较差";
                info.RatingDescription = "仅支持基础功能运行";
            }
            else if (info.Score <= 60)
            {
                info.Rating = "性能中等";
                info.RatingDescription = "可支撑常规功能使用";
            }
            else if (info.Score <= 80)
            {
                info.Rating = "性能良好";
                info.RatingDescription = "可流畅运行绝大多数功能";
            }
            else
            {
                info.Rating = "性能优异";
                info.RatingDescription = "可承载高负载场景运行";
            }
        }
    }
}