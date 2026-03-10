namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;

public interface IChamber
{
    bool IsClosed(int? minOnMs = null);
    bool IsOpen(int? minOnMs = null);
    void SetOpen();
    void SetClose();

    bool? IsCleanableCondition();
    bool? IsChamberEmpty();
    bool? IsChamberGlowOn();
    bool? IsCleanProcStopped(bool bIsCleanEnd = false);
}

public interface IDryPump
{
    bool SetOn(bool on);
    bool IsOn(int? minOnMs = null);
    bool NeedOilChange();
}

public interface IBoosterPump
{
    bool IsUsed();
    void SetOn(bool on);
    bool IsOn(int? minOnMs = null);
}

public interface IVacuumValve
{
    bool SetOpen(bool open);
    bool IsOpen(int? minOnMs = null);
}

public interface IGaugeValve
{
    bool CheckCoditionGaugeValOpen(int currentStep);
    bool NeedCloseForPressure();
    void SetOpen(bool open);
    bool IsOpen(int? minOnMs = null);
    bool IsClosed(int? minOnMs = null);
    bool IsMaxPressure();
}

public interface IVacuumGauge
{
    double Torr();
    bool IsLevelOk(bool bSecond = false);
    bool IsGaugeError();
}

public interface ICimReporter
{
    void ReportProcessStarted(int ceid);
    void ReportVacuumStarted(int ceid);
    void ReportVacuumCompleted(int ceid);
    void ReportStateChange(int ceid);
    void ReportVacPurgeStarted(int ceid);
    void ReportProcessEnd(int ceid);
    void ReportPlasmaStarted(int ceid);
    void ReportPlasmaCompleted(int ceid);
}

public interface IGas
{
    bool SetGas(int channelIndex, bool on, double val = 0);
    bool IsGasOn(int channelGas);
    void AllOn();
    void AllOff();
    bool IsAllOn();
    bool IsLevelOk();
    double ReadGas(int channelIndex);
}

public interface IN2Purge
{
    bool IsOn(int? minOnMs = null);
    bool IsOff(int? minOnMs = null);
    bool SetOn();
    bool SetOff();
    int GetVentTimeMs();
}

public interface IAutoDoor
{
    bool IsEnabled();
    void Open();
    bool IsOpened(int? minOnMs = null);
}

public interface IAutoCooler
{
    bool IsUsed();
    void Start();
    bool IsDone();
}

public interface IJobCompleteSink
{
    bool IsJobCompleted { get; set; }
    void OnJobCompleted(long elapsedMs);
}

public interface IRFGenerator
{
    void OutputOn();
    void OutputOff();
    bool IsRemoteMode();
    void SetRemoteMode();
    bool IsPowerLevelOk();
    double GetForwardWatt();
    double GetReflectedWatt();
    void SetPowerWatt(int watt, bool immediate);
}