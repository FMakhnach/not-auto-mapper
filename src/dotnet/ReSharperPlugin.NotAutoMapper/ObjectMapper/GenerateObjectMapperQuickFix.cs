using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Util;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.NotAutoMapper.ObjectMapper
{
    public sealed class GenerateObjectMapperQuickFix : QuickFixBase
    {
        [NotNull] private readonly IMethodDeclaration _methodDeclaration;

        private readonly TreeNodeCollection<ICSharpParameterDeclaration> _parameters;
        private readonly IType _returnType;

        private readonly CSharpElementFactory _factory;

        public GenerateObjectMapperQuickFix(CanGenerateObjectMapperHighlighting highlighting)
        {
            _methodDeclaration = highlighting.Declaration;
            _parameters = _methodDeclaration.ParameterDeclarations;
            _returnType = _methodDeclaration.DeclaredElement!.ReturnType;

            _factory = CSharpElementFactory.GetInstance(_methodDeclaration);
        }

        public override string Text => "Generate object mapper method";

        // Actual check is performed in ProblemAnalyzer
        public override bool IsAvailable(IUserDataHolder cache)
            => true;

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var hotspotsRegistry = new HotspotsRegistry(_methodDeclaration.GetPsiServices());

            var (returnStatement, objectInitializer) = CreateReturnConstructorWithInitializer();

            var objectMapper = new ObjectMapper(solution, returnStatement.GetPsiModule());

            FillInitializer(objectInitializer, objectMapper, hotspotsRegistry);

            objectInitializer.FormatNode(CodeFormatProfile.SPACIOUS, NullProgressIndicator.Create());

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, returnStatement.GetDocumentEndOffset());
        }

        private (IReturnStatement, IObjectInitializer) CreateReturnConstructorWithInitializer()
        {
            var methodBody = _methodDeclaration.SetBody(_factory.CreateEmptyBlock());

            var constructorExpression = _factory.CreateExpression("new $0", _returnType) as IObjectCreationExpression;
            constructorExpression!.SetInitializer(_factory.CreateObjectInitializer());
            var statement = _factory.CreateStatement("return $0;", constructorExpression);

            // I don't like what I'm doing
            var returnStatement = methodBody.AddStatementAfter(statement, null) as IReturnStatement;
            var objectExpression = returnStatement!.Value as IObjectCreationExpression;
            var objectInitializer = objectExpression!.Initializer as IObjectInitializer;

            return (returnStatement, objectInitializer);
        }

        private void FillInitializer(IObjectInitializer initializer, ObjectMapper objectMapper, HotspotsRegistry hotspotsRegistry)
        {
            var returnType = _returnType.GetTypeElement();

            var mapping = objectMapper.MapProperties(returnType, _parameters);

            IMemberInitializer memberInitializer = null;

            foreach (var (property, value) in mapping)
            {
                var defaultExpression = CSharpDefaultValueUtil.GetDefaultValue(property.Type, initializer, true);

                var expression = value is null
                    ? defaultExpression
                    : _factory.CreateExpression("$0", value);

                var propertyInitializer = _factory.CreateObjectPropertyInitializer(property.ShortName, expression);

                memberInitializer = initializer.AddMemberInitializerAfter(propertyInitializer, memberInitializer);

                if (value is null)
                {
                    hotspotsRegistry.Register(
                        new ITreeNode[]
                        {
                            memberInitializer.Expression
                        });
                }
            }
        }
    }
}