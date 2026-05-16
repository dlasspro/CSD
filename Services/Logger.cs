using System;


using CSD.Views;
using CSD.Models;
using CSD.Services;
using CSD.Helpers;
using CSD.Settings;




namespace CSD.Services
{
    /// <summary>
    /// 全局日志记录辅助类，用于在 DebugWindow 中记录信息
    /// </summary>
    public static class Logger
    {
        private static DebugWindow? _debugWindow;

        /// <summary>
        /// 设置 DebugWindow 实例
        /// </summary>
        public static void SetDebugWindow(DebugWindow debugWindow)
        {
            _debugWindow = debugWindow;
        }

        /// <summary>
        /// 记录日志到 DebugWindow
        /// </summary>
        public static void Log(string method, string path, int statusCode, string responseBody, string? errorMessage = null)
        {
            if (_debugWindow == null)
                return;

            _debugWindow.AppendLog(method, path, statusCode, responseBody, errorMessage);
        }

        /// <summary>
        /// 记录更新相关的日志
        /// </summary>
        public static void LogUpdate(string message, string? errorMessage = null)
        {
            Log("UPDATE", "/app/update", errorMessage != null ? 0 : 200, message, errorMessage);
        }
    }
}
