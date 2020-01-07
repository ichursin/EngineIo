using System;
using System.Diagnostics.CodeAnalysis;

namespace EngineIo.Thread
{
    public class Disposable : IDisposable
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeCore()
        {
        }

        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
        private void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                DisposeCore();
            }

            _isDisposed = true;
        }

        ~Disposable()
        {
            Dispose(disposing: false);
        }
    }
}
