/// <summary>
/// Wave that can gate "Press E" between elements (e.g. from a gate volume).
/// </summary>
public interface IWaveAdvance
{
    void SetAdvanceAllowed(bool allowed);
}
