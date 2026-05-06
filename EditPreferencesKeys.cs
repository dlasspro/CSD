namespace CSD
{
    /// <summary>编辑偏好：本地 AppSettings 键名与云端 JSON 字段约定。</summary>
    internal static class EditPreferencesKeys
    {
        public const string AutoSave = "Settings_EditAutoSave";
        /// <summary>开启且当前日期不是今天时，禁止任何作业写入（含手动保存）；本地键名沿用历史值。</summary>
        public const string BlockNonTodayAutoSave = "Settings_EditBlockNonTodayAutoSave";
        public const string ConfirmNonTodaySave = "Settings_EditConfirmNonTodaySave";
        public const string RefreshBeforeEdit = "Settings_EditRefreshBeforeEdit";
        public const string AutoSavePromptText = "Settings_EditAutoSavePromptText";
        public const string ManualSavePromptText = "Settings_EditManualSavePromptText";
    }
}
