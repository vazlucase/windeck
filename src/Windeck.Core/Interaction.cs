namespace Windeck.Core;

public enum IslandState { Rest, Peek, Expanded }
public enum DeckPage { Home, Apps, Windows, Tray, Media, Calendar, Controls, Settings }
public readonly record struct PointerSample(double X, double Y, double Seconds);
public readonly record struct SurfaceRect(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y, double padding = 0) => x >= X-padding && x <= X+Width+padding && y >= Y-padding && y <= Y+Height+padding;
}

/// <summary>Pure interaction model; all units are DIPs and monotonic seconds.</summary>
public sealed class IslandController
{
    private PointerSample? previous;
    private double entered = -1, left = -1, lastStrike = -10;
    private double upwardTravel;
    public IslandState State { get; private set; }
    public DeckPage Page { get; private set; } = DeckPage.Home;
    public double StrikeSpeed { get; set; } = 1100;
    public bool GesturesEnabled { get; set; } = true;
    public event Action<IslandState>? Changed;

    public void Update(PointerSample point, SurfaceRect surface, bool suppressed = false)
    {
        bool near = surface.Contains(point.X, point.Y, State == IslandState.Rest ? 36 : 24);
        if (suppressed) { previous = null; upwardTravel = 0; entered = -1; return; }
        if (previous is { } p)
        {
            double dt = point.Seconds - p.Seconds;
            if (dt > 0 && dt <= .12)
            {
                double dy = p.Y - point.Y;
                upwardTravel = dy > 0 ? upwardTravel + dy : 0;
                double speed = dy / dt;
                bool inCorridor = Math.Abs(point.X - (surface.X + surface.Width/2)) <= Math.Min(220, surface.Width/2+90);
                bool approaching = point.Y >= surface.Y - 6 && point.Y <= surface.Y + Math.Max(surface.Height, 160);
                bool directed = dy > Math.Abs(point.X - p.X) * 1.25;
                if (GesturesEnabled && State != IslandState.Expanded && inCorridor && approaching && directed && speed >= StrikeSpeed && upwardTravel >= 65 && point.Seconds-lastStrike >= 1)
                { lastStrike = point.Seconds; Expand(); upwardTravel = 0; }
            }
            else upwardTravel = 0;
        }
        previous = point;
        if (State == IslandState.Expanded) return;
        if (near)
        {
            left = -1;
            if (entered < 0) entered = point.Seconds;
            if (State == IslandState.Rest && point.Seconds-entered >= .14) Transition(IslandState.Peek);
        }
        else
        {
            entered = -1;
            if (left < 0) left = point.Seconds;
            if (State == IslandState.Peek && point.Seconds-left >= .45) Transition(IslandState.Rest);
        }
    }
    public void Expand(DeckPage page = DeckPage.Home) { Page = page; Transition(IslandState.Expanded); }
    public void Collapse() { entered = left = -1; Transition(IslandState.Rest); }
    public void Toggle() { if (State == IslandState.Expanded) Collapse(); else Expand(); }
    private void Transition(IslandState state) { if (State == state) return; State = state; Changed?.Invoke(state); }
}

/// <summary>Analytic critically damped spring: stable with variable refresh rates.</summary>
public sealed class MotionSpring(double value, double frequency = 20)
{
    public double Value { get; private set; } = value;
    public double Velocity { get; private set; }
    public double Target { get; set; } = value;
    public bool Settled => Math.Abs(Value-Target) < .08 && Math.Abs(Velocity) < .1;
    public void Snap(double target) { Target=Value=target; Velocity=0; }
    public double Step(double delta, bool reduceMotion = false)
    {
        if (reduceMotion) { Snap(Target); return Value; }
        double dt = Math.Clamp(delta, 0, .12), error = Value-Target;
        double impulse = Velocity + frequency*error, decay = Math.Exp(-frequency*dt);
        Value = Target + (error+impulse*dt)*decay;
        Velocity = (Velocity-frequency*impulse*dt)*decay;
        if (Settled) Snap(Target);
        return Value;
    }
}
