using System;
using JetBrains.Application.Progress;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.NotAutoMapper.Common;

namespace ReSharperPlugin.NotAutoMapper.EnumMapper
{
    [ContextAction(
        Group = "C#",
        Name = nameof(GenerateEnumMapperContextAction),
        Description = "Generate enum mapper")]
    public sealed class GenerateEnumMapperContextAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider _dataProvider;

        private IMethodDeclaration _methodDeclaration;
        private ICSharpParameterDeclaration _parameter;
        private IType _returnType;
        private CSharpElementFactory _factory;

        public override string Text => "Generate enum mapper method";

        public GenerateEnumMapperContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        // Actual check is performed in ProblemAnalyzer
        public override bool IsAvailable(IUserDataHolder cache)
        {
            _methodDeclaration = _dataProvider.GetSelectedElement<IMethodDeclaration>();

            return MapperAnalyzer.CanGenerateEnumMapperMethod(_methodDeclaration);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            _methodDeclaration = _dataProvider.GetSelectedElement<IMethodDeclaration>()!;
            _parameter = _methodDeclaration.ParameterDeclarations[0];
            _returnType = _methodDeclaration.DeclaredElement!.ReturnType;

            _factory = CSharpElementFactory.GetInstance(_methodDeclaration);

            var hotspotsRegistry = new HotspotsRegistry(_methodDeclaration.GetPsiServices());

            var (returnStatement, switchExpression) = CreateReturnSwitchExpression();

            FillSwitchExpression(
                switchExpression,
                _parameter.Type.GetEnumType(),
                _returnType.GetEnumType(),
                hotspotsRegistry);

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, returnStatement.GetDocumentEndOffset());
        }

        private (IReturnStatement, ISwitchExpression) CreateReturnSwitchExpression()
        {
            var methodBody = _methodDeclaration.SetBody(_factory.CreateEmptyBlock());

            var switchExpression = _factory.CreateEmptySwitchExpression();
            var governingParameter = _parameter.DeclaredElement;
            var governingExpression = _factory.CreateExpression("$0", governingParameter);
            switchExpression.SetGoverningExpression(governingExpression);
            var statement = _factory.CreateStatement("return $0;", switchExpression);

            // Мудацкая система, в которой методы (SetBody, AddStatement) создают копии переданных объектов внутри
            // вынуждает писать подобный кринж с вытаскиванием ISwitchExpression из жопы, вместо использования исходного объекта
            var returnStatement = methodBody.AddStatementAfter(statement, null) as IReturnStatement;
            var realSwitchExpression = returnStatement!.Value as ISwitchExpression;

            return (returnStatement, realSwitchExpression);
        }

        private void FillSwitchExpression(
            ISwitchExpression switchExpression,
            IEnum paramType,
            IEnum returnType,
            HotspotsRegistry hotspotsRegistry)
        {
            // Использую ArgumentOutOfRangeException для enum значений, для которых не нашлось совпадения + для discard ветки.
            var ofRangeException = switchExpression.GetPredefinedType().ArgumentOutOfRangeException;
            var throwExpression =
                _factory.CreateThrowExpression("new $0(nameof($1), $1, null)", ofRangeException, _parameter.DeclaredElement);

            var (mappedFields, notMappedFields) = EnumMapper.Map(returnType, paramType);

            ISwitchExpressionArm prevArm = null;

            // Добавляю смапленные филды
            foreach (var (from, to) in mappedFields)
            {
                var nextSwitchArm = _factory.CreateSwitchExpressionArm("$0 => $1", from, to);
                prevArm = switchExpression.AddSwitchExpressionArmAfter(nextSwitchArm, prevArm);
            }

            // После добавляю отображение несмапленных филдов на ArgumentOutOfRangeException
            foreach (var field in notMappedFields)
            {
                var nextSwitchArm = _factory.CreateSwitchExpressionArm("$0 => $1", field, throwExpression);
                prevArm = switchExpression.AddSwitchExpressionArmAfter(nextSwitchArm, prevArm);
                // Эта штучка чтобы сразу можно было указать вручную что там должно быть
                hotspotsRegistry.Register(
                    new ITreeNode[]
                    {
                        prevArm.Expression
                    });
            }

            var discardSwitchArm = _factory.CreateSwitchExpressionArm("_ => $0", throwExpression);
            switchExpression.AddSwitchExpressionArmAfter(discardSwitchArm, prevArm);
        }
    }
}