using System.Threading;

namespace SiS.Communication
{
    /// <summary>
    /// Provides simple static methods for starting a thread
    /// </summary>
    public static class ThreadEx
    {
        /// <summary>
        /// Start a thread using ParameterizedThreadStart delegate and user defined state
        /// </summary>
        /// <param name="start"> A delegate that represents the methods to be invoked when this thread begins executing.</param>
        /// <param name="parameter">An object that contains data to be used by the method the thread executes.</param>
        /// <returns></returns>
        public static Thread Start(ParameterizedThreadStart start, object parameter)
        {
            Thread thread = new Thread(start);
            thread.IsBackground = true;
            thread.Start(parameter);
            return thread;
        }

        /// <summary>
        /// Start a thread using ThreadStart delegate
        /// </summary>
        /// <param name="start"> A delegate that represents the methods to be invoked when this thread begins executing.</param>
        /// <returns></returns>
        public static Thread Start(ThreadStart start)
        {
            Thread thread = new Thread(start);
            thread.IsBackground = true;
            thread.Start();
            return thread;
        }
    }
}
