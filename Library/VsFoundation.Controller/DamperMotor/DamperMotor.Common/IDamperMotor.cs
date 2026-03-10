using VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Models;

namespace VsFoundation.Controller.DamperMotor.DamperMotor.Common;

public interface IDamperMotor
{
    Task<bool> Connect();
    Task DisConnect(int axisCount);
    Task<(bool, string)> SetStopCurrent(int axisCount);
    Task<(EziMotionMonitor, string)> Monitor(byte slaveNo);
    Task<(bool, string)> EnableMotor(byte slaveNo, bool isEnable);
    Task<(bool, string)> MotorMoveStop(byte slaveNo);
    Task<(bool, string)> AlarmClear(byte slaveNo);
    Task<(bool, string)> MotorHoming(byte slaveNo);
    Task<(bool, string)> IsHomePosition(byte slaveNo);
    Task<(bool, string)> IsMotorMoveDone(byte slaveNo);
    Task<(bool, string)> MotorOpen(byte slaveNo, int percent);
    Task<(bool, string)> MotorClose(byte slaveNo);
}
