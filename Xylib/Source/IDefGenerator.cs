namespace Xylib;

public interface IDefGenerator<out T> where T : Def, new()
{
    static abstract IEnumerable<T> ImpliedDefs(bool hotReload);
}
