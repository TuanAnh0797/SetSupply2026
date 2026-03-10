using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;

namespace VsFoundation.Controller.MainTemp.MainTempModels.Models;

public class Segment
{
    public short SegmentID { get; set; }
    /// <summary>
    /// Loop-1 final target setpoint  - 48103 1FA6 (0.0 to 100.0% of PV input range (EU) (Setting range: P.RL to P.RH))
    /// </summary>
    public float TSP_L1 { get; set; } = 0;

    /// <summary>
    /// Loop-1 final target setpoint  - 48104 1FA7 (0.0 to 100.0% of PV input range (EU) (Setting range: P.RL to P.RH))
    /// </summary>
    private float TSP_L2 { get; set; } = 0;// not changed,..

    /// <summary>
    /// Segment time setting - 48105 1FA8; 0 to 59999 (minute or second) * Setting available for the parameter SEG.T=TIME.* Use the parameter TMU to set the time unit.
    /// Minute => 
    /// </summary>
    public TimeSpan TIME { get; set; } = new TimeSpan(0);

    /// <summary>
    /// Segment ramp-rate setting - 48106 1FA9; Ramp: 0.0 to 100.0% of PV input range span (EUS) / 1 hour or 1 minute Soak: 0 to 59999 (minute or second)
    /// </summary>
    private int TM_RT { get; set; } = -1;//not changed,.. default -1

    /// <summary>
    /// Segment PID number selection - 48107 1FAA; // 1 to 8* PID number can be set when the parameter “ZON = 0.”
    /// </summary>
    private int S_PID { get; set; } = 1;//not changed,.. default

