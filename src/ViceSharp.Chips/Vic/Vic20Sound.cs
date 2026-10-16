namespace ViceSharp.Chips.Vic;

/// <summary>
/// Managed port of VICE <c>vic20sound.c</c> VIC-I audio (cycle clock + RC filters).
/// FR-VIC20-SOUND-001 / TR-VIC20-SOUND-001. Deterministic PCM16 mono at a fixed rate.
/// </summary>
public sealed class Vic20Sound
{
    private readonly Channel[] _ch = new Channel[4];
    private byte _volume;
    private int _accum;
    private int _accumCycles;
    private float _baseCyclesPerSample;
    private float _cyclesPerSample;
    private float _leftoverCycles;
    private float _highpassBuf;
    private float _highpassBeta;
    private float _lowpassBuf;
    private float _lowpassBeta;
    private int _speed;
    private ushort _noiseLfsr;
    private byte _noiseLfsr0Old;
    private bool _initialized;

    private struct Channel
    {
        public byte Out;
        public byte Reg;
        public byte Shift;
        public short Ctr;
    }

    /// <summary>Initialize sample rate and CPU clock (VICE <c>vic_sound_machine_init</c>).</summary>
    public void Init(int sampleRate, int cyclesPerSec)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (cyclesPerSec <= 0)
            throw new ArgumentOutOfRangeException(nameof(cyclesPerSec));

        Array.Clear(_ch);
        _volume = 0;
        _accum = 0;
        _accumCycles = 0;
        _baseCyclesPerSample = (float)cyclesPerSec / sampleRate;
        _cyclesPerSample = _baseCyclesPerSample;
        _leftoverCycles = 0f;
        _lowpassBuf = 0f;
        _highpassBuf = 0f;
        _speed = sampleRate;
        _noiseLfsr = 0;
        _noiseLfsr0Old = 0;

        var dt = 1f / sampleRate;
        // Low-pass: R=1k, C=100nF; High-pass: R=1k, C=1uF (VICE comments).
        _lowpassBeta = dt / (dt + 1e-4f);
        _highpassBeta = dt / (dt + 1e-3f);
        _initialized = true;
    }

    public void Reset()
    {
        for (ushort i = 10; i < 15; i++)
            Store(i, 0);
        _accum = 0;
        _accumCycles = 0;
        _leftoverCycles = 0f;
        _lowpassBuf = 0f;
        _highpassBuf = 0f;
        _noiseLfsr = 0;
        _noiseLfsr0Old = 0;
    }

    /// <summary>
    /// Adjusts live output cadence like VICE sound relative speed without
    /// disturbing channel state.
    /// </summary>
    public void SetRelativeSpeed(double speedPercent)
    {
        if (speedPercent <= 0.0)
            return;

        _cyclesPerSample =
            _baseCyclesPerSample * (float)(Math.Min(speedPercent, 200.0) / 100.0);
    }

    /// <summary>VICE <c>vic_sound_store</c> / machine_store for $900A-$900E (addr &amp; 0x0F).</summary>
    public void Store(ushort addr, byte value)
    {
        addr &= 0x0F;
        switch (addr)
        {
            case 0xA:
                _ch[0].Reg = value;
                break;
            case 0xB:
                _ch[1].Reg = value;
                break;
            case 0xC:
                _ch[2].Reg = value;
                break;
            case 0xD:
                _ch[3].Reg = value;
                break;
            case 0xE:
                _volume = (byte)(value & 0x0F);
                break;
        }
    }

    /// <summary>VICE <c>vic_sound_clock</c>.</summary>
    public void Clock(int cycles)
    {
        if (cycles <= 0)
            return;

        for (var j = 0; j < 4; j++)
        {
            // Channel speeds: "\4\3\2\1"[j]. A switch avoids the Debug-JIT
            // heap allocation caused by a per-call collection expression.
            var channelSpeed = j switch
            {
                0 => 4,
                1 => 3,
                2 => 2,
                _ => 1,
            };
            if (_ch[j].Ctr > cycles)
            {
                _accum += _ch[j].Out * cycles;
                _ch[j].Ctr = (short)(_ch[j].Ctr - cycles);
            }
            else
            {
                for (var i = cycles; i > 0; i--)
                {
                    _ch[j].Ctr--;
                    if (_ch[j].Ctr <= 0)
                    {
                        var a = (~_ch[j].Reg) & 127;
                        a = a != 0 ? a : 128;
                        _ch[j].Ctr = (short)(_ch[j].Ctr + (a << channelSpeed));
                        var enabled = (_ch[j].Reg & 128) >> 7;
                        var edgeTrigger = (_noiseLfsr & 1) & (~_noiseLfsr0Old & 1);

                        if (j != 3 || edgeTrigger != 0)
                        {
                            var shift = _ch[j].Shift;
                            shift = (byte)((shift << 1) | (((((shift & 128) >> 7)) ^ 1) & enabled));
                            _ch[j].Shift = shift;
                        }

                        if (j == 3)
                        {
                            var bit3 = (_noiseLfsr >> 3) & 1;
                            var bit12 = (_noiseLfsr >> 12) & 1;
                            var bit14 = (_noiseLfsr >> 14) & 1;
                            var bit15 = (_noiseLfsr >> 15) & 1;
                            var gate1 = bit3 ^ bit12;
                            var gate2 = bit14 ^ bit15;
                            var gate3 = (gate1 ^ gate2) ^ 1;
                            var gate4 = (gate3 & enabled) ^ 1;
                            _noiseLfsr0Old = (byte)(_noiseLfsr & 1);
                            _noiseLfsr = (ushort)((_noiseLfsr << 1) | gate4);
                        }

                        _ch[j].Out = (byte)(_ch[j].Shift & (j == 3 ? enabled : 1));
                    }

                    _accum += _ch[j].Out;
                }
            }
        }

        _accumCycles += cycles;
    }

    /// <summary>
    /// Render mono int16 samples while consuming <paramref name="deltaTCycles"/> of CPU clock
    /// (VICE <c>vic_sound_machine_calculate_samples</c> int16 path, mono).
    /// </summary>
    public int RenderSamples(Span<short> buffer, int deltaTCycles)
    {
        if (!_initialized)
            Init(44100, 1_108_405); // PAL default if forgotten

        var s = 0;
        var delta = deltaTCycles;
        var nr = buffer.Length;

        while (s < nr && delta >= _cyclesPerSample - _leftoverCycles)
        {
            var samplesToDo = (int)(_cyclesPerSample - _leftoverCycles);
            _leftoverCycles += samplesToDo - _cyclesPerSample;
            Clock(samplesToDo);

            var o = _lowpassBuf - _highpassBuf;
            _highpassBuf += _highpassBeta * (_lowpassBuf - _highpassBuf);
            var idx = 0;
            if (_accumCycles > 0)
                idx = (((_accum * 7) / _accumCycles) + 1) * _volume;
            if ((uint)idx >= (uint)Vic20SoundVoltage.Table.Length)
                idx = Vic20SoundVoltage.Table.Length - 1;
            _lowpassBuf += _lowpassBeta * (Vic20SoundVoltage.Table[idx] - _lowpassBuf);

            short vicbuf;
            if (o < -32768)
                vicbuf = -32768;
            else if (o > 32767)
                vicbuf = 32767;
            else
                vicbuf = (short)o;

            buffer[s] = vicbuf;
            s++;
            _accum = 0;
            _accumCycles = 0;
            delta -= samplesToDo;
        }

        if (delta > 0)
        {
            _leftoverCycles += delta;
            Clock(delta);
        }

        return s;
    }
}
