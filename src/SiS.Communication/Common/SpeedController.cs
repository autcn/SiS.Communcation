using System;
using System.Threading;

namespace SiS.Communication
{
    /// <summary>
    /// Provides methods for speed control
    /// </summary>
    public class SpeedController
    {
        #region Private Members
        private int? _lastTick;
        private int _dataCount = 0;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating the limit speed. If the value less or equal to 0, the speed control is not working.The default is 0.
        /// </summary>
        public double LimitSpeed { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether to enable speed control.The default is false.
        /// </summary>
        public bool Enabled { get; set; } = false;
        #endregion

        #region Public Functions

        /// <summary>
        /// Reset the speed controller.
        /// </summary>
        public void Reset()
        {
            _lastTick = null;
            _dataCount = 0;
            LimitSpeed = 0;
            Enabled = false;
        }

        /// <summary>
        /// Try to limit the speed. 
        /// </summary>
        /// <param name="newDataSize">The new data size for speed limit.</param>
        /// <returns>True if the speed is limited; otherwise false.</returns>
        public bool TryLimit(int newDataSize)
        {
            if (!Enabled || LimitSpeed <= 0)
            {
                return false;
            }
            if (_lastTick == null)
            {
                _lastTick = Environment.TickCount;
                _dataCount = 0;
                return false;
            }
            bool isLimited = false;
            _dataCount += newDataSize;
            int curTick = Environment.TickCount;
            int duration = curTick - _lastTick.Value;

            if (duration > 0 && _dataCount > 100 * 1024)
            {
                double allowMaxDataSize = LimitSpeed * (double)duration / 1000.0;
                if (_dataCount > allowMaxDataSize)
                {
                    Thread.Sleep(20);
                    isLimited = true;
                }
            }

            if (duration > 3000)
            {
                _lastTick = Environment.TickCount;
                _dataCount = 0;
            }
            return isLimited;
        }
        #endregion

    }
}
