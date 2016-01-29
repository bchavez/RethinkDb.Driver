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
        private int? _batchSize;
        private int? _limit;
        private TimeSpan? _maxTime;
        private bool? _noCursorTimeout;
        private int? _skip;
        private ReqlExpr _sort;

        // properties
        /// <summary>
        /// Gets or sets the batch size.
        /// </summary>
        /// <value>
        /// The batch size.
        /// </value>
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the maximum number of documents to return.
        /// </summary>
        /// <value>
        /// The maximum number of documents to return.
        /// </value>
        public int? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the maximum time the server should spend on the Find.
        /// </summary>
        /// <value>
        /// The maximum time the server should spend on the Find.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets whether the cursor should not timeout.
        /// </summary>
        /// <value>
        /// Whether the cursor should not timeout.
        /// </value>
        public bool? NoCursorTimeout
        {
            get { return _noCursorTimeout; }
            set { _noCursorTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents to skip.
        /// </summary>
        /// <value>
        /// The number of documents to skip.
        /// </value>
        public int? Skip
        {
            get { return _skip; }
            set { _skip = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>
        /// The sort order.
        /// </value>
        public ReqlExpr Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }
    }
}
