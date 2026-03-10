using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonModel;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;

public interface ITempLimit
{
    bool IsOpen { get; }
    Task<bool> Open();
    Task Close();
    Task<TempLimitMonitorResult?> MonitorData(byte slaveID);
    Task<bool> SetAlarm(byte slaveID, List<TempLimitAlarm> alarms);
    Task<bool> SetSVValue(byte slaveID, float sv);
    Task<bool> SetInputType(byte slaveID, eTempLimitInputType inputType);
    Task<bool> SetInputUnit(byte slaveID, eTempLimitUnit unit);
    Task<bool> SetInputBias(byte slaveID, float bias);
    Task<bool> Start(byte slaveID);
    Task<bool> Stop(byte slaveID);

    /// <summary>
    /// Only using M74 Series
    /// </summary>
    /// <param name="channel"></param>
    void SetChanel(eTempLimitChannel channel);
}
