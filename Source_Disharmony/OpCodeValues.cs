namespace Disharmony;

internal static class OpCodeValues
{
    // ReSharper disable IdentifierTypo
    // @formatter:off
    public const int Dup      =   0x25;
    public const int Ldarg    = 0xFE09;
    public const int Ldarg_0  =   0x02;
    public const int Ldarg_1  =   0x03;
    public const int Ldarg_2  =   0x04;
    public const int Ldarg_3  =   0x05;
    public const int Ldarg_S  =   0x0E;
    public const int Ldarga   = 0xFE0A;
    public const int Ldarga_S =   0x0F;
    public const int Starg    = 0xFE0B;
    public const int Starg_S  =   0x10;
    public const int Ldloc    = 0xFE0C;
    public const int Ldloc_0  =   0x06;
    public const int Ldloc_1  =   0x07;
    public const int Ldloc_2  =   0x08;
    public const int Ldloc_3  =   0x09;
    public const int Ldloc_S  =   0x11;
    public const int Ldloca   = 0xFE0D;
    public const int Ldloca_S =   0x12;
    public const int Ldobj    =   0x71;
    public const int Ldstr    =   0x72;
    public const int Ldfld    =   0x7B;
    public const int Ldflda   =   0x7C;
    public const int Ldsfld   =   0x7E;
    public const int Ldsflda  =   0x7F;
    public const int NewObj   =   0x73;
    public const int Ret      =   0x2A;
    public const int Stloc    = 0xFE0E;
    public const int Stloc_0  =   0x0A;
    public const int Stloc_1  =   0x0B;
    public const int Stloc_2  =   0x0C;
    public const int Stloc_3  =   0x0D;
    public const int Stloc_S  =   0x13;
    // @formatter:on
    // ReSharper restore IdentifierTypo
}
