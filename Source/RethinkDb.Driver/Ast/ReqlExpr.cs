namespace RethinkDb.Driver.Ast
{
    //Partial class for overloads expression overloads! :) 
    //Cool man. Yeeah. That's right. So 1337.
    public partial class ReqlExpr
    {
        /// <summary>
        /// Get a single field from an object. If called on a sequence, gets that field from every object in the sequence, skipping objects that lack it.
        /// </summary>
        /// <param name="getField"></param>
        public GetField this[string getField] => this.getField(getField);
    }
}