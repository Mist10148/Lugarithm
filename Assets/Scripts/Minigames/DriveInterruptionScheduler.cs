using System;

/// <summary>
/// Deterministic rhythm for drive interruptions shared by Manual and Automation.
/// Progression gates always run Flow Connect then Crate Stack; repairs guarantee
/// two spaced engine events before rare extras are allowed. Fuel remains a pure
/// empty-tank trigger handled by the caller.
/// </summary>
public class DriveInterruptionScheduler
{
    public const int ProgressionGateCount = 2;
    public const int GuaranteedRepairCount = 2;

    const float ExtraRepairCooldown = 0.12f;
    const float ExtraRepairChance = 0.012f;

    readonly float[] _progressionThresholds;
    readonly float[] _repairThresholds;

    int _progressionIndex;
    int _repairIndex;
    float _lastRepairProgress = -1f;

    public int CompletedProgressionGates => _progressionIndex;
    public int CompletedRepairs => _repairIndex;
    public bool AllProgressionGatesDone => _progressionIndex >= ProgressionGateCount;
    public bool GuaranteedRepairsDone => _repairIndex >= GuaranteedRepairCount;

    public DriveInterruptionScheduler(int seed)
    {
        var rng = new Random(seed);
        _progressionThresholds = new[]
        {
            0.26f + (float)rng.NextDouble() * 0.06f,
            0.58f + (float)rng.NextDouble() * 0.08f,
        };
        _repairThresholds = new[]
        {
            0.36f + (float)rng.NextDouble() * 0.08f,
            0.72f + (float)rng.NextDouble() * 0.08f,
        };
    }

    public bool TryStartProgression(float progress01, out TownPuzzleKind kind)
    {
        kind = TownPuzzleKind.None;
        if (_progressionIndex >= ProgressionGateCount) return false;
        if (ClampProgress(progress01) < _progressionThresholds[_progressionIndex]) return false;

        kind = ProgressionKindAt(_progressionIndex);
        _progressionIndex++;
        return true;
    }

    public bool TryForceNextProgression(out TownPuzzleKind kind)
    {
        kind = TownPuzzleKind.None;
        if (_progressionIndex >= ProgressionGateCount) return false;

        kind = ProgressionKindAt(_progressionIndex);
        _progressionIndex++;
        return true;
    }

    public bool TryStartRepair(float progress01, float random01)
    {
        float progress = ClampProgress(progress01);
        if (_repairIndex < GuaranteedRepairCount)
        {
            if (progress < _repairThresholds[_repairIndex]) return false;

            _repairIndex++;
            _lastRepairProgress = progress;
            return true;
        }

        if (progress - _lastRepairProgress < ExtraRepairCooldown) return false;
        if (random01 > ExtraRepairChance) return false;

        _repairIndex++;
        _lastRepairProgress = progress;
        return true;
    }

    public bool ShouldRefuel(float fuel01)
    {
        return fuel01 <= 0f;
    }

    static TownPuzzleKind ProgressionKindAt(int index)
    {
        return index == 0 ? TownPuzzleKind.FlowConnect : TownPuzzleKind.CrateStack;
    }

    static float ClampProgress(float progress01)
    {
        if (float.IsNaN(progress01) || float.IsInfinity(progress01)) return 0f;
        if (progress01 < 0f) return 0f;
        return progress01;
    }
}
