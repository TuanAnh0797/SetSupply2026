using System.Diagnostics;
using System.Text;
using VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;

namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Services;

public static class VXSeriesHelper
{
    public static bool ParseReadINGroup(in List<short> lstData, out VXSeriesINGroup result)
    {
        result = new();

        if (lstData.Count < 10) { return false; }
        result.InputType = (eVXSeriesInputType)lstData[0];
        result.Unit = (eVXSeriesUnit)lstData[1];
        result.RJC = (eVXSeriesStatus)lstData[7];
        result.BIAS = (float)lstData[9];
        //
        return true;
    }
    public static bool ParseReadSVGroup(in List<short> lstData, out VXSeriesSVGroup result)
    {
        result = new();
        if (lstData.Count < 7) { return false; }
        result.Number = (eVXSeriesSVNumber)lstData[0];
        result.HighLimit = (float)lstData[1];
        result.LowLimit = (float)lstData[2];
        result.Value1 = (float)lstData[3];
        result.Value2 = (float)lstData[4];
        result.Value3 = (float)lstData[5];
        result.Value4 = (float)lstData[6];
        //
        return true;
    }
    public static bool ParseMonitorData(in List<short> lstData, out VXSeriesMonitorResult result)
    {
        result = new();

        if (lstData.Count < 24) { return false; }
        result.CPV = (float)lstData[0];
        result.CSV = (float)lstData[1];
        result.TSV = (float)lstData[2];
        result.DPP = (int)lstData[3];
        result.Unit = (eVXSeriesUnit)lstData[4];
        result.SVNumber = (eVXSeriesSVNumber)lstData[9];
        result.Alarm1Status = (eVXSeriesStatus)lstData[20];
        result.Alarm2Status = (eVXSeriesStatus)lstData[21];
        result.Alarm3Status = (eVXSeriesStatus)lstData[22];
        result.Alarm4Status = (eVXSeriesStatus)lstData[23];
        //
        return true;
    }
    public static bool ParseALMGroup(in List<short> lstData, out VXSeriesALMGroup result)
    {
        result = new();
        if (lstData.Count < 16) { return false; }
        int count = 0;
        result.Alarm1.AlarmType = (eVXSeriesAlarmType)lstData[count++];
        result.Alarm1.AlarmSetValue = (float)lstData[count++];
        result.Alarm1.AlarmDeadBand = (float)lstData[count++];
        result.Alarm1.AlarmLS = (eVXSeriesLatchStatus)lstData[count++];
        //
        result.Alarm2.AlarmType = (eVXSeriesAlarmType)lstData[count++];
        result.Alarm2.AlarmSetValue = (float)lstData[count++];
        result.Alarm2.AlarmDeadBand = (float)lstData[count++];
        result.Alarm2.AlarmLS = (eVXSeriesLatchStatus)lstData[count++];
        //
        result.Alarm3.AlarmType = (eVXSeriesAlarmType)lstData[count++];
        result.Alarm3.AlarmSetValue = (float)lstData[count++];
        result.Alarm3.AlarmDeadBand = (float)lstData[count++];
        result.Alarm3.AlarmLS = (eVXSeriesLatchStatus)lstData[count++];
        //
        result.Alarm4.AlarmType = (eVXSeriesAlarmType)lstData[count++];
        result.Alarm4.AlarmSetValue = (float)lstData[count++];
        result.Alarm4.AlarmDeadBand = (float)lstData[count++];
        result.Alarm4.AlarmLS = (eVXSeriesLatchStatus)lstData[count++];
        //
        return true;

    }
    public static bool ParseSUBGroup(in List<short> lstData, out VXSeriesSUBGroup result)
    {
        result = new();
        if (lstData.Count < 2) { return false; }
        int count = 0;
        result.SUB1 = (eVXSeriesSUBType)lstData[count++];
        result.SUB2 = (eVXSeriesSUBType)lstData[count++];
        return true;
    }
}

