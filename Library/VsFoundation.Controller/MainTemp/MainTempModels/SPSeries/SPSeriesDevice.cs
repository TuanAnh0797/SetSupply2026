using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.MainTemp.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;
using VsFoundation.Controller.MainTemp.MainTempModels.SPSeries.Models;

namespace VsFoundation.Controller.MainTemp.MainTempModels.SPSeries;



//static int reg[REG_COUNT] = { 116, 401, 402, 406, 407, 502, 503, 601, 602, 603, 604, 605, 627, 628, 629, 630, 641, 667 };
//static int val[REG_COUNT] = { 2, 1, 2, 5000, 0, 1, 1, 2, 0, 5000, 0, 1, 6, 3, 15, 2, 1000, 10 };

//#define PV_HIGH_TEMP_INDEX  3   // AL1
//#define PV_LOW_TEMP_INDEX   4   // AL2
//#define EV3_INDEX			14  // EV3
//#define OH_INDEX            16  // OH

public class SPSeriesDevice : IDeviceMainTemperatureController
{
    private IProtocol _protocol;
    public byte SlaveID { get => _slaveID; set => _slaveID = value; }
    public IProtocol Protocol { get => _protocol; set => _protocol = value; }
    private byte _slaveID = (byte)(1 & 0xFF);
    public SPSeriesDevice(IProtocol protocol, string slaveid)
    {
        Protocol = protocol;
        SlaveID = (byte)ushort.Parse(slaveid);
    }
    public byte[] GetHold()
    {
        ushort Address = 0112;
        return Protocol.CreateRead(SlaveID, Address, 1);
    }
    public byte[] GetMode()
    {
        ushort Address = 0111;
        return Protocol.CreateRead(SlaveID, Address, 1);
    }
    public byte[] MonitorData()
    {
        ushort monitorAdd = 0001 ;
        ushort TotalRegister = 40;
        return Protocol.CreateRead(SlaveID, monitorAdd, TotalRegister);
    }
    public IResultMainTemperatureController ParseMonitorData(byte[] dataParse)
    {
        string err = string.Empty;
        if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
        var lstData = Protocol.GetListReceiveData(dataParse);
        if (lstData.Count < 40) throw new Exception("Not enough data");
        MonitorResult MonitorResult = new MonitorResult();
        MonitorResult.PV_L1 = lstData[0] / 10f;
        MonitorResult.CSP_L1 = lstData[1] / 10f;
        MonitorResult.TSP_L1 = lstData[2] / 10f;
        MonitorResult.OUT_L1 = lstData[5] / 10f;
        MonitorResult.CurrentPatternNo = lstData[24];
        MonitorResult.CurrentSegmentNo = lstData[25];
        MonitorResult.RemainingSegmentTime = new TimeSpan(0, lstData[28] - lstData[27], 0);
        MonitorResult.PVEventStatus.SetValueSPSeries(lstData[13]);
        MonitorResult.TimeEventStatus.SetValueSPSeries(lstData[16]);
        MonitorResult.SegmentInSelectedPattern = lstData[26];
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.MonitorResult = MonitorResult;
        return SPSeriesResult;
    }
    public IResultMainTemperatureController ParsesGetHold(byte[] dataParse)
    {
        short Mode = Protocol.ParsesReadSingleRegister(dataParse);
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.IsHold = Mode == 1;
        SPSeriesResult.ParsesOK = true;
        return SPSeriesResult;
    }
    public IResultMainTemperatureController ParsesGetMode(byte[] dataParse)
    {
        short Mode = Protocol.ParsesReadSingleRegister(dataParse);
        if (!System.Enum.IsDefined(typeof(eMainTempMode), (int)Mode)) throw new ArgumentOutOfRangeException($"Invalid Mode value: {Mode}");

        eMainTempMode rsmode = eMainTempMode.RESET;
        if (Mode == 1 )
        {
            rsmode = eMainTempMode.RESET;
        }
        else if (Mode == 2 || Mode == 3)
        {
            rsmode = eMainTempMode.PROG;
        }
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.Mode = rsmode;
        SPSeriesResult.ParsesOK = true;
        return SPSeriesResult;
    }
    public Dictionary<string, byte[]> PatternSetting(IConfigurationMainTemperatureController param)
    {
        if (param.PatternID < 1 || param.PatternID > 2)
        {
            throw new ArgumentException("Sp series has only 2 patterns: 1 and 2");
        }
        if (param is SPSeriesConfig p)
        {
            Dictionary<string, byte[]> Steps = new Dictionary<string, byte[]>();
            Steps.Add("Set STC", SetSTC(p));
            Steps.Add("Get STC", GetSTC());
            Steps.Add("Set SSP", SetSSP(p));
            Steps.Add("Get SSP", GetSSP(p));
            Steps.Add("Set LinkCode", SetLinkCode(p, eLinkCode.RST));
            Steps.Add("Get LinkCode", GetLinkCode(p));
            int indexSegment = 1;
            ushort address = 1104;
            if (param.PatternID == 2)
            {
                address = 1204;
            }
            foreach (var item in p.Segments)
            {
                if (indexSegment > 15)
                {
                    break;
                }
                Steps.Add($"Set Segment No_{item.SegmentID}", SetSegment(address,item,0));
                Steps.Add($"Get Segment No_{item.SegmentID}", GetSegment(address));
                address = (ushort)(address + indexSegment * 3);
                indexSegment++;
            }
            return Steps;
        }
        throw new ArgumentException("Invalid parameter type for SP Series");
    }
    public IResultMainTemperatureController ParsesPatternSetting(string CommandName, byte[] dataParse, IConfigurationMainTemperatureController param)
    {
        if (CommandName == "Get STC")
        {
            short value = Protocol.ParsesReadSingleRegister(dataParse);
            if (value != (short)param.Pattern.STC)
            {
                throw new Exception("DownLoad STC Fail");
            }
            SPSeriesResult Result = new SPSeriesResult();
            Result.ParsesOK = true;
            return Result;
        }
        else if (CommandName == "Get SSP")
        {
            short value = Protocol.ParsesReadSingleRegister(dataParse);
            if (value*10 != (short)param.Pattern.SSP_L1)
            {
                throw new Exception("DownLoad SSP Fail");
            }
            SPSeriesResult Result = new SPSeriesResult();
            Result.ParsesOK = true;
            return Result;
        }
        else if (CommandName == "Get LinkCode")
        {
            short value = Protocol.ParsesReadSingleRegister(dataParse);
            if (value != (short)eLinkCode.RST)
            {
                throw new Exception("DownLoad LinkCode Fail");
            }
            SPSeriesResult Result = new SPSeriesResult();
            Result.ParsesOK = true;
            return Result;
        }
        else if (CommandName.Contains("Get Segment No"))
        {
            string err = string.Empty;
            if (!Protocol.ValidByteReceive(dataParse, out err)) throw new Exception(err);
            var lstData = Protocol.GetListReceiveData(dataParse);
            if (lstData.Count < 3) throw new Exception("Not enough data");
            short SetPoint = lstData[0];
            short Time = lstData[1];
            short TimeSignal = lstData[2];
            short segmentid = short.Parse(CommandName.Split("_").Last());
            Segment segmentconfig = param.Segments.Where(p=> p.SegmentID == segmentid).FirstOrDefault();
            if (segmentconfig.TSP_L1 * 10 != SetPoint || segmentconfig.TIME.TotalMinutes != Time || TimeSignal != 0)
            {
                throw new Exception(CommandName + " Fail");
            }
            SPSeriesResult Result = new SPSeriesResult();
            Result.ParsesOK = true;
            return Result;
        }
        else
        {
            string error = "";
            bool rs = Protocol.ValidByteReceive(dataParse, out error);
            if (!rs)
            {
                throw new Exception(error);
            }
            SPSeriesResult Result = new SPSeriesResult();
            Result.ParsesOK = false;
            return Result;
        }
    }
    public IResultMainTemperatureController ParsesSelectPattern(byte[] dataParse)
    {
        return null;
    }
    public IResultMainTemperatureController ParsesSetHold(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.ReadWriteSingleRegisterOK = true;
        return SPSeriesResult;
    }
    public IResultMainTemperatureController ParsesSetMode(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.ReadWriteSingleRegisterOK = true;
        return SPSeriesResult;
    }
    public IResultMainTemperatureController ParsesSetResum(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.ReadWriteSingleRegisterOK = true;
        return SPSeriesResult;
    }
    public byte[] SelectPattern(IConfigurationMainTemperatureController param)
    {
        return null;
    }
    public byte[] SetHold()
    {
        ushort Address = 0112;
        return Protocol.CreateWrite(SlaveID, Address, 1);
    }
    public byte[] SetMode(IConfigurationMainTemperatureController param)
    {
        if (param.PatternID > 2)
        {
            throw new Exception("Sp series has only 2 patterns: 1 and 2");
        }
        ushort Address = 0111;
        if (param.Mode == Enum.eMainTempMode.RESET)
        {
            return Protocol.CreateWrite(SlaveID, Address, 1);
        }
        else if (param.Mode == Enum.eMainTempMode.PROG)
        {
            if (param.PatternID == 1)
            {
                return Protocol.CreateWrite(SlaveID, Address, 2);
            }
            else 
            {
                return Protocol.CreateWrite(SlaveID, Address, 3);
            }
        }
        else
        {
            throw new Exception("Sp series don't have Local Mode");
        }
    }
    public byte[] SetResume()
    {
        ushort Address = 0112;
        return Protocol.CreateWrite(SlaveID, Address, 0);
    }
    private byte[] SetSTC(IConfigurationMainTemperatureController param)
    {
        ushort Address = 1002;
        short STC = 0;
        if (param.Pattern.STC != eStartCode.SSP)
        {
            STC = 1;
        }
        return Protocol.CreateWrite(SlaveID, Address, STC);
    }
    private byte[] GetSTC()
    {
        ushort Address = 1002;
        return Protocol.CreateRead(SlaveID, Address, 1);
    }
    private byte[] SetSSP(IConfigurationMainTemperatureController param)
    {
        ushort Address = 1102;
        if (param.PatternID == 2)
        {
            Address = 1202;
        }
        return Protocol.CreateWrite(SlaveID, Address, (short)(param.Pattern.SSP_L1*10));
    }
    private byte[] GetSSP(IConfigurationMainTemperatureController param)
    {
        ushort Address = 1102;
        if (param.PatternID == 2)
        {
            Address = 1202;
        }
        return Protocol.CreateRead(SlaveID, Address, 1);
    }
    private byte[] SetLinkCode(IConfigurationMainTemperatureController param, eLinkCode eLinkCode)
    {
        ushort Address = 1101;
        if (param.PatternID == 2)
        {
            Address = 1201;
        }
        return Protocol.CreateWrite(SlaveID, Address, (short)eLinkCode);
    }
    private byte[] GetLinkCode(IConfigurationMainTemperatureController param)
    {
        ushort Address = 1101;
        if (param.PatternID == 2)
        {
            Address = 1201;
        }
        return Protocol.CreateRead(SlaveID, Address, 1);
    }
    private byte[] SetSegment(ushort Address,  Segment param, short TimeSignal)
    {
        short[] data = [(short)(param.TSP_L1*10), (short)param.TIME.TotalMinutes, TimeSignal];
        return Protocol.CreateWriteMultiple(SlaveID, Address,data);
    }
    private byte[] GetSegment(ushort Address)
    {
        return Protocol.CreateRead(SlaveID, Address, 3);
    }
    private byte[] SetPVEventType(eAlarmNo eAlarmNo, eAlarmType eAlarmType)
    {
        return Protocol.CreateWrite(SlaveID, (ushort)eAlarmNo, (short)eAlarmType);
    }
    private byte[] GetPVEventType(eAlarmNo eAlarmNo)
    {
        return Protocol.CreateRead(SlaveID, (ushort)eAlarmNo,1);
    }
    private byte[] SetPVEventTemp(eAlarmNo eAlarmNo, short PVHighLow)
    {
        return Protocol.CreateWrite(SlaveID, (ushort)(eAlarmNo + 5), (short)(PVHighLow*10));
    }
    private byte[] GetPVEventTemp(eAlarmNo eAlarmNo)
    {
        return Protocol.CreateRead(SlaveID, (ushort)(eAlarmNo + 5), 1);
    }
    public Dictionary<string, byte[]> SetPVEvent(eAlarmNo eAlarmNo, eAlarmType eAlarmType, short PVHighLow)
    {
        Dictionary<string, byte[]> Steps = new Dictionary<string, byte[]>();
        Steps.Add("Set PV Event Type", SetPVEventType(eAlarmNo, eAlarmType));
        Steps.Add("Get PV Event Type", GetPVEventType(eAlarmNo));
        Steps.Add("Set PV Event Value", SetPVEventTemp(eAlarmNo, PVHighLow));
        Steps.Add("Get PV Event Value", GetPVEventTemp(eAlarmNo));
        return Steps;
    }
    public void ParsesSetPVEvent(string CommandName ,byte[] dataParse, eAlarmNo eAlarmNo, eAlarmType eAlarmType, short PVHighLow)
    {
        if (CommandName == "Get PV Event Type")
        {
            short value = Protocol.ParsesReadSingleRegister(dataParse);
            if (value != (short)eAlarmType)
            {
                throw new Exception("DownLoad Event Type Fail");
            }
        }
        else if (CommandName == "Get PV Event Value")
        {
            short value = Protocol.ParsesReadSingleRegister(dataParse);
            if (value != PVHighLow *10)
            {
                throw new Exception("DownLoad Event Value Fail");
            }
        }
    }
    public byte[] SetTimeSignal(short PatternID, short SegmentNo , eTimeSignal Status)
    {
        if (PatternID < 1 || PatternID > 2)
        {
            throw new ArgumentException("Sp series has only 2 patterns: 1 and 2");
        }
        if (SegmentNo < 1 || SegmentNo > 15)
        {
            throw new ArgumentException("Sp series have 15 Segment from 1 to 15");
        }
        ushort address = 1106;
        if (PatternID == 2) {
            address = 1206;
        }
        address = (ushort)(address + (SegmentNo - 1) * 3);
        return Protocol.CreateWrite(SlaveID, address, (short)Status);
    }
    public IResultMainTemperatureController ParsesSetTimeSignal(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.ReadWriteSingleRegisterOK = true;
        return SPSeriesResult;
    }
    public byte[] SetCoolingParam(eCoolingState state)
    {
        ushort address = 0629;
        return Protocol.CreateWrite(SlaveID, address, (short)state);
    }
    public IResultMainTemperatureController ParsesSetCoolingParam(byte[] dataParse)
    {
        bool rs = Protocol.ParsesWriteSingleRegister(dataParse);
        SPSeriesResult SPSeriesResult = new SPSeriesResult();
        SPSeriesResult.ReadWriteSingleRegisterOK = true;
        return SPSeriesResult;
    }

}
