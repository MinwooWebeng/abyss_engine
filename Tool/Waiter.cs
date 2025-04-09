namespace AbyssCLI.Tool
{
    public class Waiter<T>
    {
        //every method is safe to call multiple times
        public void Finalize(T value)
        {
            if (Interlocked.CompareExchange(ref state, 1, 0) == 0)
            {
                result = value;
                semaphore.Release();
                return;
            }
        }
        public T GetValue()
        {
            if (state == 0)
            {
                semaphore.WaitOne();
                semaphore.Release();
                return result;
            }
            return result;
        }

        private T result;
        private readonly Semaphore semaphore = new(0, 1);
        private int state = 0; //0: loading, 1: loaded (no need to check sema)
    }
}
