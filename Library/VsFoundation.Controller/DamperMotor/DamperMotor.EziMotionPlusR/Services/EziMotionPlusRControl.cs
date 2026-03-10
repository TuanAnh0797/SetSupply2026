
using VsFoundation.Controller.DamperMotor.DamperMotor.Common;
using VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Models;

namespace VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Services;

public class EziMotionPlusRControl:IDamperMotor
{
    public EziMotionPlusRClient client;
    public EziMotionPlusRControl(EziConfig eziConfig)
    {
        client = new EziMotionPlusRClient(eziConfig);
    }
    public async Task<bool> Connect()
    {
        return await client.Connect();
    }
    public async Task DisConnect(int axisCount)
    {
        await client.DisConnect(axisCount);
    }
    public async Task<(bool, string)> SetStopCurrent(int axisCount)
    {
        return await client.SetStopCurrent(axisCount);
    }
    public async Task<(EziMotionMonitor, string)> Monitor(byte slaveNo)
    {
        return await client.Monitor(slaveNo);
    }
    public async Task<(bool, string)> EnableMotor(byte slaveNo, bool isEnable)
    {
        return await client.EnableMotor(slaveNo, isEnable);
    }
    public async Task<(bool, string)> MotorMoveStop(byte slaveNo)
    {
        return await client.MotorMoveStop(slaveNo);
    }
    public async Task<(bool, string)> AlarmClear(byte slaveNo)
    {
        return await client.AlarmClear(slaveNo);
    }
    public async Task<(bool, string)> MotorHoming(byte slaveNo)
    {
        return await client.MotorHoming(slaveNo);
    }
    public async Task<(bool, string)> IsHomePosition(byte slaveNo)
    {
        return await client.IsHomePosition(slaveNo);
    }
    public async Task<(bool, string)> IsMotorMoveDone(byte slaveNo)
    {
        return await client.IsMotorMoveDone(slaveNo);
    }
    public async Task<(bool, string)> MotorClose(byte slaveNo)
    {
        return await client.MotorClose(slaveNo);
    }
    public async Task<(bool, string)> MotorOpen(byte slaveNo, int percent)
    {
        return await client.MotorOpen(slaveNo, percent);
    }
}
