using Microsoft.CodeAnalysis.Text;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class CodeSelectionTests
{
    [Test]
    public void IsInRange_WithSelectionBeyondFileBoundaries_ShouldReturnFalse()
    {
        var selection = CodeSelection.Parse("100:1-100:10");
        var lines = CreateShortFileLines();

        var result = selection.IsInRange(lines);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsInRange_WithValidSelection_ShouldReturnTrue()
    {
        var selection = CodeSelection.Parse("2:1-3:5");
        var lines = CreateMultiLineFileLines();

        var result = selection.IsInRange(lines);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Parse_WithInvalidFormat_ShouldThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => CodeSelection.Parse("1:1:1-2:2"));
    }

    [Test]
    public void Create_WithInvalidLineNumbers_ShouldThrowInvalidOperationException()
    {
        var invalidCursor = CreateInvalidCursor();
        var validCursor = CreateValidCursor();

        Assert.Throws<InvalidOperationException>(() => CodeSelection.Create(invalidCursor, validCursor));
    }

    [Test]
    public void CursorParse_WithInvalidFormat_ShouldThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Cursor.Parse("invalid:format:extra"));
    }

    [Test]
    public void CursorParse_WithNonNumericValues_ShouldThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Cursor.Parse("abc:def"));
    }

    private static TextLineCollection CreateShortFileLines()
    {
        var fileText = SourceText.From("line 1\nline 2\nline 3");
        return fileText.Lines;
    }

    private static TextLineCollection CreateMultiLineFileLines()
    {
        var fileText = SourceText.From("line 1\nline 2\nline 3\nline 4");
        return fileText.Lines;
    }

    private static Cursor CreateInvalidCursor()
    {
        return new Cursor(0, 1);
    }

    private static Cursor CreateValidCursor()
    {
        return new Cursor(1, 1);
    }
}
