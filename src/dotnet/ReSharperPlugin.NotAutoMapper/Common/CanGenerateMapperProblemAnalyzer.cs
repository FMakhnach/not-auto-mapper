using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.NotAutoMapper.EnumMapper;
using ReSharperPlugin.NotAutoMapper.ObjectMapper;

namespace ReSharperPlugin.NotAutoMapper.Common
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

                if (CanGenerateObjectMapperMethod(element))
                {
                    consumer.AddHighlighting(new CanGenerateObjectMapperHighlighting(element));
                }
            }
        }

        private static bool CanGenerateMapperMethod(IMethodDeclaration methodDeclaration)
        {
            var body = methodDeclaration.GetCodeBody().BlockBody;
            return (body is null || body.Statements.IsEmpty)
                   && methodDeclaration.GetCodeBody().ExpressionBody is null
                   && methodDeclaration.IsExtensionMethod
                   && methodDeclaration.ParameterDeclarations.Count > 0
                   && methodDeclaration.DeclaredElement != null
                   && !methodDeclaration.DeclaredElement.ReturnType.IsVoid();
        }

        private static bool CanGenerateEnumMapperMethod(IMethodDeclaration methodDeclaration)
        {
            var parameterType = methodDeclaration.ParameterDeclarations[0].Type;
            var returnType = methodDeclaration.DeclaredElement!.ReturnType;

            return parameterType.IsEnumType() && returnType.IsEnumType();
        }

        private static bool CanGenerateObjectMapperMethod(IMethodDeclaration methodDeclaration)
        {
            var parameterType = methodDeclaration.ParameterDeclarations[0].Type;
            var returnType = methodDeclaration.DeclaredElement!.ReturnType;

            return !parameterType.IsEnumType() && !returnType.IsEnumType();
        }
    }
}