using System;
using System.Diagnostics;

namespace Styx.Common
{
    /// <summary>
    /// A class that keeps record of an entry and a time at an interval for the last X milliseconds.
    /// </summary>
    public class TimedRecordKeeper<T>
    {
        // Note that we are using ints instead of TimeSpan's here to be able to divide properly.
        public TimedRecordKeeper(int storeIntervalMilliseconds, int storeTimeMilliseconds)
        {
            StoreIntervalMilliseconds = storeIntervalMilliseconds;
            StoreTimeMilliseconds = storeTimeMilliseconds;

            if (NumRecords < 0)
                throw new ArgumentException("Invalid arguments passed!");

            _records = new Record[NumRecords];
            for (int i = 0; i < _records.Length; i++)
                _records[i] = new Record { Timer = new Stopwatch() };

            _timer.Start();
        }

        /// <summary>
        /// Gets the interval, in milliseconds, that the record keeper stores records.
        /// </summary>
        public int StoreIntervalMilliseconds { get; private set; }

        /// <summary>
        /// Gets the amount of time the record keeper will keep records for.
        /// </summary>
        public int StoreTimeMilliseconds { get; private set; }

        /// <summary>
        /// Gets the amount of records this keeper keeps, based on the store interval and store time.
        /// </summary>
        public int NumRecords { get { return StoreTimeMilliseconds / StoreIntervalMilliseconds; } }

        private readonly Stopwatch _timer = new Stopwatch();

        private class Record
        {
            public Stopwatch Timer { get; set; }
            public T Value { get; set; }
        }

        private readonly Record[] _records;

        /// <summary>
        /// Resets this record keeper to contain zero active records.
        /// </summary>
        public void Reset()
        {
            foreach (Record record in _records)
            {
                record.Timer.Reset();
            }
        }

        /// <summary>
        /// Adds a record into the time keeper.
        /// </summary>
        /// <param name="value"></param>
        public void AddRecord(T value)
        {
            long index = _timer.ElapsedMilliseconds / StoreIntervalMilliseconds;
            index %= NumRecords;
            _records[index].Timer.Restart();
            _records[index].Value = value;
        }

        /// <summary>
        /// Find the record that has been stored for an amount of time closest to the passed time.
        /// </summary>
        /// <param name="timeAgo"></param>
        /// <param name="value"></param>
        /// <param name="elapsed"></param>
        /// <returns>A bool indicating whether any record was found.</returns>
        public bool GetRecord(TimeSpan timeAgo, out T value, out TimeSpan elapsed)
        {
            Record bestRecord = null;
            TimeSpan bestElapsed = TimeSpan.Zero;
            TimeSpan bestDifference = TimeSpan.MaxValue;
            foreach (Record record in _records)
            {
                if (!record.Timer.IsRunning || record.Timer.ElapsedMilliseconds > StoreTimeMilliseconds)
                    continue;

                TimeSpan recordElapsed = record.Timer.Elapsed;
                TimeSpan difference = recordElapsed - timeAgo;
                if (difference < TimeSpan.Zero)
                    difference = -difference;

                if (difference < bestDifference)
                {
                    bestDifference = difference;
                    bestRecord = record;
                    bestElapsed = recordElapsed;
                }
            }

            if (bestRecord == null)
            {
                value = default(T);
                elapsed = TimeSpan.Zero;
                return false;
            }

            value = bestRecord.Value;
            elapsed = bestElapsed;
            return true;
        }
    }
}
