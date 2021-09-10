using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;
using ReSharperPlugin.NotAutoMapper.PluginComponents.Zones;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.NotAutoMapper.Tests
{

    [ZoneDefinition]
    public class NotAutoMapperTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<INotAutoMapperZone> { }

    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<NotAutoMapperTestEnvironmentZone> { }
    
    [SetUpFixture]
    public class NotAutoMapperTestsAssembly : ExtensionTestEnvironmentAssembly<NotAutoMapperTestEnvironmentZone> { }
}