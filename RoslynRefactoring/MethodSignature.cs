using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public record MethodSignature
    {
        public BlockSyntax MethodBody { get; }
        public TypeSyntax ReturnType { get; }

        private MethodSignature(BlockSyntax methodBody, TypeSyntax returnType)
        {
            MethodBody = methodBody;
            ReturnType = returnType;
        }

        public static MethodSignature Create(BlockSyntax methodBody, TypeSyntax returnType)
        {
            return new MethodSignature(methodBody, returnType);
        }
}
