using JetBrains.Application;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using ReSharperPlugin.NotAutoMapper.Components.EnumMapper;

namespace ReSharperPlugin.NotAutoMapper.Components
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
        }
    }
}