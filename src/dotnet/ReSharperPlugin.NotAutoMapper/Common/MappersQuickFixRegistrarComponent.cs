using JetBrains.Application;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using ReSharperPlugin.NotAutoMapper.EnumMapper;
using ReSharperPlugin.NotAutoMapper.ObjectMapper;

namespace ReSharperPlugin.NotAutoMapper.Common
{
    [ShellComponent]
    internal class MapperQuickFixRegistrarComponent
    {
        public MapperQuickFixRegistrarComponent(IQuickFixes table)
        {
            table.RegisterQuickFix<CanGenerateEnumMapperHighlighting>(
                Lifetime.Eternal,
                highlighting => new GenerateEnumMapperQuickFix(highlighting),
                typeof(GenerateEnumMapperQuickFix));
            
            table.RegisterQuickFix<CanGenerateObjectMapperHighlighting>(
                Lifetime.Eternal,
                highlighting => new GenerateObjectMapperQuickFix(highlighting),
                typeof(GenerateObjectMapperQuickFix));
        }
    }
}