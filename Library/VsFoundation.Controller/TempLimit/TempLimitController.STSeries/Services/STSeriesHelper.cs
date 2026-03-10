using VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Services;

public static class STSeriesHelper
{

    #region Input
    public static bool ParseInputConfig(in List<short> lstData, out STSeriesInput result)
    {
        result = new();
        if (lstData.Count < 21) return false;
        result.InputType = (eSTSeriesInputType)lstData[0];//D0601
        result.InputUnit = (eSTSeriesInputUnit)lstData[1];//D0602
        result.InputRH1 = (int)(lstData[2] / 10f);//D0603
        result.InputRL1 = (int)(lstData[4] / 10f);//D0604
        result.InputBias = (int)(lstData[20] / 10f);//D0621
        return true;
    }

    #endregion
    #region Monitor

    public static bool ParseMonitorData(in List<short> lstData, out STSeriesMonitorResult result)
    {
        result = new();
        if (lstData.Count < 30) return false;
        result.NPV = lstData[0] / 10f;//D0001
        result.NSP = lstData[1] / 10f;//D0002
        result.TSP = lstData[2] / 10f;//D0003
        result.SetpointType = (eSTSeriesSetpointType)lstData[4];//D0005
        result.NowStatus.SetValue(lstData[9]);//D0010
        result.AlarmStatus.SetValue(lstData[13]);//D0014
        return true;
    }
    #endregion

