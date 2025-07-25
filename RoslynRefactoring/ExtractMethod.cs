using Microsoft.CodeAnalysis;
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

    private static async Task<TextSpan> GetSpan(Document document, CodeSelection selection)
    {
        var lines = (await document.GetTextAsync()).Lines;

        int GetPos(Cursor cursor) => lines[cursor.Line - 1].Start + cursor.Column - 1;

        var span = TextSpan.FromBounds(
            GetPos(selection.Start),
            GetPos(selection.End)
        );
        return span;
    }

    public async Task<Document> PerformAsync(Document document)
    {
        var span = await GetSpan(document, selection);

        var root = await document.GetSyntaxRootAsync();
        if (root == null) throw new InvalidOperationException("SyntaxRoot is null.");
        var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

        var selectedNode = root.FindNode(span);
        var block = selectedNode.AncestorsAndSelf().OfType<BlockSyntax>().FirstOrDefault();
        if (block == null)
        {
            if (string.IsNullOrWhiteSpace(root.ToString()))
            {
                return document;
            }
            throw new InvalidOperationException("Selected statements are not inside a block.");
        }

        var model = await document.GetSemanticModelAsync();
        if (model == null) throw new InvalidOperationException("SemanticModel is null.");

        try
        {
            var extractionTarget = ExtractionTarget.CreateFromSelection(selectedNode, span, block, model);
            var replacementNode = extractionTarget.CreateReplacementNode(newMethodName);
            extractionTarget.ReplaceInEditor(editor, replacementNode);
            var methodDeclaration = extractionTarget.CreateMethodDeclaration(newMethodName);
            var insertionPoint = extractionTarget.GetInsertionPoint();
            editor.InsertAfter(insertionPoint, methodDeclaration);

            var newRoot = editor.GetChangedRoot().NormalizeWhitespace();
            Console.WriteLine($"✅ Extracted method '{newMethodName}'");
            return document.WithSyntaxRoot(newRoot);
        }
        catch (InvalidOperationException ex) when (ex.Message == "No statements or expressions selected for extraction.")
        {
            return document;
        }
    }
}
