using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace RoslynRefactoring
{
    /// <summary>
    /// Abstract base class for different types of extraction targets (statements, expressions, etc.)
    /// </summary>
    public abstract class ExtractionTarget
    {
        /// <summary>
        /// Gets the selected syntax node to be extracted
        /// </summary>
        /// <returns>The syntax node representing the code to extract</returns>
        public abstract SyntaxNode GetSelectedNode();

        /// <summary>
        /// Analyzes data flow for the selected code to determine parameters and return values
        /// </summary>
        /// <param name="model">The semantic model for analysis</param>
        /// <returns>Data flow analysis results</returns>
        public abstract DataFlowAnalysis AnalyzeDataFlow(SemanticModel model);

        /// <summary>
        /// Determines the return type for the extracted method based on data flow analysis
        /// </summary>
        /// <param name="model">The semantic model for type resolution</param>
        /// <param name="dataFlow">The data flow analysis results</param>
        /// <returns>The return type syntax for the extracted method</returns>
        public abstract TypeSyntax DetermineReturnType(SemanticModel model, DataFlowAnalysis dataFlow);

        /// <summary>
        /// Creates the method body for the extracted method
        /// </summary>
        /// <param name="dataFlow">The data flow analysis results</param>
        /// <returns>The block syntax representing the method body</returns>
        public abstract BlockSyntax CreateMethodBody(DataFlowAnalysis dataFlow);

        /// <summary>
        /// Replaces the original code in the editor with a method call
        /// </summary>
        /// <param name="editor">The syntax editor for making changes</param>
        /// <param name="methodCall">The method call expression to replace the original code</param>
        public abstract void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall);
    }
}
