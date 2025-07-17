using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public class ReturnBehavior
    {
        private readonly List<StatementSyntax> selectedStatements;

        public ReturnBehavior(List<StatementSyntax> statements)
        {
            selectedStatements = statements;
        }

        public bool HasReturnStatements => selectedStatements
            .SelectMany(stmt => stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>())
            .Any();

        public bool AllPathsReturnOrThrow => selectedStatements is [SwitchStatementSyntax switchStatement]
                                             && switchStatement.Sections.All(sec =>
                                                 sec.Statements.LastOrDefault() is ReturnStatementSyntax
                                                     or ThrowStatementSyntax);

        public bool RequiresReturnStatement => HasReturnStatements || AllPathsReturnOrThrow;
}
