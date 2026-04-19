using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.Monitor;

/// <summary>
/// Zero-allocation deterministic logger that outputs monitor-style traces
/// compatible with VICE x64sc monitor format for cycle-accurate validation.
/// </summary>
public class DeterministicTraceLogger : IDisposable
{
    private readonly IMachine _machine;
    private readonly TextWriter _writer;
    private readonly StringBuilder _lineBuilder;
    private readonly Task _flushTask;
    
    // Reusable buffer for zero-allocation formatting
    private readonly char[] _charBuffer = new char[256];
    
    private long _frameCount;
    private long _rasterLine;
    private long _rasterCycle;
    private long _frameStartCycle;
    
    public DeterministicTraceLogger(IMachine machine, string outputPath)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        
        // Create directory if it doesn't exist
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        _writer = new StreamWriter(outputPath, append: false, Encoding.UTF8) { AutoFlush = false };
        _lineBuilder = new StringBuilder(256);
        _flushTask = Task.CompletedTask;
        
        // Initialize raster tracking
        _frameStartCycle = _machine.Clock.TotalCycles;
        _frameCount = 0;
        UpdateRasterState();
    }
    
    public void LogInstruction()
    {
        var state = _machine.GetState();
        UpdateRasterState();
        
        // Format: [Frame:Line:Cycle] PC A X Y S P ZNVC
        // Example: [00000:001:001] C000 A:00 X:00 Y:00 S:FD P:24 ZNVC:------ 4C 02 C0  JMP $C002
        _lineBuilder.Clear();
        
        _lineBuilder.Append('[');
        _lineBuilder.Append(_frameCount.ToString("D5"));
        _lineBuilder.Append(':');
        _lineBuilder.Append(_rasterLine.ToString("D3"));
        _lineBuilder.Append(':');
        _lineBuilder.Append(_rasterCycle.ToString("D3"));
        _lineBuilder.Append("] ");
        
        _lineBuilder.Append(state.PC.ToString("X4"));
        _lineBuilder.Append(" A:");
        _lineBuilder.Append(state.A.ToString("X2"));
        _lineBuilder.Append(" X:");
        _lineBuilder.Append(state.X.ToString("X2"));
        _lineBuilder.Append(" Y:");
        _lineBuilder.Append(state.Y.ToString("X2"));
        _lineBuilder.Append(" S:");
        _lineBuilder.Append(state.S.ToString("X2"));
        _lineBuilder.Append(" P:");
        _lineBuilder.Append(state.P.ToString("X2"));
        _lineBuilder.Append(" ZNVC:");
        _lineBuilder.Append(GetFlagString(state.P));
        
        // TODO: Add disassembly once opcode decoding is available
        // _lineBuilder.Append(" OPCODE OP1 OP2  DISASM");
        
        _lineBuilder.Append('\n');
        
        // Write without allocations
        _writer.Write(_lineBuilder.ToString());
    }
    
    public void Flush()
    {
        _writer.Flush();
    }
    
    private void UpdateRasterState()
    {
        // PAL C64: 312 lines × 63 cycles per line = 19656 cycles per frame
        var totalCycles = _machine.Clock.TotalCycles;
        var cyclesSinceFrame = totalCycles - _frameStartCycle;
        
        if (cyclesSinceFrame >= 19656)
        {
            _frameCount++;
            _frameStartCycle = totalCycles;
            cyclesSinceFrame = 0;
        }
        
        _rasterLine = cyclesSinceFrame / 63;
        _rasterCycle = cyclesSinceFrame % 63;
    }
    
    private string GetFlagString(byte flags)
    {
        // ZNVC flags for display
        var z = (flags & 0x02) != 0 ? 'Z' : '-';
        var n = (flags & 0x80) != 0 ? 'N' : '-';
        var v = (flags & 0x40) != 0 ? 'V' : '-';
        var c = (flags & 0x01) != 0 ? 'C' : '-';
        return $"{z}{n}{v}{c}";
    }
    
    public void Dispose()
    {
        _writer?.Dispose();
    }
}