    #region Alarm
    public static bool ParseVsAlarmConfig(in List<short> lstData, out STSeriesAlarm result)
    {
        result = new();
        if (lstData.Count < 47) return false;
        //AlarmType
        result.AlarmType.Alarm1 = (eSTSeriesAlarmType)lstData[0];// D0401
        result.AlarmType.Alarm2 = (eSTSeriesAlarmType)lstData[1];// D0402
        result.AlarmType.Alarm3 = (eSTSeriesAlarmType)lstData[2];// D0403
        result.AlarmType.Alarm4 = (eSTSeriesAlarmType)lstData[3];// D0404

        //AlarmValue
        result.AlarmValue.Alarm1 = (float)lstData[5] / 10f;//D0406
        result.AlarmValue.Alarm2 = (float)lstData[6] / 10f;//D0407
        result.AlarmValue.Alarm3 = (float)lstData[7] / 10f;//D0408
        result.AlarmValue.Alarm4 = (float)lstData[8] / 10f;//D0409

        //AlarmDeadBand
        result.AlarmDeadBand.Alarm1 = (float)lstData[10] / 10f;//D0411
        result.AlarmDeadBand.Alarm2 = (float)lstData[11] / 10f;//D0412
        result.AlarmDeadBand.Alarm3 = (float)lstData[12] / 10f;//D0413
        result.AlarmDeadBand.Alarm4 = (float)lstData[13] / 10f;//D0414

        //AlarmDelayTime
        result.AlarmDelayTime.Alarm1 = (float)lstData[15] / 10f;//D0416
        result.AlarmDelayTime.Alarm2 = (float)lstData[16] / 10f;//D0417
        result.AlarmDelayTime.Alarm3 = (float)lstData[17] / 10f;//D0418
        result.AlarmDelayTime.Alarm4 = (float)lstData[18] / 10f;//D0419

        //AlarmUpperLowerDeviation
        result.AlarmUpperLowerDeviation.Alarm1Upper = (float)lstData[20] / 10f;//D0421
        result.AlarmUpperLowerDeviation.Alarm2Upper = (float)lstData[21] / 10f;//D0422
        result.AlarmUpperLowerDeviation.Alarm3Upper = (float)lstData[22] / 10f;//D0423
        result.AlarmUpperLowerDeviation.Alarm4Upper = (float)lstData[23] / 10f;//D0424

        result.AlarmUpperLowerDeviation.Alarm1Lower = (float)lstData[25] / 10f;//D0426
        result.AlarmUpperLowerDeviation.Alarm2Lower = (float)lstData[26] / 10f;//D0427
        result.AlarmUpperLowerDeviation.Alarm3Lower = (float)lstData[27] / 10f;//D0428
        result.AlarmUpperLowerDeviation.Alarm4Lower = (float)lstData[28] / 10f;//D0429

        //AlarmHighLowDeviation
        result.AlarmHighLowDeviation.Alarm1High = (float)lstData[39] / 10f;//D0440
        result.AlarmHighLowDeviation.Alarm2High = (float)lstData[40] / 10f;//D0441
        result.AlarmHighLowDeviation.Alarm3High = (float)lstData[41] / 10f;//D0442
        result.AlarmHighLowDeviation.Alarm4High = (float)lstData[42] / 10f;//D0443

        result.AlarmHighLowDeviation.Alarm1Low = (float)lstData[44] / 10f;//D0445
        result.AlarmHighLowDeviation.Alarm2Low = (float)lstData[45] / 10f;//D0446
        result.AlarmHighLowDeviation.Alarm3Low = (float)lstData[46] / 10f;//D0447
        result.AlarmHighLowDeviation.Alarm4Low = (float)lstData[47] / 10f;//D0448

        //AlarmMode
        result.AlarmMode.Alarm1 = (eSTSeriesAlarmMode)lstData[53];//D0454
        result.AlarmMode.Alarm2 = (eSTSeriesAlarmMode)lstData[54];//D0455
        result.AlarmMode.Alarm3 = (eSTSeriesAlarmMode)lstData[55];//D0456
        result.AlarmMode.Alarm4 = (eSTSeriesAlarmMode)lstData[56];//D0457

        return true;
    }
    #endregion
    #region Setpoint group
    public static bool ParseGetSetPointConfig(in List<short> lstData, out STSeriesSetPoint result)
    {
        result = new();
        if (lstData.Count < 5) return false;
        result.SetpointType = (eSTSeriesSetpointType)lstData[0];//D0200
        result.Setpoint1 = (float)lstData[1] / 10f;//D0201
        result.Setpoint2 = (float)lstData[2] / 10f;//D0202
        result.Setpoint3 = (float)lstData[3] / 10f;//D0203
        result.Setpoint4 = (float)lstData[4] / 10f;//D0204
        return true;
    }
    public static bool ParseGetSetPointSlopeConfig(in List<short> lstData, out STSeriesSetPointSlope result)
    {
        result = new();
        if (lstData.Count < 2) return false;
        result.IncreasingSlope = (float)lstData[0] / 10f;//D0216
        result.DecreasingSlope = (float)lstData[1] / 10f;//D0217
        return true;
    }
    #endregion
    #region output config
    public static bool ParseGetOutputConfig(in List<short> lstData, out STSeriesOutput result)
    {
        result = new();
        if (lstData.Count < 11) return false;
        result.Output1AnalogType = (eSTSeriesAnalogOutputType)lstData[0];//D0624
        result.Output2AnalogType = (eSTSeriesAnalogOutputType)lstData[1];//D0625
        result.EVent1Type = (eSTSeriesEventType)lstData[3];//D0627
        result.EVent2Type = (eSTSeriesEventType)lstData[4];//D0628
        result.EVent3Type = (eSTSeriesEventType)lstData[5];//D0629
        result.EVent4Type = (eSTSeriesEventType)lstData[6];//D0630

        result.OutputHeat1Type = (eSTSeriesOutputType)lstData[7];//D0631
        result.OutputHeat2Type = (eSTSeriesOutputType)lstData[8];//D0632
        result.OutputCool1Type = (eSTSeriesOutputType)lstData[9];//D0633
        result.OutputCool2Type = (eSTSeriesOutputType)lstData[10];//D0634

        return true;
    }
    #endregion
    #region Timer config
    public static bool ParseGetTimerConfig(in List<short> lstData, out STSeriesTimerConfig result)
    {
        result = new();
        if (lstData.Count < 8) return false;
        result.Timer1.TimerSource = (eSTSeriesTimerSource)lstData[0];//D0311
        result.Timer1.TimerSourceType = (eSTSeriesTimerSourceType)lstData[1];//D0312
        result.Timer1.Time1 = (float)lstData[2] / 10f;//D0313
        result.Timer1.Time2 = (float)lstData[3] / 10f;//D0314
                                                      //
        result.Timer2.TimerSource = (eSTSeriesTimerSource)lstData[4];//D0315
        result.Timer2.TimerSourceType = (eSTSeriesTimerSourceType)lstData[5];//D0316
        result.Timer2.Time1 = (float)lstData[6] / 10f;//D0317
        result.Timer2.Time2 = (float)lstData[7] / 10f;//D0318

        return true;
    }
    #endregion
}
