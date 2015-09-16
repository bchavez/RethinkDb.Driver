using System;
using System.Text;
using IronPython.Compiler.Ast;
using Z.ExtensionMethods;

namespace Templates
{
	public class SharpWalker : PythonWalker
	{
		public StringBuilder gen = new StringBuilder();
		public override void PostWalk(AndExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(BackQuoteExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(BinaryExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(CallExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
			gen.Append(")");
		}

		public override void PostWalk(ConditionalExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ConstantExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(DictionaryComprehension node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(DictionaryExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ErrorExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(GeneratorExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(IndexExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(LambdaExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ListComprehension node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ListExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
			gen.Append("new[]{}");
		}

		public override void PostWalk(MemberExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
			gen.Append("." + node.Name+"(");
		}

		public override void PostWalk(NameExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
			gen.Append(node.Name);
		}

		public override void PostWalk(OrExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ParenthesisExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(SetComprehension node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(SetExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(SliceExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(TupleExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(UnaryExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(YieldExpression node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(AssertStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(AssignmentStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(AugmentedAssignStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(BreakStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ClassDefinition node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ContinueStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(DelStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(EmptyStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ExecStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ExpressionStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ForStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(FromImportStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(FunctionDefinition node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(GlobalStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(IfStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ImportStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(PrintStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(PythonAst node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(RaiseStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ReturnStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(SuiteStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(TryStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(WhileStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(WithStatement node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(Arg node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
			gen.Append("(");
		}

		public override void PostWalk(ComprehensionFor node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ComprehensionIf node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(DottedName node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(IfStatementTest node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(ModuleName node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(Parameter node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(RelativeModuleName node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(SublistParameter node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}

		public override void PostWalk(TryStatementHandler node)
		{
			Console.WriteLine(node);
			base.PostWalk(node);
		}
	}
}