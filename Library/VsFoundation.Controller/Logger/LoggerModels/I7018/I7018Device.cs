using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.Enum;
using VsFoundation.Controller.Logger.Interface;
using VsFoundation.Controller.Logger.LoggerModels.I7018.Models;
using VsFoundation.Controller.Logger.LoggerModels.Interfacce;

namespace VsFoundation.Controller.Logger.LoggerModels.I7018;

public class I7018Device: IDeviceLoggerTemperatureController
{
    public string Address { get => _address; set => _address = value; }
    private string _address = "01";
    public I7018Device(int slaveid)
    {
        Address = slaveid.ToString("D2");
    }
    public byte[] SetConfigurationModule(IConfigurationLoggerTemperatureController param)
    {
        string strcmd = "%" + Address + param.DeviceSetting.NewAddress + ((byte)param.DeviceSetting.InputType).ToString("X2") + ((byte)param.DeviceSetting.Baudrate).ToString("X2") + "00\r";
        return Encoding.ASCII.GetBytes(strcmd);
    }
    public IResultLoggerTemperatureController ParsesSetConfigurationModule(byte[] dataParse, IConfigurationLoggerTemperatureController param)
    {
        CheckEndOfCommand(dataParse);
        var dataresponse = Encoding.ASCII.GetString(dataParse.ToArray()).TrimEnd('\r');
        bool sendOK = false;
        if (dataresponse.Contains("!"))
        {
            string newaddress = dataresponse.TrimStart('!');
            sendOK = true;
            Address = newaddress;
        }
        I7018Result result = new I7018Result();
        result.StringResponse = dataresponse;
        result.SendOK = sendOK;
        return result;
    }
    public byte[] GetConfigurationModule()
    {
        string strcmd = "$" + Address + "2\r";
        return Encoding.ASCII.GetBytes(strcmd);
    }
    public IResultLoggerTemperatureController ParsesGetConfigurationModule(byte[] dataParse)
    {
        CheckEndOfCommand(dataParse);
        string dataresponse = Encoding.ASCII.GetString(dataParse.ToArray()).TrimEnd('\r');
        if (dataresponse.Length != 9) throw new Exception("Get Information Configuration Fail");
        string inputtype = dataresponse.Substring(3, 2);
        string baudrate = dataresponse.Substring(5, 2);
        byte valueinputtype = Convert.ToByte(inputtype, 16);
        byte valuebaudrate = Convert.ToByte(baudrate, 16);
        I7018Result result = new I7018Result();
        result.DeviceSetting.InputType = (eAnalogInputType)valueinputtype;
        result.DeviceSetting.Baudrate = (ebaudrate)valuebaudrate;
        result.StringResponse = dataresponse;
        result.SendOK = true;
        return result;
    }

    public byte[] MonitorAllChanel()
    {
        string strcmd = "#" + Address + "\r";
        return Encoding.ASCII.GetBytes(strcmd);
    }

    public IResultLoggerTemperatureController ParsesMonitorAllChanel(byte[] dataParse)
    {
        CheckEndOfCommand(dataParse);
        string dataresponse = Encoding.ASCII.GetString(dataParse.ToArray()).Trim('>').TrimEnd('\r');
        List<double> values = new List<double>();
        var matches = Regex.Matches(dataresponse, @"[+-]?\d+(\.\d+)?");
        foreach (Match match in matches)
        {
            values.Add(double.Parse(match.Value, CultureInfo.InvariantCulture));
        }
        I7018Result result = new I7018Result();
        result.DataChanel.SetData(values.ToArray());
        result.StringResponse = dataresponse;
        result.SendOK = true;
        return result;
    }

    public byte[] SetEnableDisableChanel(IConfigurationLoggerTemperatureController param)
    {
        string strcmd = "$" + Address + "5" + param.EnableChanel.ToHexValue() + "\r";
        return Encoding.ASCII.GetBytes(strcmd);
    }
    public IResultLoggerTemperatureController ParsesSetEnableDisableChanel(byte[] dataParse)
    {
        CheckEndOfCommand(dataParse);
        var dataresponse = Encoding.ASCII.GetString(dataParse.ToArray()).TrimEnd('\r');
        bool sendOK = false;
        if (dataresponse.Contains("!"))
        {
            sendOK = true;
        }
        else
        {
            throw new Exception("Set Enable Disable Chanel Fail");
        }
        I7018Result result = new I7018Result();
        result.StringResponse = dataresponse;
        result.SendOK = sendOK;
        return result;
    }

    public byte[] GetEnableDisableChanel()
    {
        string strcmd = "$" + Address + "6\r";
        return Encoding.ASCII.GetBytes(strcmd);
    }

    public IResultLoggerTemperatureController ParsesGetEnableDisableChanel(byte[] dataParse)
    {
        CheckEndOfCommand(dataParse);
        string dataresponse = Encoding.ASCII.GetString(dataParse.ToArray()).TrimEnd('\r');
        if (dataresponse.Length != 5) throw new Exception("Get Information Chanel Fail");
        string DataEnablechanel = dataresponse.Substring(3, 2);
        I7018Result result = new I7018Result();
        result.EnableChanel = new EnableChanel().HexTo8Bits(DataEnablechanel);
        result.StringResponse = dataresponse;
        result.SendOK = true;
        return result;
    }
    public byte[] MonitorSingleChanel(IConfigurationLoggerTemperatureController param)
    {
        string strcmd = "#" + Address + param.SingleChanelMonitor + "\r";
        return Encoding.ASCII.GetBytes(strcmd);
    }
    public IResultLoggerTemperatureController ParsesMonitorSingleChanel(byte[] dataParse, IConfigurationLoggerTemperatureController param)
    {
        CheckEndOfCommand(dataParse);
        string dataresponse = Encoding.ASCII.GetString(dataParse.ToArray()).Trim('>').TrimEnd('\r');
        var temp =  double.Parse(dataresponse, CultureInfo.InvariantCulture);
        I7018Result result = new I7018Result();
        result.MonitorSingleChanelData = new KeyValuePair<string, double>(param.SingleChanelMonitor,temp);
        result.StringResponse = dataresponse;
        result.SendOK = true;
        return result;
    }
    public byte[] SynchronizedSampling()
    {
        string strcmd = "#**\r";
        return Encoding.ASCII.GetBytes(strcmd);
    }
    private bool CheckEndOfCommand(byte[] dataParse)
    {
        if (dataParse[dataParse.Length - 1] == 13)
        {
            return true;
        }
        throw new Exception("CheckEndOfCommand Fail");
    }
}
