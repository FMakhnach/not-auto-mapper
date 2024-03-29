﻿using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace ReSharperPlugin.NotAutoMapper.Common
{
    public class MapperAnalyzer
    {
        public static bool CanGenerateEnumMapperMethod([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if (!CanGenerateMapperMethod(methodDeclaration))
            {
                return false;
            }

            var parameterType = methodDeclaration!.ParameterDeclarations[0].Type;
            var returnType = methodDeclaration.DeclaredElement!.ReturnType;

            return parameterType.IsEnumType() && returnType.IsEnumType();
        }

        public static bool CanGenerateObjectMapperMethod([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if (!CanGenerateMapperMethod(methodDeclaration))
            {
                return false;
            }

            var parameterType = methodDeclaration!.ParameterDeclarations[0].Type;
            var returnType = methodDeclaration.DeclaredElement!.ReturnType;

            var containsDefaultConstructor = returnType.GetTypeElement()?.Constructors.Any(x => x.IsDefault) ?? false;

            return !parameterType.IsEnumType() && !returnType.IsEnumType() && containsDefaultConstructor;
        }

        public static bool CanGenerateObjectConstructorMapperMethod([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if (!CanGenerateMapperMethod(methodDeclaration))
            {
                return false;
            }

            var parameterType = methodDeclaration!.ParameterDeclarations[0].Type;
            var returnType = methodDeclaration!.DeclaredElement!.ReturnType;

            if (parameterType.IsEnumType() || returnType.IsEnumType())
            {
                return false;
            }

            var notDefaultConstructors = returnType.GetTypeElement()?.Constructors.Where(x => !x.IsDefault).ToArray();

            return notDefaultConstructors?.Length == 1;
        }

        private static bool CanGenerateMapperMethod([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if (methodDeclaration is null)
            {
                return false;
            }

            var body = methodDeclaration.GetCodeBody().BlockBody;

            return (body is null || body.Statements.IsEmpty)
                   && methodDeclaration.GetCodeBody().ExpressionBody is null
                   && methodDeclaration.IsExtensionMethod
                   && methodDeclaration.ParameterDeclarations.Count > 0
                   && methodDeclaration.DeclaredElement != null
                   && !methodDeclaration.DeclaredElement.ReturnType.IsVoid();
        }
    }
}