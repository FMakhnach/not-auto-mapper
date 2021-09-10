using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.NotAutoMapper.Mapping;

namespace ReSharperPlugin.NotAutoMapper.Components.EnumMapper
{
    public sealed class GenerateEnumMapperQuickFix : QuickFixBase
    {
        [NotNull] private readonly IMethodDeclaration _methodDeclaration;
        private readonly ICSharpParameterDeclaration _parameter;
        private readonly IType _returnType;

        private readonly CSharpElementFactory _factory;

        public GenerateEnumMapperQuickFix(CanGenerateEnumMapperHighlighting highlighting)
        {
            _methodDeclaration = highlighting.Declaration;
            _parameter = _methodDeclaration.ParameterDeclarations[0];
            _returnType = _methodDeclaration.DeclaredElement.ReturnType;

            _factory = CSharpElementFactory.GetInstance(_methodDeclaration);
        }

        public override string Text => "Generate mapper";

        // Actual check is performed in ProblemAnalyzer
        public override bool IsAvailable(IUserDataHolder cache) => true;

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var switchExpression = CreateSwitchExpressionForEnumTypes(_parameter.Type.GetEnumType(), _returnType.GetEnumType());

            var block = WrapWithReturnMethodBlock(switchExpression);

            _methodDeclaration.SetBody(block);

            return null;
        }

        private ISwitchExpression CreateSwitchExpressionForEnumTypes(IEnum paramType, IEnum returnType)
        {
            var switchExpression = CreateEmptySwitchExpression(_parameter.DeclaredElement);

            FillSwitchExpression(switchExpression, paramType, returnType);

            return switchExpression;
        }

        private ISwitchExpression CreateEmptySwitchExpression(IParameter governingParameter)
        {
            var switchExpression = _factory.CreateEmptySwitchExpression();
            var governingExpression = _factory.CreateExpression("$0", governingParameter);
            switchExpression.SetGoverningExpression(governingExpression);

            return switchExpression;
        }

        private void FillSwitchExpression(ISwitchExpression switchExpression, IEnum paramType, IEnum returnType)
        {
            var ofRangeException = switchExpression.GetPredefinedType().ArgumentOutOfRangeException;
            var throwExpression =
                _factory.CreateThrowExpression("new $0(nameof($1), $1, null)", ofRangeException, _parameter.DeclaredElement);

            ISwitchExpressionArm prevArm = null;
            var fieldsMapping = paramType.EnumMembers.MapOnto(returnType.EnumMembers);
            foreach (var (key, value) in fieldsMapping)
            {
                var nextSwitchArm = _factory.CreateSwitchExpressionArm("$0 => $1", key, (object) value ?? throwExpression);
                prevArm = switchExpression.AddSwitchExpressionArmAfter(nextSwitchArm, prevArm);
            }

            var lastSwitchArm = _factory.CreateSwitchExpressionArm("_ => $0", throwExpression);
            switchExpression.AddSwitchExpressionArmAfter(lastSwitchArm, prevArm);
        }

        private IBlock WrapWithReturnMethodBlock(IExpression expression)
        {
            var block = _factory.CreateEmptyBlock();

            var statement = _factory.CreateStatement("return $0;", expression);
            block.AddStatementAfter(statement, null);

            return block;
        }
    }
}