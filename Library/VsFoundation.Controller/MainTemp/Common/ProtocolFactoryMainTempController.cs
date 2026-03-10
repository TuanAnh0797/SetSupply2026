using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.Common.Protocol.Serial.Common;
using VsFoundation.Controller.Common.Protocol.Serial.MainTempController;
using VsFoundation.Controller.MainTemp.Enum;

namespace VsFoundation.Controller.MainTemp.Common;

public static class ProtocolFactoryMainTempController
{
    public static IProtocol Create(eMainTemperatureControllerModel type, eProtocolTypeMainTempController protocoltype)
    {
        switch (type)
        {
            case eMainTemperatureControllerModel.UP55A:
                if (protocoltype == eProtocolTypeMainTempController.PCLink)
                {
                    return new PCLinkSUMUPSeries();
                }
                else
                {
                    return new ModBusRTU();
                }
            case eMainTemperatureControllerModel.UP550:
                if (protocoltype == eProtocolTypeMainTempController.PCLink)
                {
                    return new PCLinkSUMUPSeries();
                }
                else
                {
                    return new ModBusRTU();
                }
            case eMainTemperatureControllerModel.SPSeries:
                if (protocoltype == eProtocolTypeMainTempController.PCLink)
                {
                    return new PCLinkSUMSPSeries();
                }
                else
                {
                    return new ModBusRTU();
                }
            default:
                 throw new ArgumentException("Invalid device type", nameof(type));
        }
            
    }
}