    /// <summary>
    /// Junction code - 48108 1FAB; 
    /// </summary>
    private eJunctionCode JC { get; set; } = eJunctionCode.CONT;
    public List<PVEvent> LstPVEvent { get; set; } = new();
    public List<TimeEvent> LstTimeEvent { get; set; } = new();
    public List<short> ToListByte()
    {
        if (LstPVEvent.Count != 8 || LstTimeEvent.Count != 16) { throw new ArgumentException("PVEvent(8) or TimeEvent(16) not enough data"); }
        var lst = new List<short>() { (short)(TSP_L1 * 10), (short)(TSP_L2 * 10), (short)TIME.TotalMinutes, (short)TM_RT, (short)S_PID, (short)JC };
        foreach (var tmp in LstPVEvent.OrderBy(x => x.EventIndex)) { lst.AddRange(tmp.ToListByte()); }
        //var lst = new List<short>();
        foreach (var tmp1 in LstTimeEvent.OrderBy(x => x.EventIndex)) { lst.AddRange(tmp1.ToListByte()); }
        return lst;
    }
    public List<short> ToListByteUP550()
    {

        //UP550 only use 2 PVEvent and 4 TimeEvent

        if (LstPVEvent.Count != 8 || LstTimeEvent.Count != 16) { throw new ArgumentException("PVEvent(8) or TimeEvent(16) not enough data"); }
        var lst = new List<short>() { (short)(TSP_L1 * 10), (short)(TSP_L2 * 10), (short)TIME.TotalMinutes, (short)TM_RT, (short)S_PID };//(short)JC 

        int count1 = 0;
        foreach (var tmp in LstPVEvent.OrderBy(x => x.EventIndex))
        {
            count1++;
            if (count1 > 2) { break; }

            if (tmp.PVEventType == ePVEventType.Disable)
            {
                lst.Add(0);//eUP550EventNumber
            }
            else if ((int)tmp.PVEventType > 10)
            {
                throw new Exception("EventType UP550 not exists");
            }
            else
            {
                lst.Add((short)(count1 + 20));//eUP550EventNumber
            }

            lst.AddRange(tmp.ToListByte());
        }
        int count2 = 0;
        foreach (var tmp in LstTimeEvent.OrderBy(x => x.EventIndex))
        {
            //if (tmp.IsEnable == false) continue;
            count2++;
            if (count2 > 4) { break; }
            if (tmp.OnTime.TotalMinutes == 0 & tmp.OffTime.TotalMinutes == 0)
            {
                lst.Add(0);//eUP550EventNumber
            }
            else
            {
                lst.Add((short)count2);//eUP550EventNumber
            }
            lst.AddRange(tmp.ToListByteUP550());
        }
        int remain = 8 - (count1 + count2);
        if (remain > 0)
        {
            for (int i = 0; i < remain; i++) { lst.AddRange(new List<short> { 0, 0, 0 }); }
        }
        lst.Add((short)JC);
        return lst;
    }
    public Segment()
    {
        LstPVEvent.Clear(); LstTimeEvent.Clear();
        for (int i = 1; i <= 8; i++) { LstPVEvent.Add(new PVEvent() { EventIndex = i }); }
        for (int i = 1; i <= 16; i++) { LstTimeEvent.Add(new TimeEvent() { EventIndex = i }); }
    }
    public bool CompareConfig(Segment segment)
    {
        bool config = false;
        bool pvevent = true;

        if (TSP_L1 == segment.TSP_L1 && TIME == segment.TIME && TM_RT == segment.TM_RT && S_PID == segment.S_PID)
        {
            config = true;
        }
        for (int i = 0; i < LstPVEvent.Count; i++)
        {
            if (LstPVEvent[i].EventIndex != segment.LstPVEvent[i].EventIndex || LstPVEvent[i].PVEventType != segment.LstPVEvent[i].PVEventType || LstPVEvent[i].PVEventSetPoint != segment.LstPVEvent[i].PVEventSetPoint)
            {
                pvevent = false;
                break;
            }
        }
        if (config & pvevent)
        {
            return true;
        }
        return false;
    }
    public bool CompareConfigUP550(Segment segment)
    {
        bool config = false;
        bool pvevent = true;

        if (TSP_L1 == segment.TSP_L1 && TIME == segment.TIME && TM_RT == segment.TM_RT && S_PID == segment.S_PID)
        {
            config = true;
        }
        for (int i = 0; i < LstPVEvent.Count; i++)
        {
            if (LstPVEvent[i].EventIndex != segment.LstPVEvent[i].EventIndex || LstPVEvent[i].PVEventType != segment.LstPVEvent[i].PVEventType || LstPVEvent[i].PVEventSetPoint != segment.LstPVEvent[i].PVEventSetPoint)
            {
                pvevent = false;
                break;
            }
        }
        if (config & pvevent)
        {
            return true;
        }
        return false;
    }



    public bool CompareTimeEvent(Segment segment)
    {
        for (int i = 0; i < LstTimeEvent.Count; i++)
        {
            if (LstTimeEvent[i].IsEnable != segment.LstTimeEvent[i].IsEnable || LstTimeEvent[i].OnTime != segment.LstTimeEvent[i].OnTime || LstTimeEvent[i].OffTime != segment.LstTimeEvent[i].OffTime)
            {
                return false;
            }
        }
        return true;
    }

}
public class PVEvent
{
    public ePVEventType PVEventType { get; set; } = ePVEventType.Disable;
    public float PVEventSetPoint { get; set; } = 0;
    public int EventIndex { get; set; } = 0;
    public List<short> ToListByte()
    {
        return new List<short>() { (short)PVEventType, (short)(PVEventSetPoint * 10) };
    }
}
public class TimeEvent
{
    public bool IsEnable { get; set; } = false;
    public TimeSpan OnTime { get; set; } = new TimeSpan(0,0,0);//minute
    public TimeSpan OffTime { get; set; } = new TimeSpan(0,0,0);


    public int EventIndex { get; set; } = 0;
    public List<short> ToListByte()
    {
        return new List<short>() { (short)(IsEnable ? 1 : 0), (short)OnTime.TotalMinutes, (short)OffTime.TotalMinutes };
    }
    public List<short> ToListByteUP550()
    {
        return new List<short>() { (short)OnTime.TotalMinutes, (short)OffTime.TotalMinutes };
    }
}
