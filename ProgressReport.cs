using System;
using System.Collections.Generic;
using System.Text;

namespace MediaRipper
{
    /// <summary>
    /// A report of the current progress of a fetch or rip action.
    /// </summary>
    public struct ProgressReport
    {
        /// <summary>The percentage of completion.</summary>
        public float percent;

        /// <summary>The item that was queried.</summary>
        public FetchedItem finishedItem;

        /// <summary>Whether the query was successful.</summary>
        public bool success;

        /// <summary>The exception message in case the query was not successful.</summary>
        public string error;

        /// <summary>The time at which the query was completed.</summary>
        public DateTime time;

        /// <summary>The percentage of completion.</summary>
        public int count;

        /// <summary>
        /// Initializes a progress report.
        /// </summary>
        /// <param name="percent">The percentage of completion</param>
        /// <param name="finishedItem">The item that was queried</param>
        /// <param name="success">Whether the query was successful</param>
        /// <param name="error">The exception message in case the query was not successful</param>
        /// <param name="time">The time at which the query was completed</param>
        /// <param name="count">The percentage of completion</param>
        public ProgressReport(float percent, FetchedItem finishedItem, bool success, string error, DateTime time, int count)
        {
            this.percent = percent;
            this.finishedItem = finishedItem;
            this.success = success;
            this.error = error;
            this.time = time;
            this.count = count;
        }
    }
}
