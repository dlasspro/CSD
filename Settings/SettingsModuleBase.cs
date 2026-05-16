using Microsoft.UI.Xaml;
using System;

namespace CSD.Settings
{
    public abstract class SettingsModuleBase : ISettingsModule
    {
        public abstract string CategoryKey { get; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public virtual string Glyph => "";
        public virtual string ImageIconUri => "";

        protected bool IsAutoSaveSuspended { get; set; } = true;
        
        public event Action? SettingsChanged;

        private FrameworkElement? _view;

        protected SettingsContext Context { get; private set; } = null!;

        public virtual void Initialize(SettingsContext context)
        {
            Context = context;
        }

        public FrameworkElement CreateView()
        {
            if (_view == null)
            {
                _view = BuildContent();
                LoadSettings();
                HookAutoSaveHandlers();
                IsAutoSaveSuspended = false;
            }
            return _view;
        }

        protected abstract FrameworkElement BuildContent();

        protected virtual void LoadSettings() { }

        protected virtual void HookAutoSaveHandlers() { }

        public virtual void OnNavigatedTo() { }

        public virtual void PersistSettings() { }

        protected void NotifySettingsChanged()
        {
            if (!IsAutoSaveSuspended)
            {
                PersistSettings();
                SettingsChanged?.Invoke();
            }
        }
    }
}