using System;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Represents options for a GridFS Find operation.
    /// </summary>
    public class FindOptions
    {
        // fields
        private int? batchSize;
        private int? limit;
        private TimeSpan? maxTime;
        private bool? noCursorTimeout;
        private int? skip;
        private ReqlExpr sort;

        // properties
        /// <summary>
        /// Gets or sets the batch size.
        /// </summary>
        /// <value>
        /// The batch size.
        /// </value>
        public int? BatchSize
        {
            get { return batchSize; }
            set { batchSize = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the maximum number of documents to return.
        /// </summary>
        /// <value>
        /// The maximum number of documents to return.
        /// </value>
        public int? Limit
        {
            get { return limit; }
            set { limit = value; }
        }

        /// <summary>
        /// Gets or sets the maximum time the server should spend on the Find.
        /// </summary>
        /// <value>
        /// The maximum time the server should spend on the Find.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return maxTime; }
            set { maxTime = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets whether the cursor should not timeout.
        /// </summary>
        /// <value>
        /// Whether the cursor should not timeout.
        /// </value>
        public bool? NoCursorTimeout
        {
            get { return noCursorTimeout; }
            set { noCursorTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents to skip.
        /// </summary>
        /// <value>
        /// The number of documents to skip.
        /// </value>
        public int? Skip
        {
            get { return skip; }
            set { skip = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>
        /// The sort order.
        /// </value>
        public ReqlExpr Sort
        {
            get { return sort; }
            set { sort = value; }
        }
    }
}
