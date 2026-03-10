using VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Models;
using static VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Models.EziMOTIONPlusRLib;

namespace VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Services;

public class EziMotionPlusRClient
{
    public EziMotionPlusRClient(EziConfig _eziConfig)
    {
        eziConfig = _eziConfig;
    }

    #region Properties
    public EziConfig eziConfig { get; set; } = new EziConfig();

    #endregion

    public async Task<bool> Connect()
    {
        var result = FAS_Connect(eziConfig.PortNo, eziConfig.BaudRate) != 0;
        return await Task.FromResult(result);
    }
    public async Task DisConnect(int axisCount)
    {
        bool ret = false;
        try
        {
            for (int i = 1; i <= axisCount; i++)
            {
                ret = await IsSlaveExist((byte)i);
                if (!ret) { continue; }
                ret = await ServoEnable((byte)i, false);
            }
            await Close();
        }
        catch { }
    }
    public async Task<(bool, string)> SetStopCurrent(int axisCount)
    {
        string error = string.Empty;
        int paramVal;
        bool ret = false;
        try
        {
            for (int i = 1; i <= axisCount; i++)
            {
                ret = await IsSlaveExist((byte)i);
                if (!ret) { error = $"Error Axis {i} : Not Existed"; break; }

                ret = await GetParameter((byte)i, eziConfig.StepConfig.StopCurrent.StepType, out paramVal);
                if (!ret) { error = $"Error Axis {i}: Get Parameter"; break; }

                if (paramVal != eziConfig.StepConfig.StopCurrent.Value) //50
                {
                    ret = await SetParameter(eziConfig.StepConfig.StopCurrent, (byte)i);
                    if (!ret) { error = $"Error Axis {i}: Set Parameter {eziConfig.StepConfig.StopCurrent.Value}"; break; }
                }
                ret = await SaveAllParameter((byte)i);
            }
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }
    public async Task<(EziMotionMonitor, string)> Monitor(byte slaveNo)
    {
        string error = string.Empty;
        EziMotionMonitor motionMonitor = new();
        bool ret = false;
        uint alarmCode = 0;
        try
        {

            ret = await IsSlaveExist(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Not Existed"; return (motionMonitor, error); }

            ret = await GetAxisStatus(slaveNo, ref alarmCode);
            motionMonitor.AlarmCode.Value = alarmCode;
            if (!ret) { error = $"Error Slave {slaveNo} : Axis Status {alarmCode}"; return (motionMonitor, error); }

            ret = await GetMotionStatus(slaveNo, ref motionMonitor);
            if (!ret) { error = $"Error Slave {slaveNo} : Monitor"; }
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (motionMonitor, error);
    }

    public async Task<(bool, string)> EnableMotor(byte slaveNo, bool isEnable)
    {
        string error = string.Empty;
        uint alarmCode = 0;
        bool ret = false;
        try
        {
            ret = await IsSlaveExist(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Not Existed"; return (ret, error); }

            if (!isEnable)
            {
                ret = await ServoEnable(slaveNo, isEnable);
                if (!ret) { error = $"Error Slave {slaveNo} : Disable Motor"; }
                return (ret, error);
            }

            //-----
            EziAxisStatus axisStatus = new();
            axisStatus.Value = alarmCode;
            ret = await GetAxisStatus(slaveNo, ref alarmCode);
            if (!ret) { error = $"Error Slave {slaveNo} : Axis Status {alarmCode}"; return (ret, error); }

            if (axisStatus.FFLAG_ERRORALL || axisStatus.FFLAG_ERROVERCURRENT || axisStatus.FFLAG_ERRSTEPALARM)
            {
                ret = await StepAlarmReset(slaveNo, true);
                await Task.Delay(500);
                ret = await StepAlarmReset(slaveNo, false);
            }

            ret = await ServoEnable(slaveNo, isEnable);
            if (!ret) { error = $"Error Slave {slaveNo} : Enable Motor"; }
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }

    public async Task<(bool, string)> MotorMoveStop(byte slaveNo)
    {
        string error = string.Empty;
        bool ret = false;
        try
        {
            ret = await IsSlaveExist(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Not Existed"; return (ret, error); }

            ret = await MoveStop(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Move Stop"; return (ret, error); }
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }

    public async Task<(bool, string)> AlarmClear(byte slaveNo)
    {
        string error = string.Empty;
        bool ret = false;
        try
        {
            ret = await IsSlaveExist(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Not Existed"; return (ret, error); }

            await StepAlarmReset(slaveNo, true);
            await Task.Delay(500);
            await StepAlarmReset(slaveNo, false);
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }

    public async Task<(bool, string)> MotorHoming(byte slaveNo)
    {
        string error = string.Empty;
        bool ret = false;
        int paramVal = 0;

        try
        {
            (ret, error) = await AlarmClear(slaveNo);
            if (!ret) { return (ret, error); }

            ret = await ServoEnable(slaveNo, true);
            if (!ret) { error = $"Error Slave {slaveNo} : Enable Motor"; return (ret, error); }

            foreach (var tmp in eziConfig.StepConfig.ListHomingSteps)
            {
                ret = await GetParameter(slaveNo, tmp.StepType, out paramVal);
                if (!ret) { error = $"Error Axis {slaveNo}: Get Parameter {tmp.StepType.ToString()}"; break; }

                if (paramVal != tmp.Value)
                {
                    ret = await SetParameter(tmp, slaveNo);
                    if (!ret) { error = $"Error Axis {slaveNo}: Set Parameter {tmp.StepType.ToString()} : {tmp.Value}"; break; }
                }
            }

            ret = await MoveOriginSingleAxis(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Move Origin Single Axis"; return (ret, error); }

        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }

    public async Task<(bool, string)> IsHomePosition(byte slaveNo)
    {
        string error = string.Empty;
        EziMotionMonitor motionMonitor = new();
        bool ret = false;
        uint alarmCode = 0;
        try
        {
            ret = await IsSlaveExist(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Not Existed"; return (ret, error); }

            ret = await GetAxisStatus(slaveNo, ref alarmCode);
            motionMonitor.AlarmCode.Value = alarmCode;
            if (!ret) { error = $"Error Slave {slaveNo} : Axis Status {alarmCode}"; return (ret, error); }

            ret = (!motionMonitor.AlarmCode.FFLAG_ORIGINRETURNING || motionMonitor.AlarmCode.FFLAG_ORIGINSENSOR);

        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }


    public async Task<(bool, string)> IsMotorMoveDone(byte slaveNo)
    {
        string error = string.Empty;
        EziMotionMonitor motionMonitor = new();
        bool ret = false;
        uint alarmCode = 0;
        try
        {
            ret = await IsSlaveExist(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Not Existed"; return (ret, error); }

            ret = await GetAxisStatus(slaveNo, ref alarmCode);
            motionMonitor.AlarmCode.Value = alarmCode;
            if (!ret) { error = $"Error Slave {slaveNo} : Axis Status {alarmCode}"; return (ret, error); }

            ret = !motionMonitor.AlarmCode.FFLAG_MOTIONING;
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }
    public async Task<(bool, string)> MotorOpen(byte slaveNo, int percent)
    {
        string error = string.Empty;
        bool ret = false;
        try
        {
            int step = eziConfig.RevolutionCounter / 4;
            step = step * percent;
            step = step / 100;
            (ret, error) = await MotorMoveStep(slaveNo, step);
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }
    public async Task<(bool, string)> MotorClose(byte slaveNo)
    {
        string error = string.Empty;
        bool ret = false;
        try
        {
            (ret, error) = await MotorMoveStep(slaveNo, 0);
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }
    #region private

    private async Task<(bool, string)> MotorMoveStep(byte slaveNo, int abPosition)
    {
        string error = string.Empty;
        EziMotionMonitor motionMonitor = new();
        bool ret = false;
        uint alarmCode = 0;
        try
        {
            ret = await IsSlaveExist(slaveNo);
            if (!ret) { error = $"Error Slave {slaveNo} : Not Existed"; return (ret, error); }

            ret = await GetAxisStatus(slaveNo, ref alarmCode);
            motionMonitor.AlarmCode.Value = alarmCode;
            if (!ret) { error = $"Error Slave {slaveNo} : Axis Status {alarmCode}"; return (ret, error); }

            if (motionMonitor.AlarmCode.FFLAG_ALARMRESET)
            {
                ret = false;
                await MotorHoming(slaveNo);
                error = $"Homing...";
                return (ret, error);
            }
            // I/O Not Setting.
            uint ioInput = 0;
            ret = await GetIOInput(slaveNo, ref ioInput);
            if (!ret) { error = $"Error Slave {slaveNo} : Get IO Input"; return (ret, error); }

            if ((ioInput & ((uint)eStepInputBitMask.STOP | (uint)eStepInputBitMask.PAUSE | (uint)eStepInputBitMask.ESTOP)) == 0)
            {
                ret = await SetIOInput(eziConfig.PortNo, ((uint)eStepInputBitMask.STOP | (uint)eStepInputBitMask.PAUSE | (uint)eStepInputBitMask.ESTOP));
                if (!ret) { error = $"Error Slave {slaveNo} : Set IO Input"; return (ret, error); }
            }
            ret = await MoveSingleAxisAbsPos(slaveNo, abPosition);
            if (!ret) { error = $"Error Slave {slaveNo} : Move Single Axis Absolute Position"; return (ret, error); }
        }
        catch (Exception e) { ret = false; error = e.Message; }
        return (ret, error);
    }


    private Task<bool> CheckDriveInfo(byte slaveNo, out byte type, out string version)
    {
        type = 0; version = "";
        var result = FAS_GetSlaveInfo(eziConfig.PortNo, slaveNo, ref type, ref version) != 0;
        return Task.FromResult(result);
    }
    private Task<bool> GetParameter(byte slaveNo, eEziParamStepType paramType, out int paramVal)
    {
        paramVal = 0;
        var result = FAS_GetParameter(eziConfig.PortNo, slaveNo, (byte)paramType, ref paramVal) == FMM_OK;
        return Task.FromResult(result);
    }

    private Task<bool> SetParameter(EziParamSteps eziParamSteps, byte slaveNo)
    {
        var result = FAS_SetParameter(eziConfig.PortNo, slaveNo, (byte)eziParamSteps.StepType, eziParamSteps.Value) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> SaveAllParameter(byte slaveNo)
    {
        var result = FAS_SaveAllParameters(eziConfig.PortNo, slaveNo) == FMM_OK;
        return Task.FromResult(result);
    }


    private Task<bool> IsSlaveExist(byte slaveNo)
    {
        var result = FAS_IsSlaveExist(eziConfig.PortNo, slaveNo) != 0;
        return Task.FromResult(result);
    }
    private Task<bool> ServoEnable(byte slaveNo, bool isOn)
    {
        var result = FAS_ServoEnable(eziConfig.PortNo, slaveNo, isOn ? 1 : 0) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> StepAlarmReset(byte slaveNo, bool isReset)
    {
        var result = FAS_StepAlarmReset(eziConfig.PortNo, slaveNo, isReset ? 1 : 0) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> GetAxisStatus(byte slaveNo, ref uint alarmCode)
    {
        var result = FAS_GetAxisStatus(eziConfig.PortNo, slaveNo, ref alarmCode) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> GetMotionStatus(byte slaveNo, ref EziMotionMonitor motionMonitor)
    {
        int cmdPos = 0, actPos = 0, posErr = 0, actVel = 0;
        ushort posItemNo = 0;
        var result = FAS_GetMotionStatus(eziConfig.PortNo, slaveNo, ref cmdPos, ref actPos, ref posErr, ref actVel, ref posItemNo) == FMM_OK;
        motionMonitor = new EziMotionMonitor() { ActualPosition = actPos, ActualVelocity = actVel, CommandPosition = cmdPos, PositionError = posErr, PositionTableItemNo = posItemNo };
        return Task.FromResult(result);
    }
    private Task<bool> MoveOriginSingleAxis(byte slaveNo)
    {
        var result = FAS_MoveOriginSingleAxis(eziConfig.PortNo, slaveNo) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> GetIOInput(byte slaveNo, ref uint ioInput)
    {
        ioInput = 0;
        var result = FAS_GetIOInput(eziConfig.PortNo, slaveNo, ref ioInput) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> SetIOInput(byte slaveNo, uint iOCLRMask)
    {
        uint iOSETMask = 0;
        var result = FAS_SetIOInput(eziConfig.PortNo, slaveNo, iOSETMask, iOCLRMask) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> MoveSingleAxisAbsPos(byte slaveNo, int absPosition, uint velocity = 10000)
    {
        var result = FAS_MoveSingleAxisAbsPos(eziConfig.PortNo, slaveNo, absPosition, velocity) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task<bool> MoveStop(byte slaveNo)
    {
        var result = FAS_MoveStop(eziConfig.PortNo, slaveNo) == FMM_OK;
        return Task.FromResult(result);
    }
    private Task Close()
    {
        FAS_Close(eziConfig.PortNo);
        return Task.CompletedTask;
    }
    #endregion
}
