using System;

namespace Styx.CommonBot
{
    public class StatusTextChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusTextChangedEventArgs"/> class.
        /// </summary>
        /// <param name="new">The @new.</param>
        /// <param name="old">The old.</param>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        internal StatusTextChangedEventArgs(string @new, string old)
        {
            NewStatus = @new;
            OldStatus = old;
        }

        /// <summary>
        /// Gets or sets the new status.
        /// </summary>
        /// <value>
        /// The new status.
        /// </value>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public string NewStatus { get; set; }

        /// <summary>
        /// Gets or sets the old status.
        /// </summary>
        /// <value>
        /// The old status.
        /// </value>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public string OldStatus { get; set; }
    }
}
