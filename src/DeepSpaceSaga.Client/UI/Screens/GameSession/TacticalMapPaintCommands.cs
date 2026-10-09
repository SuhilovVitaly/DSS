using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal enum TacticalMapAnimationKind { Selection, Active, EngineFlame, Status }
internal readonly record struct TacticalMapAnimation(TacticalMapAnimationKind Kind, float X, float Y,
    float Radius, double Direction = 0, SKRect Rect = default, SKColor Color = default,
    SimulationSpeed Speed = SimulationSpeed.Speed0);
internal readonly record struct TacticalMapPaintCommand(SKPicture? Picture = null,
    TacticalMapAnimation? Animation = null, string? Stage = null);

/// <summary>Scene-owned native display list. Valid through Draw, input and capture until scene replacement.</summary>
internal sealed class TacticalMapPaintCommands : IDisposable
{
    internal ImmutableArray<TacticalMapPaintCommand> Commands { get; }
    internal bool IsDisposed { get; private set; }
    internal TacticalMapPaintCommands(ImmutableArray<TacticalMapPaintCommand> commands) => Commands = commands;
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        foreach (var command in Commands) command.Picture?.Dispose();
    }
}

/// <summary>Prepare-only writer. No canvas or mutable command list escapes publication.</summary>
internal sealed class TacticalMapPaintRecorder : IDisposable
{
    private readonly SKPictureRecorder _recorder = new();
    private readonly ImmutableArray<TacticalMapPaintCommand>.Builder _commands = ImmutableArray.CreateBuilder<TacticalMapPaintCommand>();
    private readonly SKRect _bounds;
    private bool _finished;
    internal SKCanvas Canvas { get; private set; }
    internal TacticalMapPaintRecorder(int width, int height)
    {
        _bounds = SKRect.Create(Math.Max(1, width), Math.Max(1, height));
        Canvas = _recorder.BeginRecording(_bounds);
    }
    private void Flush()
    {
        var matrix = Canvas.TotalMatrix;
        int saves = Canvas.SaveCount;
        var clip = Canvas.DeviceClipBounds;
        _commands.Add(new(Picture: _recorder.EndRecording()));
        Canvas = _recorder.BeginRecording(_bounds);
        Canvas.ClipRect(new SKRect(clip.Left, clip.Top, clip.Right, clip.Bottom));
        while (Canvas.SaveCount < saves) Canvas.Save();
        Canvas.SetMatrix(matrix);
    }
    internal SKCanvas Animate(TacticalMapAnimation animation)
    {
        Flush();
        _commands.Add(new(Animation: animation));
        return Canvas;
    }
    internal SKCanvas Stage(string stage)
    {
        Flush();
        _commands.Add(new(Stage: stage));
        return Canvas;
    }
    internal TacticalMapPaintCommands Finish()
    {
        if (_finished) throw new InvalidOperationException("Display list already published.");
        _commands.Add(new(Picture: _recorder.EndRecording()));
        _finished = true;
        return new(_commands.ToImmutable());
    }
    public void Dispose()
    {
        if (!_finished)
        {
            using var unfinished = _recorder.EndRecording();
            foreach (var command in _commands) command.Picture?.Dispose();
        }
        _recorder.Dispose();
    }
}
