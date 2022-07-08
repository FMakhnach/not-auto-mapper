using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Util;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.NotAutoMapper.Common;

namespace ReSharperPlugin.NotAutoMapper.ObjectMapper
{
    [ContextAction(
        Group = "C#",
        Name = nameof(GenerateObjectConstructorMapperContextAction),
        Description = "Generate object mapper using constructor")]
    public sealed class GenerateObjectConstructorMapperContextAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider _dataProvider;

        private IMethodDeclaration _methodDeclaration;
        private TreeNodeCollection<ICSharpParameterDeclaration> _parameters;
        private IType _returnType;
        private CSharpElementFactory _factory;

        public override string Text => "Generate object mapper method using constructor";

        public GenerateObjectConstructorMapperContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            _methodDeclaration = _dataProvider.GetSelectedElement<IMethodDeclaration>();

            return MapperAnalyzer.CanGenerateObjectConstructorMapperMethod(_methodDeclaration);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            _methodDeclaration = _dataProvider.GetSelectedElement<IMethodDeclaration>()!;
            _parameters = _methodDeclaration.ParameterDeclarations;
            _returnType = _methodDeclaration.DeclaredElement!.ReturnType;
            _factory = CSharpElementFactory.GetInstance(_methodDeclaration);

            var hotspotsRegistry = new HotspotsRegistry(_methodDeclaration.GetPsiServices());

            var constructor = _returnType.GetTypeElement()?.Constructors.First(x => !x.IsDefault);

            var objectMapper = new ObjectMapper(solution, _methodDeclaration.GetPsiModule());

            var mapping = objectMapper.MapParameters(constructor!.Parameters, _parameters);

            var returnStatement = CreateReturnConstructor(mapping, hotspotsRegistry);

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, returnStatement.GetDocumentEndOffset());
        }

        private IReturnStatement CreateReturnConstructor(IDictionary<ITypeOwner, string> mapping, HotspotsRegistry hotspotsRegistry)
        {
            var methodBody = _methodDeclaration.SetBody(_factory.CreateEmptyBlock());

            var paramPlaceholders = string.Join(", ", Enumerable.Range(1, mapping.Count).Select(num => $"${num}"));

            var args = new List<object> { _returnType };
            var notFoundArgIds = new List<int>();
            var id = 0;

            foreach (var (argument, value) in mapping)
            {
                var expression = value is null
                    ? CSharpDefaultValueUtil.GetDefaultValue(argument.Type, _methodDeclaration, true)
                    : _factory.CreateExpression(value);

                var arg = _factory.CreateArgument(ParameterKind.VALUE, argument.ShortName, expression);

                args.Add(arg);

                if (value is null)
                {
                    notFoundArgIds.Add(id);
                }

                ++id;
            }

            var constructorExpression =
                _factory.CreateExpression($"new $0({paramPlaceholders});", args: args.ToArray());

            var statement = _factory.CreateStatement("return $0;", constructorExpression);

            var returnStatement = methodBody.AddStatementAfter(statement, null) as IReturnStatement;
            var addedConstructorExpression = returnStatement!.Value as IObjectCreationExpression;

            foreach (var notFoundArgId in notFoundArgIds)
            {
                var arg = addedConstructorExpression!.Arguments[notFoundArgId].Value;

                hotspotsRegistry.Register(new ITreeNode[] { arg });
            }

            return returnStatement;
        }
    }
}