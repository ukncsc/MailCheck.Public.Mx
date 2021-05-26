using System.Threading.Tasks;

namespace MailCheck.Mx.TlsTester.Test
{
    public static class TaskHelpers
    {
        public static Task<T> NeverReturn<T>()
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            return tcs.Task;
        }
    }
}
