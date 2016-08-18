namespace RethinkDb.Driver.Net
{
    internal interface IConnectionBuilder<TBuilder>
    {
        /// <summary>
        /// The default DB for queries.
        /// </summary>
        TBuilder Db(string val);

        /// <summary>
        /// The authorization key to the server.
        /// </summary>
        TBuilder AuthKey(string key);

        /// <summary>
        /// The user account and password to connect as (default "admin", "").
        /// </summary>
        TBuilder User(string user, string password);
    }
}