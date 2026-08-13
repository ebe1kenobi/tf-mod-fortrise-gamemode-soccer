using FortRise;

namespace TFModFortRiseGameModeSoccer;

public interface IHookable
{
    abstract static void Load(IHarmony harmony);
}

internal interface IRegisterable
{
  abstract static void Register(IModContent content, IModRegistry registry);
}