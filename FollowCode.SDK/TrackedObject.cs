using System;

namespace FollowCode.SDK
{
    public class TrackedObject
    {
        internal WeakReference? DataRef { get; set; }

        public string Key { get; set; } = string.Empty;

        public object? Data
        {
            get => DataRef?.Target;
            set => DataRef = value != null ? new WeakReference(value) : null;
        }

        public DateTime UpdatedAt { get; set; }

        public bool IsAlive => DataRef?.IsAlive ?? false;
    }
}
