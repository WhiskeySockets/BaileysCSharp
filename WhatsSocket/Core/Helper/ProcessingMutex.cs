namespace WhatsSocket.Core.Helper
{
    public class ProcessingMutex
    {
        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        public ProcessingMutex()
        {

        }

        public async Task Mutex(Action action)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                action();
            }
            catch (Exception ex)
            {
            }
            semaphoreSlim.Release();
        }
    }
}
