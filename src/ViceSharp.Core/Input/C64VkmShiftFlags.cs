namespace ViceSharp.Core.Input;

internal static class C64VkmShiftFlags
{
    public const int CombineWithShift = 0x0001;
    public const int LeftShiftKey = 0x0002;
    public const int RightShiftKey = 0x0004;
    public const int ShiftOptional = 0x0008;
    public const int Deshift = 0x0010;
    public const int AnotherDefinitionFollows = 0x0020;
    public const int ShiftLockKey = 0x0040;
    public const int HostShiftRequired = 0x0080;
    public const int AlternativeMapping = 0x0100;
    public const int HostAltGrRequired = 0x0200;
    public const int HostCtrlRequired = 0x0400;
    public const int CombineWithCbm = 0x0800;
    public const int CombineWithCtrl = 0x1000;
    public const int CbmKey = 0x2000;
    public const int CtrlKey = 0x4000;
}
