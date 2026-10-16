namespace ViceSharp.Library.ViewModels;

/// <summary>FR-CSDB-001. The kind of a CSDb scene release.</summary>
public enum CsdbKind
{
    /// <summary>A demo.</summary>
    Demo = 0,

    /// <summary>A cracked release.</summary>
    Crack = 1,

    /// <summary>A SID music file.</summary>
    Sid = 2,

    /// <summary>Anything else.</summary>
    Other = 3,
}
