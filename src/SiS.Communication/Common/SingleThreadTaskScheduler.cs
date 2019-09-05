using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiS.Communication
{
    /// <summary>
    /// Specifies the buffer type of the queue within SiS.Communication.SingleThreadTaskScheduler object
    /// </summary>
    public enum BufferType
    {
        Queue,
        Overlapped
    }

    /// <summary>
    /// Represents an object that handles work in queue with one thread
    /// </summary>
    public class SingleThreadTaskScheduler : TaskScheduler
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.SingleThreadTaskScheduler class using 
        /// the specific buffer type
        /// </summary>
        /// <param name="bufferType">The buffer type of the task queue</param>
        public SingleThreadTaskScheduler(BufferType bufferType)
        {
            _bufferType = bufferType;
        }

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.SingleThreadTaskScheduler class
        /// </summary>
        public SingleThreadTaskScheduler() : this(BufferType.Queue) { }
        #endregion

        #region Private Members

        private BlockingCollection<Task> _queue = new BlockingCollection<Task>();
        private CancellationTokenSource _cancellSource;
        private Thread _workThread;
        private bool _isRunning = false;
        private BufferType _bufferType;
        #endregion

        #region Implement TaskScheduler Functions
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _queue;
        }

        protected override void QueueTask(Task task)
        {
            if (!_isRunning && _workThread != null)
            {
                throw new Exception("the scheduler is disposed");
            }
            if (!_isRunning)
            {
                Run();
            }
            if (_bufferType == BufferType.Overlapped)
            {
                while (_queue.Count > 0)
                {
                    _queue.Take();
                }
                _queue.Add(task);
            }
            else
            {
                _queue.Add(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        #endregion

        #region Private Functions
        private void Run()
        {
            if (_isRunning)
            {
                return;
            }
            if (_workThread != null)
            {
                throw new Exception("can not run a disposed scheduler");
            }
            _cancellSource = new CancellationTokenSource();
            _isRunning = true;
            _workThread = ThreadEx.Start(() =>
            {
                while (_isRunning)
                {
                    Task task = null;
                    try
                    {
                        if (!_queue.TryTake(out task, Timeout.Infinite, _cancellSource.Token))
                        {
                            continue;
                        }
                    }
                    catch { return; }
                    TryExecuteTask(task);
                }
            });
        }
        #endregion

        #region Public Functions

        /// <summary>
        /// Stop the working thread within the scheduler
        /// </summary>
        public void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _cancellSource.Cancel();
            }
        }

        #endregion
    }
}
