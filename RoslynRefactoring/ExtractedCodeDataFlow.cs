using Microsoft.CodeAnalysis;

namespace RoslynRefactoring;

public sealed record ExtractedCodeDataFlow(
    IEnumerable<ISymbol> DataFlowsOut,
    IEnumerable<ISymbol> WrittenInside,
    IEnumerable<ISymbol> ReadInside
)
{
    public ExtractedCodeDataFlow(DataFlowAnalysis dataFlow) : this(
        dataFlow.DataFlowsOut,
        dataFlow.WrittenInside,
        dataFlow.ReadInside
    )
    {
    }

    public List<ILocalSymbol> GetReturns()
    {
        return DataFlowsOut.Intersect(WrittenInside, SymbolEqualityComparer.Default)
            .OfType<ILocalSymbol>()
            .ToList();
    }
}
