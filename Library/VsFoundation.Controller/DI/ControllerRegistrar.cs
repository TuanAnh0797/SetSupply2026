using VsFoundation.Base.DI;
using VsFoundation.Base.Helper.Utils;
using VSLibrary.Common.MVVM.Core;
using VSLibrary.Controller;
using VSLibrary.UIComponent.Localization;

namespace VsFoundation.Controller.DI;

public static class ControllerRegistrar
{
    public static void RegisterController(List<ControllerType> controllerList, List<IIOSettinglist> iODataList, List<IAxisSettinglist> axisList)
    {
        var controllerManager = new ControllerManager(VSContainer.Instance, controllerList);
        VSContainer.Instance.RegisterInstance(controllerManager);

        #region Setup DIO, AXIS List, Range for AIO
        controllerManager.SetIOlist(iODataList);
        controllerManager.SetAxislist(axisList);

        controllerManager.AxtAIOCtrl?.SetAllAioConfig(0, true);
        #endregion

        #region Show warning: When controller disconnected
        var controllers = new List<(bool IsConnected, string Message)>();

        if (controllerManager.AxtDIOCtrl != null)
            controllers.Add((controllerManager.AxtDIOCtrl.IsConnected,
                VsLocalization.Get("DIOControllerIsNotConnected")));

        if (controllerManager.AxtAIOCtrl != null)
            controllers.Add((controllerManager.AxtAIOCtrl.IsConnected,
                VsLocalization.Get("AIOControllerIsNotConnected")));

        if (controllerManager.ADLinkAIOCtrl != null)
            controllers.Add((controllerManager.ADLinkAIOCtrl.IsConnected,
                VsLocalization.Get("AIControllerIsNotConnected")));

        if (controllerManager.AxtMotionCtrl != null)
            controllers.Add((controllerManager.AxtMotionCtrl.IsConnected,
                VsLocalization.Get("MotionControllerIsNotConnected")));

        var controllerArray = controllers.ToArray();

        var messages = controllers
            .Where(c => !c.IsConnected)
            .Select(c => c.Message)
            .ToList();

        if (messages.Any())
        {
            var fullMessage = string.Join(Environment.NewLine, messages);
            VSMessage.ShowMessageAsync(fullMessage);
        }
        #endregion
    }
}
