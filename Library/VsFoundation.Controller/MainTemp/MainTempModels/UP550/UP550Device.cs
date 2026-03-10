

using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.MainTemp.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;
using VsFoundation.Controller.MainTemp.MainTempModels.UP550.Models;
using VsFoundation.Controller.MainTemp.MainTempModels.UP55A.Models;

namespace VsFoundation.Controller.MainTemp.MainTempModels.UP550;

public class UP550Device : IDeviceMainTemperatureController
{
    private IProtocol _protocol;
    public byte SlaveID { get => _slaveID; set => _slaveID = value; }
    public IProtocol Protocol { get => _protocol; set => _protocol = value; }

    private byte _slaveID = (byte)(1 & 0xFF);
    public UP550Device(IProtocol protocol, string slaveid)
    {
        Protocol = protocol;
        SlaveID = (byte)ushort.Parse(slaveid);
    }
    public byte[] GetHold()
    {
        ushort Address = 0209 - 1; //D0209
        return Protocol.CreateRead(SlaveID, Address, 0);
    }

    public byte[] GetMode()
    {
        ushort Address = 0208 - 1; //D0208
        return Protocol.CreateRead(SlaveID, Address, 1);
    }

    public byte[] MonitorData()
    {
        ushort monitorAdd = 0002 - 1; 
        ushort TotalRegister = 44;
        return Protocol.CreateRead(SlaveID, monitorAdd, TotalRegister);
    }
    public IResultMainTemperatureController ParseMonitorData(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 44) throw new Exception("Not enough data");
        MonitorResult subResult = new MonitorResult() ;
        subResult.UP550ADConverterErrorStatus.SetValue(lstData[0]);//D0001
        subResult.PV_L1 = lstData[1] / 10f;//D0003
        subResult.CSP_L1 = lstData[2] / 10f;//D0004
        subResult.OUT_L1 = lstData[3] / 10f;//D0005
        subResult.CurrentPatternNo = lstData[13];//D0015
        subResult.CurrentSegmentNo = lstData[14];//D0016
        subResult.RemainingSegmentTime = new TimeSpan(0, lstData[15], 0);//D0017
        subResult.PVEventStatus.SetValue(lstData[10]); //Bit Configuration of D0012: PVEV (PV Event Status)
        subResult.TimeEventStatus.SetValue(lstData[11], lstData[12]);//Bit Configuration of D0013,14: TMEV1,2(Time Event Status)
        subResult.SegmentInSelectedPattern = lstData[41];//D0043: SEGNO (Current segment number)
        UP550Result result = new();
        result.MonitorResult = subResult;
        result.ParsesOK = true;
        return result;
    }

    public IResultMainTemperatureController ParsesGetHold(byte[] dataParse)
    {
        short Mode = Protocol.ParsesReadSingleRegister(dataParse);
        UP550Result uP55AResult = new UP550Result();
        uP55AResult.IsHold = Mode == 1;
        uP55AResult.ParsesOK = true;
        return uP55AResult;
    }

    public IResultMainTemperatureController ParsesGetMode(byte[] dataParse)
    {
        short Mode = Protocol.ParsesReadSingleRegister(dataParse);
        if (!System.Enum.IsDefined(typeof(eMainTempMode), (int)Mode)) throw new ArgumentOutOfRangeException($"Invalid Time Mode value: {Mode}");
        eMainTempMode rs = (eMainTempMode)Mode;
        UP550Result uP55AResult = new UP550Result();
        uP55AResult.Mode = rs;
        uP55AResult.ParsesOK = true;
        return uP55AResult;
    }
    public Dictionary<string, byte[]> PatternSetting(IConfigurationMainTemperatureController param)
    {
        if (param is UP550Config p)
        {
            Dictionary<string, byte[]> Steps = new Dictionary<string, byte[]>();
            Steps.Add("Selected Pattern To Clear", SetPattern_SelectClearPattern(p));
            Steps.Add("Clear Pattern Selected", SetPattern_ClearPattern());
            Steps.Add("Get Error Clear", GetPattern_ErrorClearPattern());
            Steps.Add("Select Pattern To Config", SetPattern_SelectPattern(p));
            Steps.Add("Download Pattern No", SetPattern_Config(p));
            Steps.Add("Enable To Write Pattern", SetPattern_ValueToWrite());
            Steps.Add("Get Error Download Pattern", GetPattern_ErrorReadWrite());
            Steps.Add("Enable To Read Pattern", SetPattern_ValueToRead());
            Steps.Add("Get Pattern Config", GetPattern_Config());
            Steps.Add("Selected Pattern To Download Segment", SetSegment_SelectPattern(p));
            foreach (var item in p.Segments)
            {
                foreach (var timeevent in item.LstTimeEvent)
                {
                    timeevent.IsEnable = true;
                }
                Steps.Add($"Selected Segment To DownLoad Segment Param {item.SegmentID}", SetSegment_SelectSegment((Segment)item));
                Steps.Add($"Download Segment No {item.SegmentID}", SetSegment_Config((Segment)item));
                Steps.Add("Enable To Write Segment", SetSegment_ValueToWrite());
                Steps.Add($"Get Error Download Segment {item.SegmentID}", GetSegment_ErrorReadWrite());
                Steps.Add("Enable To Read Segment", SetSegment_ValueToRead());
                Steps.Add($"Get Segment Config_{item.SegmentID}", GetSegment_Config());
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
            UP550Result uP55AResult = new UP550Result();
            uP55AResult.ErrorCodeReadWritePatternSegment = (errorCodeReadWritePatternSegment)rs;
            uP55AResult.ParsesOK = true;
            if (uP55AResult.ErrorCodeReadWritePatternSegment != errorCodeReadWritePatternSegment.OK)
            {
                throw new Exception(uP55AResult.ErrorCodeReadWritePatternSegment.ToString());
            }
            return uP55AResult;
        }
        else if (CommandName == "Get Pattern Config")
        {
            Pattern datafromdevice = ParsesPatternGetConfig(dataParse);
            Pattern datadownload = ((UP55AConfig)param).Pattern;

            if (!datafromdevice.Equals(datadownload))
            {
                throw new Exception("DownLoad Pattern Fail");
            }
            else
            {
                UP550Result uP55AResult = new UP550Result();
                uP55AResult.ParsesOK = true;
                return uP55AResult;
            }
        }
        else if (CommandName.Contains("Get Segment Config"))
        {
            int SegmentID = int.Parse(CommandName.Split('_').Last().Trim());
            Segment datafromdevice = ParsesSegmentInfor(dataParse);
            Segment datadownload = (((UP550Config)param)).Segments.Where(p => p.SegmentID == SegmentID).FirstOrDefault()!;

            if (!datafromdevice.CompareConfig(datadownload))
            {
                throw new Exception("DownLoad Segment Fail");
            }
            else if (!datafromdevice.CompareTimeEvent(datadownload))
            {
                throw new Exception("DownLoad Segment Event Fail");
            }
            else
            {
                UP550Result uP55AResult = new UP550Result();
                uP55AResult.ParsesOK = true;
                return uP55AResult;
            }
        }
        else if (!CommandName.Contains("Download Pattern No") && !CommandName.Contains("Download Segment No") && !CommandName.Contains("Download Segment Event No"))
        {
            bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
            UP550Result uP55AResult = new UP550Result();
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
            UP550Result uP55AResult = new UP550Result();
            uP55AResult.ParsesOK = false;
            return uP55AResult;
        }
    }
    public byte[] SetTagetSetpoint(IConfigurationMainTemperatureController param)
    {
        ushort Address = 0101 - 1;
        return Protocol.CreateWrite(SlaveID, Address, (short)(param.SetpointLocalMode * 10));
    }
   

    public byte[] SelectPattern(IConfigurationMainTemperatureController param)
    {
        CheckPatternID(param);
        ushort Address = 0214 - 1; //D0214
        return Protocol.CreateWrite(SlaveID, Address, param.PatternID);
    }
    public IResultMainTemperatureController ParsesSelectPattern(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP550Result uP55AResult = new UP550Result();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }

    public byte[] SetHold()
    {
        ushort Address = 0209 - 1; //D0209
        return Protocol.CreateWrite(SlaveID, Address, 1);
    }
    public IResultMainTemperatureController ParsesSetHold(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP550Result uP55AResult = new UP550Result();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }

    public byte[] SetMode(IConfigurationMainTemperatureController param)
    {
        ushort Address = 0208 - 1; //D0208
        return Protocol.CreateWrite(SlaveID, Address, (short)param.Mode);
    }
    public IResultMainTemperatureController ParsesSetMode(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP550Result uP55AResult = new UP550Result();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }
    public byte[] SetResume()
    {
        ushort Address = 0209 - 1; //D0209
        return Protocol.CreateWrite(SlaveID, Address, 0);
    }
    public IResultMainTemperatureController ParsesSetResum(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        UP550Result uP55AResult = new UP550Result();
        uP55AResult.ReadWriteSingleRegisterOK = true;
        return uP55AResult;
    }
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
    #region Pattern Setting
    public byte[] SetPattern_SelectClearPattern(UP550Config param)
    {
        CheckPatternID(param);
        ushort address = 1761 - 1; //D1761
        return Protocol.CreateWrite(SlaveID, address, param.PatternID);
    }
    public byte[] SetPattern_ClearPattern()
    {
        ushort address = 1763 - 1; //D1763
        return Protocol.CreateWrite(SlaveID, address, 1);
    }
    public byte[] GetPattern_ErrorClearPattern()
    {
        ushort address = 1765 - 1; //D1765
        return Protocol.CreateRead(SlaveID, address, 1);
    }
    public byte[] SetPattern_SelectPattern(UP550Config param)
    {
        CheckPatternID(param);
        ushort patternNumberAdd = 1701 - 1; // D1701//B0001
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, param.PatternID);
    }
    public byte[] SetPattern_ValueToWrite()
    {
        ushort patternNumberAdd = 1704 - 1; // D1704//B0004
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, 1);

    }
    public byte[] SetPattern_Config(UP550Config param)
    {
        short[] arrayDataConfig = param.Pattern.ToListByte().ToArray();
        ushort patternDataAdd = 1711 - 1; //D1711 - B0011
        return Protocol.CreateWriteMultiple(SlaveID, patternDataAdd, arrayDataConfig);
    }
    public byte[] SetPattern_ValueToRead()//SetPattern_ValueToReadWrite
    {
        ushort patternNumberAdd = 1703 - 1; // D1703//B0003
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, 1);

    }
    public byte[] GetPattern_ErrorReadWrite()
    {
        ushort errorInforAdd = 1705 - 1; //D1705 //B0005
        return Protocol.CreateRead(SlaveID, errorInforAdd, 1);

    }
    public byte[] GetPattern_Config()
    {
        ushort patternDataAdd = 1711 - 1; //D1711 - B0011//3 
        return Protocol.CreateRead(SlaveID, patternDataAdd, 3);
    }
    #endregion
    #region Segment Setting
    public byte[] SetSegment_SelectPattern(UP550Config param)
    {
        CheckPatternID(param);
        ushort patternNumberAdd = 1701 - 1; // D1701//B0001
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, param.PatternID);
    }
    public byte[] SetSegment_SelectSegment(Segment param)
    {
        CheckSegmentID(param);
        ushort segmentNumberAdd = 1702 - 1; // D1702//B0002
        return Protocol.CreateWrite(SlaveID, segmentNumberAdd, param.SegmentID);
    }
    public byte[] SetSegment_Config(Segment param)
    {
        short[] arrayDataConfig = param.ToListByteUP550().ToArray();
        ushort segmentDataAdd = 1811 - 1;  //41811 (B0111) =>        (B0140 - D1840)
        return Protocol.CreateWriteMultiple(SlaveID, segmentDataAdd, arrayDataConfig.ToArray());
    }
    public byte[] SetSegment_ValueToWrite()//add
    {
        ushort patternNumberAdd = 1804 - 1; // B0104 (= D1804)
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, 1);

    }
    public byte[] GetSegment_ErrorReadWrite()
    {
        ushort errorInforAdd = 1805 - 1; //D1805 //B0105        
        return Protocol.CreateRead(SlaveID, errorInforAdd, 1);

    }
    public byte[] SetSegment_ValueToRead()//add
    {
        ushort patternNumberAdd = 1803 - 1; // B0103 (= D1803)
        return Protocol.CreateWrite(SlaveID, patternNumberAdd, 1);

    }
    public byte[] GetSegment_Config()
    {
        ushort segmentDataAdd = 1811 - 1; //41811 (B0111) =>        (B0140 - D1840)
        return Protocol.CreateRead(SlaveID, segmentDataAdd, 30);

    }
    #endregion

    private Pattern ParsesPatternGetConfig(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 3) throw new Exception("Not enough data");
        Pattern pattern = new();
        pattern.SSP_L1 = lstData[0] / 10f;
        //pattern.SSP_L2 = lstData[1] / 10f;
        pattern.STC = (eStartCode)lstData[2];
        return pattern;
    }
    private Segment ParsesSegmentInfor(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 30) throw new Exception("Not enough data");
        Segment segment = new Segment();

        segment.TSP_L1 = lstData[0] / 10f;
        //segment.TSP_L2 = lstData[1] / 10f;
        segment.TIME = new TimeSpan(0, lstData[2], 0);
        //segment.TM_RT = lstData[3];
        //segment.S_PID = lstData[4];
        //segment.JC = (eJunctionCode)lstData[39];
        int indexPV = 0;
        int indexTime = 0;

        for (int i = 0; i < 24; i += 3)
        {
            //index++;
            if (lstData[i + 5] == 1 || lstData[i + 5] == 2 || lstData[i + 5] == 3 || lstData[i + 5] == 4)
            {
                segment.LstTimeEvent[indexTime].IsEnable = true;
                segment.LstTimeEvent[indexTime].OnTime = new TimeSpan(0, lstData[i + 5 + 1], 0);
                segment.LstTimeEvent[indexTime].OffTime = new TimeSpan(0, lstData[i + 5 + 2], 0);
                indexTime++;
            }
            else if (lstData[i + 5] == 21 || lstData[i + 5] == 22)
            {
                segment.LstPVEvent[indexPV].PVEventType = (ePVEventType)lstData[i + 5 + 1];
                segment.LstPVEvent[indexPV].PVEventSetPoint =  lstData[i + 5 + 2] / 10;
                indexPV++;
            }
        }
        int pvCount = segment.LstPVEvent.Count();
        int timeCount = segment.LstTimeEvent.Count();
        for (int i = pvCount; i <= 8; i++) { segment.LstPVEvent.Add(new()); }
        for (int i = timeCount; i <= 16; i++) { segment.LstTimeEvent.Add(new()); }
       
        return segment;
    }

}
