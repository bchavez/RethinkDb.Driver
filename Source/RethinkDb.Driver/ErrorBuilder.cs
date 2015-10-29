using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver
{
	public class ErrorBuilder
	{
	    internal string Msg { get; }
	    internal ResponseType? ResponseType { get; }
	    internal Backtrace Backtrace { get; set; }
	    internal ErrorType? ErrorType { get; set; }
	    internal ReqlAst Term { get; set; }

	    public ErrorBuilder(string msg, ResponseType responseType)
		{
			this.Msg = msg;
			this.ResponseType = responseType;
		}

		public virtual ErrorBuilder SetBacktrace(Backtrace backtrace)
		{
			this.Backtrace = backtrace;
			return this;
		}

		public virtual ErrorBuilder SetErrorType(ErrorType errorType)
		{
			this.ErrorType = errorType;
			return this;
		}

		public virtual ErrorBuilder SetTerm(Query query)
		{
			this.Term = query.Term;
			return this;
		}

		public virtual ReqlError Build()
		{
			ReqlError con;
			switch (ResponseType)
			{
				case Proto.ResponseType.CLIENT_ERROR:
					con = new ReqlClientError(Msg);
					break;
				case Proto.ResponseType.COMPILE_ERROR:
					con = new ReqlServerCompileError(Msg);
					break;
				case Proto.ResponseType.RUNTIME_ERROR:
			    {
			        switch( ErrorType )
			        {
			            case Proto.ErrorType.INTERNAL:
			                con = new ReqlInternalError(Msg);
			                break;
			            case Proto.ErrorType.RESOURCE_LIMIT:
			                con = new ReqlResourceLimitError(Msg);
			                break;
			            case Proto.ErrorType.QUERY_LOGIC:
			                con =  new ReqlQueryLogicError(Msg);
			                break;
			            case Proto.ErrorType.NON_EXISTENCE:
			                con = new ReqlNonExistenceError(Msg);
			                break;
			            case Proto.ErrorType.OP_FAILED:
			                con = new ReqlOpFailedError(Msg);
			                break;
			            case Proto.ErrorType.OP_INDETERMINATE:
			                con = new ReqlOpIndeterminateError(Msg);
			                break;
			            case Proto.ErrorType.USER:
			                con =  new ReqlUserError(Msg);
			                break;
			            default:
			                con = new ReqlRuntimeError(Msg);
			                break;
			        }
			        break;
			    }
				default:
			        con = new ReqlError(Msg);
				break;
			}

		    con.Backtrace = this.Backtrace;
		    con.Term = this.Term;

		    return con;
		}
	}

}
