using Silk.NET.OpenAL;

namespace DeepSpaceSaga.Client.UI;

/// <summary>Owned and used by one audio thread, including context teardown.</summary>
internal sealed unsafe class OpenAlTonePlayer : IDisposable
{
    private readonly ALContext _alc = ALContext.GetApi();
    private readonly AL _al = AL.GetApi();
    private Device* _device;
    private Context* _context;
    private uint _source;
    private readonly Dictionary<CombatTone, uint> _buffers = new();

    internal OpenAlTonePlayer()
    {
        try
        {
            _device = _alc.OpenDevice("");
            if (_device == null) throw new InvalidOperationException("OpenAL has no playback device");
            _context = _alc.CreateContext(_device, null);
            if (_context == null || !_alc.MakeContextCurrent(_context)) throw new InvalidOperationException("OpenAL context unavailable");
            _source = _al.GenSource();
            Check();
        }
        catch { Dispose(); throw; }
    }

    internal void Prepare(CombatTone tone)
    {
        if (_buffers.ContainsKey(tone)) return;
        short[] samples = tone.Generate();
        uint buffer = _al.GenBuffer();
        try
        {
            fixed (short* data = samples) _al.BufferData(buffer, BufferFormat.Mono16, data, samples.Length * sizeof(short), tone.SampleRate);
            Check();
            _buffers.Add(tone, buffer);
        }
        catch { _al.DeleteBuffer(buffer); throw; }
    }

    internal void Play(CombatTone tone, Func<bool> active)
    {
        Prepare(tone);
        if (!active()) return;
        _al.SetSourceProperty(_source, SourceInteger.Buffer, (int)_buffers[tone]);
        _al.SourcePlay(_source);
        Check();
        int elapsed = 0;
        while (elapsed < tone.DurationMs && active())
        {
            int delay = Math.Min(10, tone.DurationMs - elapsed);
            Thread.Sleep(delay);
            elapsed += delay;
        }
        _al.SourceStop(_source);
        Check();
    }

    private void Check()
    {
        var error = _al.GetError();
        if (error != AudioError.NoError) throw new InvalidOperationException("OpenAL: " + error);
    }

    public void Dispose()
    {
        if (_context != null)
        {
            if (_source != 0) { _al.SourceStop(_source); _al.DeleteSource(_source); _source = 0; }
            foreach (uint buffer in _buffers.Values) _al.DeleteBuffer(buffer);
            _buffers.Clear();
            _alc.MakeContextCurrent(null);
            _alc.DestroyContext(_context);
            _context = null;
        }
        if (_device != null) { _alc.CloseDevice(_device); _device = null; }
        _al.Dispose();
        _alc.Dispose();
    }
}
