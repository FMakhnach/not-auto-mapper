using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.NotAutoMapper.Components.EnumMapper;
using ReSharperPlugin.NotAutoMapper.Mapping;

namespace ReSharperPlugin.NotAutoMapper.Components
{
    [ElementProblemAnalyzer(
        typeof(IMethodDeclaration),
        HighlightingTypes = new[] {typeof(CanGenerateEnumMapperHighlighting)})]
    public class CanGenerateMapperProblemAnalyzer : ElementProblemAnalyzer<IMethodDeclaration>
    {
        protected override void Run(
            IMethodDeclaration element,
            ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (CanGenerateMapperMethod(element))
            {
                if (CanGenerateEnumMapperMethod(element))
                {
                    consumer.AddHighlighting(new CanGenerateEnumMapperHighlighting(element));
                }
            }
        }

        private static bool CanGenerateMapperMethod(IMethodDeclaration methodDeclaration)
        {
            var body = methodDeclaration.GetCodeBody().BlockBody;
            return (body is null || body.Statements.IsEmpty)
                   && methodDeclaration.GetCodeBody().ExpressionBody is null
                   && methodDeclaration.IsExtensionMethod
                   && methodDeclaration.DeclaredElement != null
                   && !methodDeclaration.DeclaredElement.ReturnType.IsVoid();
        }

        private static bool CanGenerateEnumMapperMethod(IMethodDeclaration methodDeclaration)
        {
            if (methodDeclaration.ParameterDeclarations.Count != 1 || methodDeclaration.DeclaredElement is null)
            {
                return false;
            }

            var parameterType = methodDeclaration.ParameterDeclarations[0].Type;
            var returnType = methodDeclaration.DeclaredElement.ReturnType;
            return parameterType.IsEnumType() && returnType.IsEnumType();
        }
    }
}