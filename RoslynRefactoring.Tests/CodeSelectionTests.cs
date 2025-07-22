using Microsoft.CodeAnalysis.Text;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class CodeSelectionTests
{
    [Test]
    public void IsInRange_WithSelectionBeyondFileBoundaries_ShouldReturnFalse()
    {
        var selection = CodeSelection.Parse("100:1-100:10");
        var shortFileText = SourceText.From("line 1\nline 2\nline 3");
        var lines = shortFileText.Lines;

        var result = selection.IsInRange(lines);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsInRange_WithValidSelection_ShouldReturnTrue()
    {
        var selection = CodeSelection.Parse("2:1-3:5");
        var fileText = SourceText.From("line 1\nline 2\nline 3\nline 4");
        var lines = fileText.Lines;

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
        var invalidCursor = new Cursor(0, 1);
        var validCursor = new Cursor(1, 1);

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
}
