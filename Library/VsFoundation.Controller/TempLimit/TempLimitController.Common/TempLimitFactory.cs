using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Services;
using VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Services;
using VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Services;
using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common;

public static class TempLimitFactory
{
    public static ITempLimit Create(eTempLimitType tempLimitType, ICommunication connection, CancellationToken _cancellationToken, eTempLimitProtocolType protocolType = eTempLimitProtocolType.ModBus, int timeoutMs = 3000, int retryCount = 3, eTempLimitChannel channel = eTempLimitChannel.CH1)
    {
        return tempLimitType switch
        {
            eTempLimitType.STSeries => new STSeriesControl(connection, _cancellationToken, protocolType, timeoutMs, retryCount, channel),
            eTempLimitType.M74Series => new M74SeriesControl(connection, _cancellationToken, protocolType, timeoutMs, retryCount,channel),
            eTempLimitType.VXSeries => new VXSeriesControl(connection, _cancellationToken, protocolType, timeoutMs, retryCount, channel),
            _ => throw new NotSupportedException($"TempLimitType '{tempLimitType}' is not supported.")
        };
    }
}
