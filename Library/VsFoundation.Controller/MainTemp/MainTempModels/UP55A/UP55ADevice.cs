
using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.MainTemp.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;
using VsFoundation.Controller.MainTemp.MainTempModels.UP55A.Models;

namespace VsFoundation.Controller.MainTemp.MainTempModels.UP55A;

public class UP55ADevice : IDeviceMainTemperatureController
{
    private IProtocol _protocol;
    public byte SlaveID { get => _slaveID; set => _slaveID = value; }
    public IProtocol Protocol { get => _protocol; set => _protocol = value; }

    private byte _slaveID = (byte)(1 & 0xFF);
    public UP55ADevice(IProtocol protocol, string slaveid)
    {
        Protocol = protocol;
        SlaveID = (byte)ushort.Parse(slaveid);
    }
    #region Monitor
    public byte[] MonitorData()
    {
        ushort monitorAdd = 2001 - 1; //D2001
        ushort TotalRegister = 44;
        return Protocol.CreateRead(SlaveID, monitorAdd, TotalRegister);
    }
    public IResultMainTemperatureController ParseMonitorData(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 44) throw new Exception("Not enough data");
        MonitorResult uP55AMonitorResult = new MonitorResult();
        uP55AMonitorResult.ADConverterErrorStatus.SetValue(lstData[0]);//2001
        uP55AMonitorResult.PV_L1 = lstData[2] / 10f;//2003
        uP55AMonitorResult.CSP_L1 = lstData[3] / 10f;//2004
        uP55AMonitorResult.OUT_L1 = lstData[4] / 10f;//2005
        uP55AMonitorResult.CurrentPatternNo = lstData[14];//2015
        uP55AMonitorResult.CurrentSegmentNo = lstData[15];//2016
        uP55AMonitorResult.RemainingSegmentTime = new TimeSpan(0, lstData[16], 0);//2017
        uP55AMonitorResult.PVEventStatus.SetValue(lstData[38]);//2039
        uP55AMonitorResult.TimeEventStatus.SetValue(lstData[39], lstData[40]);//40,41
        uP55AMonitorResult.SegmentInSelectedPattern = lstData[43];//2044
    

