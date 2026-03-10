using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.MainTemp.Enum;
using VsFoundation.Controller.MainTemp.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.SPSeries;
using VsFoundation.Controller.MainTemp.MainTempModels.UP550;
using VsFoundation.Controller.MainTemp.MainTempModels.UP55A;


namespace VsFoundation.Controller.MainTemp.Common;

public static class DeviceFactoryMainTempController
{
    public static IDeviceMainTemperatureController Create(eMainTemperatureControllerModel type, IProtocol protocol, string deviceId)
    {
        return type switch
        {
            eMainTemperatureControllerModel.UP55A =>  new UP55ADevice(protocol, deviceId),
            eMainTemperatureControllerModel.UP550=> new UP550Device(protocol, deviceId),
            eMainTemperatureControllerModel.SPSeries => new SPSeriesDevice(protocol, deviceId),
            _ => throw new ArgumentException("Invalid device type", nameof(type)),
        };
    }
}
