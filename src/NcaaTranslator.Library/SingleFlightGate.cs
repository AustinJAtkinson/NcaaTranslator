namespace NcaaTranslator.Library
{
    public sealed class SingleFlightGate : IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public async Task<bool> RunAsync(Func<Task> action)
        {
            if (!await _gate.WaitAsync(0).ConfigureAwait(false))
                return false;

            try
            {
                await action().ConfigureAwait(false);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose() => _gate.Dispose();
    }
}
