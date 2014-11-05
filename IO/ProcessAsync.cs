using System.Diagnostics;
using System.Threading.Tasks;

namespace Common.IO
{
    public class ProcessAsync
    {
        public static Task RunProcessAsync(string fileName, string arguments)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = arguments
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                taskCompletionSource.SetResult(true);
                process.Dispose();
            };

            process.Start();

            return taskCompletionSource.Task;
        }
    }
}
