namespace MacDeviceEmulator.Abstractions;

/// <summary>Evaluates locally whether recognized speech should be sent to JARVIS.</summary>
public interface IUtteranceGate
{
    GateDecision Evaluate(string? transcript, GateContext context);
}
