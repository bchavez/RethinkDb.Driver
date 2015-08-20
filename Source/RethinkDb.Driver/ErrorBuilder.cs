using System;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace com.rethinkdb
{

	public class ErrorBuilder
	{
		internal readonly string msg;
		internal readonly ResponseType? responseType;
	    internal Backtrace backtrace = null;
	    internal ErrorType? errorType = null;
	    internal ReqlAst term = null;

		public ErrorBuilder(string msg, ResponseType responseType)
		{
			this.msg = msg;
			this.responseType = responseType;
		}

		public virtual ErrorBuilder setBacktrace(Backtrace backtrace)
		{
			this.backtrace = backtrace;
			return this;
		}

		public virtual ErrorBuilder setErrorType(ErrorType errorType)
		{
			this.errorType = errorType;
			return this;
		}

		public virtual ErrorBuilder setTerm(Query query)
		{
			this.term = query.term;
			return this;
		}

		public virtual ReqlError build()
		{
			ReqlError con;
			switch (responseType)
			{
				case ResponseType.CLIENT_ERROR:
					con = new ReqlClientError(msg);
					break;
				case ResponseType.COMPILE_ERROR:
					con = new ReqlCompileError(msg);
					break;
				case ResponseType.RUNTIME_ERROR:
			    {
			        switch( errorType )
			        {
			            case ErrorType.INTERNAL:
			                con = new ReqlInternalError(msg);
			                break;
			            case ErrorType.RESOURCE:
			                con = new ReqlResourceLimitError(msg);
			                break;
			            case ErrorType.LOGIC:
			                con =  new ReqlQueryLogicError(msg);
			                break;
			            case ErrorType.NON_EXISTENCE:
			                con = new ReqlNonExistenceError(msg);
			                break;
			            case ErrorType.OP_FAILED:
			                con = new ReqlOpFailedError(msg);
			                break;
			            case ErrorType.OP_INDETERMINATE:
			                con = new ReqlOpIndeterminateError(msg);
			                break;
			            case ErrorType.USER:
			                con =  new ReqlUserError(msg);
			                break;
			            default:
			                con = new ReqlRuntimeError(msg);
			                break;
			        }
			        break;
			    }
				default:
			        con = new ReqlError(msg);
				break;
			}

		    con.Backtrace = this.backtrace;
		    con.Term = this.term;

		    return con;
		}
	}

}