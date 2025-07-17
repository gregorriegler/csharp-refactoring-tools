using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace RoslynRefactoring;

/// <summary>
/// Extract selected code into a new method
/// </summary>
public class ExtractMethod(CodeSelection selection, string newMethodName) : IRefactoring
{
    public static ExtractMethod Create(string[] args)
    {
        var selection = CodeSelection.Parse(args[0]);
        var newMethodName = args[1];
        return new ExtractMethod(selection, newMethodName);
    }

    public async Task<Document> PerformAsync(Document document)
    {
        var span = await GetSpan(document, selection);

        var root = await document.GetSyntaxRootAsync();
        if (root == null)
            throw new InvalidOperationException("SyntaxRoot is null.");

        var selectedNode = root.FindNode(span);

        var block = selectedNode.AncestorsAndSelf().OfType<BlockSyntax>().FirstOrDefault();
        if (block == null)
            throw new InvalidOperationException("Selected statements are not inside a block.");

        var extractionTarget = ExtractionTarget.CreateFromSelection(selectedNode, span, block);

        var model = await document.GetSemanticModelAsync();
        if (model == null)
            throw new InvalidOperationException("SemanticModel is null.");

        var dataFlow = extractionTarget.AnalyzeDataFlow(model);

        var parameters = dataFlow.ReadInside.Except(dataFlow.WrittenInside)
            .OfType<ILocalSymbol>()
            .Select(s => SyntaxFactory.Parameter(SyntaxFactory.Identifier(s.Name))
                .WithType(SyntaxFactory.ParseTypeName(s.Type.ToDisplayString()))).ToList();


        var methodCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(newMethodName),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(parameters.Select(p =>
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier.Text))))));


        var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

        var returns = dataFlow.DataFlowsOut.Intersect(dataFlow.WrittenInside, SymbolEqualityComparer.Default)
            .OfType<ILocalSymbol>()
            .ToList();
        var replacementNode = extractionTarget.CreateReplacementNode(methodCall, model, returns);
        extractionTarget.ReplaceInEditor(editor, replacementNode);
        var returnType = extractionTarget.DetermineReturnType(model, dataFlow);
        var methodBody = extractionTarget.CreateMethodBody(returns);

        var insertionPoint = extractionTarget.GetInsertionPoint();
        var methodDeclaration = new MethodDeclaration(newMethodName, parameters, methodBody, returnType).Create();
        editor.InsertAfter(insertionPoint, methodDeclaration);

        var newRoot = editor.GetChangedRoot().NormalizeWhitespace();
        Console.WriteLine($"✅ Extracted method '{newMethodName}'");
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<TextSpan> GetSpan(Document document, CodeSelection selection)
    {
        var lines = (await document.GetTextAsync()).Lines;
        var span = TextSpan.FromBounds(
            GetPos(selection.Start),
            GetPos(selection.End)
        );
        return span;

        int GetPos(Cursor cursor) => lines[cursor.Line - 1].Start + cursor.Column - 1;
    }
}
