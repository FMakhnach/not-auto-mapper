using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther

namespace ReSharperPlugin.NotAutoMapper.ObjectMapper
{
    [RegisterConfigurableSeverity(
        nameof(CanGenerateObjectMapperHighlighting),
        CompoundItemName: null,
        Group: HighlightingGroupIds.BestPractice,
        Title: Message,
        Description: null,
        DefaultSeverity: Severity.SUGGESTION)]
    [ConfigurableSeverityHighlighting(
        nameof(CanGenerateObjectMapperHighlighting),
        CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.NONE,
        OverloadResolvePriority = int.MaxValue,
        ToolTipFormatString = Message)]
    public class CanGenerateObjectMapperHighlighting : IHighlighting
    {
        private const string Message = "Can generate object mapper method";
        
        public string ToolTip => Message;

        public string ErrorStripeToolTip => "Generate object mapper method";

        public CanGenerateObjectMapperHighlighting(IMethodDeclaration declaration)
        {
            Declaration = declaration;
        }

        public IMethodDeclaration Declaration { get; }

        // Actual check is performed in ProblemAnalyzer
        public bool IsValid() => true;

        public DocumentRange CalculateRange()
        {
            return Declaration.GetDocumentRange();
        }
    }
}