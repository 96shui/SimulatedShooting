using System;

namespace VRShooting.P3.TestSupport
{
    /// <summary>Explicit scripted snapshots; never computes gameplay outcomes.</summary>
    public sealed class FixtureFeed<T> : IDisposable
    {
        readonly Func<T, string> sessionId;
        readonly Func<T, long> revision;
        bool disposed;
        public FixtureFeed(Func<T, string> getSession, Func<T, long> getRevision)
        {
            sessionId = getSession ?? throw new ArgumentNullException(nameof(getSession));
            revision = getRevision ?? throw new ArgumentNullException(nameof(getRevision));
        }
        public T Current { get; private set; }
        public bool IsBound { get; private set; }
        public event Action<T> Published;
        public int SubscriberCount => Published?.GetInvocationList().Length ?? 0;
        public string SessionId => IsBound ? sessionId(Current) : string.Empty;
        public void Bind(T snapshot)
        {
            if (disposed) throw new ObjectDisposedException(nameof(FixtureFeed<T>));
            if (string.IsNullOrWhiteSpace(sessionId(snapshot)) || revision(snapshot) < 0) throw new ArgumentException("Frame needs a session and nonnegative revision.");
            Current = snapshot;
            IsBound = true;
        }
        public bool Publish(T snapshot)
        {
            if (disposed || !IsBound || sessionId(snapshot) != SessionId || revision(snapshot) <= revision(Current)) return false;
            Current = snapshot;
            Published?.Invoke(snapshot);
            return true;
        }
        public void Clear() { Current = default; IsBound = false; }
        public void Dispose() { disposed = true; Clear(); Published = null; }
    }
}