        UP55AResult uP55AResult = new UP55AResult();
        uP55AResult.MonitorResult = uP55AMonitorResult;
        return uP55AResult;
    }
    #endregion
    #region Operation
    public byte[] SetMode(IConfigurationMainTemperatureController param)
    {
        ushort Address = 2316 - 1;
        return Protocol.CreateWrite(SlaveID, Address, (short)param.Mode);
    }

    public byte[] GetMode()
    {
        ushort Address = 2316 - 1;
        return Protocol.CreateRead(SlaveID, Address, 1);
    }
    public IResultMainTemperatureController ParsesGetMode(byte[] dataParse)
    {
        short Mode = Protocol.ParsesReadSingleRegister(dataParse);
        if (!System.Enum.IsDefined(typeof(eMainTempMode), (int)Mode)) throw new ArgumentOutOfRangeException($"Invalid Time Mode value: {Mode}");
        eMainTempMode rs = (eMainTempMode)Mode;
        UP55AResult uP55AResult = new UP55AResult();
        uP55AResult.Mode = rs;
        uP55AResult.ParsesOK = true;
        return uP55AResult;
    }
    public byte[] SelectPattern(IConfigurationMainTemperatureController param)
    {
        CheckPatternID(param);
        ushort Address = 2322 - 1;
        return Protocol.CreateWrite(SlaveID, Address, param.PatternID);
    }
    public byte[] SetHold()
    {
        ushort Address = 2317 - 1;
        return Protocol.CreateWrite(SlaveID, Address, 1);
    }
    public IResultMainTemperatureController ParsesGetHold(byte[] dataParse)
    {
        short Mode = Protocol.ParsesReadSingleRegister(dataParse);
        UP55AResult uP55AResult = new UP55AResult();
        uP55AResult.IsHold = Mode == 1;
        uP55AResult.ParsesOK = true;
        return uP55AResult;
    }
    public byte[] SetResume()
    {
        ushort Address = 2317 - 1;
        return Protocol.CreateWrite(SlaveID, Address, 0);
    }
    public byte[] GetHold()
    {
        ushort Address = 2317 - 1;
        return Protocol.CreateRead(SlaveID, Address, 1);
    }
    #endregion
    #region Pattern Setting
    private byte[] SetPattern_SelectClearPattern(IConfigurationMainTemperatureController param)
    {
        CheckPatternID(param);
        ushort address = 8991 - 1;
        return Protocol.CreateWrite(SlaveID, address, param.PatternID);
    }
    private byte[] SetPattern_ClearPattern()
    {
        ushort address = 8992 - 1;
        return Protocol.CreateWrite(SlaveID, address, 1);
    }
    private byte[] GetPattern_ErrorClearPattern()
    {
        ushort address = 8993 - 1;
        return Protocol.CreateRead(SlaveID, address, 1);
    }
    private byte[] SetPattern_SelectPattern(IConfigurationMainTemperatureController param)
    {
        CheckPatternID(param);
        ushort patternNumberAdd = 8001 - 1;
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, param.PatternID);
    }
    private byte[] SetTagetSetpoint(IConfigurationMainTemperatureController param)
    {
        ushort Address = 2201 - 1;
        return Protocol.CreateWrite(SlaveID, Address, (short)(param.SetpointLocalMode * 10));
    }
    private byte[] SetPattern_ValueToReadWrite()
    {
        ushort segmentNumberAdd = 8002 - 1;
        return Protocol.CreateWrite(SlaveID, segmentNumberAdd, 0);
    }
    private byte[] GetPattern_ErrorReadWrite()
    {
        ushort errorInforAdd = 8042 - 1;
        return Protocol.CreateRead(SlaveID, errorInforAdd, 1);
    }
    private byte[] GetPattern_Config()
    {
        ushort patternDataAdd = 8003 - 1;
        return Protocol.CreateRead(SlaveID, patternDataAdd, 6);
    }
    private byte[] SetPattern_Config(IConfigurationMainTemperatureController param)
    {
        short[] arraydataconfig = param.Pattern.ToListByte().ToArray();
        ushort patternDataAdd = 8003 - 1;
        return Protocol.CreateWriteMultiple(SlaveID, patternDataAdd, arraydataconfig);
    }

    public Dictionary<string, byte[]> PatternSetting(IConfigurationMainTemperatureController param)
    {
        if (param is UP55AConfig p)
        {
            Dictionary<string, byte[]> Steps = new Dictionary<string, byte[]>();
            Steps.Add("Selected Pattern To Clear", SetPattern_SelectClearPattern(p));
            Steps.Add("Clear Pattern Selected", SetPattern_ClearPattern());
            Steps.Add("Get Error Clear", GetPattern_ErrorClearPattern());
            Steps.Add("Select Pattern To Config", SetPattern_SelectPattern(p));
            Steps.Add("Enable To Write Pattern", SetPattern_ValueToReadWrite());
            Steps.Add("Download Pattern No", SetPattern_Config(p));
            Steps.Add("Get Error Download Pattern", GetPattern_ErrorReadWrite());
            Steps.Add("Get Pattern Config", GetPattern_Config());
            Steps.Add("Selected Pattern To Download Segment", SetSegment_SelectPattern(p));
            foreach (var item in p.Segments)
            {
                foreach (var timeevent in item.LstTimeEvent)
                {
                    if (timeevent.OnTime.TotalMinutes == 0 & timeevent.OffTime.TotalMinutes != 0)
                    {
                        timeevent.IsEnable = true;
                    }
                    else
                    {
                        timeevent.IsEnable = false;
                    }
                }
                Steps.Add($"Selected Segment To DownLoad Segment Param {item.SegmentID}", SetSegment_SelectSegment((Segment)item));
                Steps.Add($"Download Segment No {item.SegmentID}", SetSegment_Config((Segment)item));
                Steps.Add($"Download Segment Event No {item.SegmentID}", SetSegment_ConfigTimeEvent((Segment)item));
                Steps.Add($"Get Error Download Segment {item.SegmentID}", GetSegment_ErrorReadWrite());
                Steps.Add($"Get Segment Config_{item.SegmentID}", GetSegment_Config());
                Steps.Add($"Get Segment TimeEvent_{item.SegmentID}", GetSegment_Config_TimeEvent());
            }
            if (p.UseLocalMode)
            {
                Steps.Add($"Download SetPoint LocalMode", SetTagetSetpoint(p));
            }
            Steps.Add($"Selected Pattern To Run", SelectPattern(p));
            return Steps;
        }
        throw new ArgumentException("Invalid parameter type for UP55A");
    }
    public IResultMainTemperatureController ParsesPatternSetting(string CommandName, byte[] dataParse, IConfigurationMainTemperatureController param)
    {
        if (CommandName.Contains("Get Error Download") || CommandName.Contains("Get Error Clear"))
        {
            short rs = Protocol.ParsesReadSingleRegister(dataParse);
            UP55AResult uP55AResult = new UP55AResult();
            uP55AResult.ErrorCodeReadWritePatternSegment = (errorCodeReadWritePatternSegment)rs;
            uP55AResult.ParsesOK = true;
            if (uP55AResult.ErrorCodeReadWritePatternSegment != errorCodeReadWritePatternSegment.OK)
            {
                throw new Exception (uP55AResult.ErrorCodeReadWritePatternSegment.ToString());
            }
            return uP55AResult;
        }
        else if (CommandName == "Get Pattern Config")
        {
             Pattern datafromdevice =  ParsesPatternGetConfig(dataParse);
             Pattern datadownload = ((UP55AConfig)param).Pattern;

            if (!datafromdevice.Equals(datadownload))
            {
                throw new Exception("DownLoad Pattern Fail");
            }
            else
            {
                UP55AResult uP55AResult = new UP55AResult();
                uP55AResult.ParsesOK = true;
                return uP55AResult;
            }
        }
        else if (CommandName.Contains("Get Segment Config"))
        {
            int SegmentID = int.Parse(CommandName.Split('_').Last().Trim());
            Segment datafromdevice = ParsesSegmentInfor(dataParse);
            Segment datadownload = (((UP55AConfig)param)).Segments.Where(p => p.SegmentID == SegmentID).FirstOrDefault()!;

            if (!datafromdevice.CompareConfig(datadownload))
            {
                throw new Exception("DownLoad Segment Fail");
            }
            else
            {
                UP55AResult uP55AResult = new UP55AResult();
                uP55AResult.ParsesOK = true;
                return uP55AResult;
            }
        }
        else if (CommandName.Contains("Get Segment TimeEvent"))
        {
            int SegmentID = int.Parse(CommandName.Split('_').Last().Trim());
            Segment datafromdevice = ParsesSegmentInforTimeEvent(dataParse);
            Segment datadownload = (((UP55AConfig)param)).Segments.Where(p => p.SegmentID == SegmentID).FirstOrDefault()!;
            if (!datafromdevice.CompareTimeEvent(datadownload))
            {
                throw new Exception("DownLoad Segment Time Event Fail");
            }
            else
            {
                UP55AResult uP55AResult = new UP55AResult();
                uP55AResult.ParsesOK = true;
                return uP55AResult;
            }
        }
        else if (!CommandName.Contains("Download Pattern No") && !CommandName.Contains("Download Segment No") && !CommandName.Contains("Download Segment Event No"))
        {
            bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
            UP55AResult uP55AResult = new UP55AResult();
            uP55AResult.ReadWriteSingleRegisterOK = true;
            return uP55AResult;
        }
        else
        {
            string error = "";
            bool rs = Protocol.ValidByteReceive(dataParse, out error);
            if (!rs)
            {
                throw new Exception(error);
            }
            UP55AResult uP55AResult = new UP55AResult();
            uP55AResult.ParsesOK = false;
            return uP55AResult;
        }
    }
    #endregion
    #region Segment Setting
    public byte[] SetSegment_SelectPattern(IConfigurationMainTemperatureController param)
    {
        CheckPatternID(param);
        ushort patternNumberAdd = 8101 - 1;
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, param.PatternID);
    }
    public byte[] SetSegment_SelectSegment(Segment param)
    {
        CheckSegmentID(param);
        ushort segmentNumberAdd = 8102 - 1;
        return Protocol.CreateWrite(SlaveID, segmentNumberAdd, param.SegmentID);
    }
    public byte[] GetSegment_ErrorReadWrite()
    {
        ushort errorInforAdd = 8173 - 1;
        return Protocol.CreateRead(SlaveID, errorInforAdd, 1);
    }
    public byte[] GetSegment_Config()
    {
        ushort segmentDataAdd = 8103 - 1;
        return Protocol.CreateRead(SlaveID, segmentDataAdd, 22);
    }

    public byte[] GetSegment_Config_TimeEvent()
    {
        ushort segmentDataAdd = 8125 - 1;
        return Protocol.CreateRead(SlaveID, segmentDataAdd, 48);
    }
    public byte[] SetSegment_Config(Segment param)
    {
        short[] arraydataconfig = param.ToListByte().ToArray();
        ushort segmentDataAdd = 8103 - 1;
        return Protocol.CreateWriteMultiple(SlaveID, segmentDataAdd, arraydataconfig.Take(8125 - 8103).ToArray());
    }
    public byte[] SetSegment_ConfigTimeEvent(Segment param)
    {
        short[] arraydataconfig = param.ToListByte().ToArray();
        ushort segmentDataTimeEventAdd = 8125 - 1;
        return Protocol.CreateWriteMultiple(SlaveID, segmentDataTimeEventAdd, arraydataconfig.Skip(8125 - 8103).ToArray());
    }
    public IResultMainTemperatureController ParsesSetHold(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP55AResult uP55AResult = new UP55AResult();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }
    public IResultMainTemperatureController ParsesSetResum(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP55AResult uP55AResult = new UP55AResult();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }
    public IResultMainTemperatureController ParsesSetMode(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP55AResult uP55AResult = new UP55AResult();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }
    public IResultMainTemperatureController ParsesSelectPattern(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP55AResult uP55AResult = new UP55AResult();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }
    #endregion
    #region Common
    private bool CheckPatternID(IConfigurationMainTemperatureController param)
    {
        if (param.PatternID <= 0 || param.PatternID > 30) throw new ArgumentOutOfRangeException(nameof(param.PatternID));
        return true;
    }
    private bool CheckSegmentID(Segment param)
    {
        if (param.SegmentID <= 0 || param.SegmentID > 99) throw new ArgumentOutOfRangeException(nameof(param.SegmentID));
        return true;
    }
    #endregion

    private Pattern ParsesPatternGetConfig(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 3) throw new Exception("Not enough data");
        Pattern uP55APattern = new Pattern();
        uP55APattern.SSP_L1 = lstData[0] / 10f;
        //uP55APattern.SSP_L2 = lstData[1] / 10f;
        uP55APattern.STC = (eStartCode)lstData[2];
        return uP55APattern;
    }
    private Segment ParsesSegmentInfor(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 22) throw new Exception("Not enough data");
        Segment uP55ASegment = new Segment();

        uP55ASegment.TSP_L1 = lstData[0] / 10f;
        //uP55ASegment.TSP_L2 = lstData[1] / 10f;
        uP55ASegment.TIME = new TimeSpan(0, lstData[2], 0);
        //uP55ASegment.TM_RT = lstData[3];
        //uP55ASegment.S_PID = lstData[4];
        //uP55ASegment.JC = (eJunctionCode)lstData[5];
        uP55ASegment.LstPVEvent.Clear();
       
        int index = 0;
        for (int i = 6; i < 6 + 16; i += 2)
        {
            index++;
            uP55ASegment.LstPVEvent.Add(new PVEvent() { EventIndex = index, PVEventType = (ePVEventType)lstData[i], PVEventSetPoint = lstData[i + 1] / 10f });
        }
        return uP55ASegment;
    }
    private Segment ParsesSegmentInforTimeEvent(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 48) throw new Exception("Not enough data");
        Segment uP55ASegment = new Segment();
        uP55ASegment.LstTimeEvent.Clear();
        int index = 0;
        for (int i = 0; i < 48; i += 3)
        {
            index++;
            uP55ASegment.LstTimeEvent.Add(new TimeEvent() { EventIndex = index, IsEnable = lstData[i] == 1, OnTime = new TimeSpan(0, lstData[i + 1], 0), OffTime = new TimeSpan(0, lstData[i + 2], 0) });
        }
        return uP55ASegment;
    }
}
