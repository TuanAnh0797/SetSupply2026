using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.Logger.LoggerModels.Interfacce;
public class MonitorData
{
    private double chanel1 = 0;
    private double chanel2 = 0;
    private double chanel3 = 0;
    private double chanel4 = 0;
    private double chanel5 = 0;
    private double chanel6 = 0;
    private double chanel7 = 0;
    private double chanel8 = 0;
    public double Chanel1 { get => chanel1; set => chanel1 = value; }
    public double Chanel2 { get => chanel2; set => chanel2 = value; }
    public double Chanel3 { get => chanel3; set => chanel3 = value; }
    public double Chanel4 { get => chanel4; set => chanel4 = value; }
    public double Chanel5 { get => chanel5; set => chanel5 = value; }
    public double Chanel6 { get => chanel6; set => chanel6 = value; }
    public double Chanel7 { get => chanel7; set => chanel7 = value; }
    public double Chanel8 { get => chanel8; set => chanel8 = value; }
    public void SetData(double[] data)
    {
        Chanel1 = data[0];
        Chanel2 = data[1];
        Chanel3 = data[2];
        Chanel4 = data[3];
        Chanel5 = data[4];
        Chanel6 = data[5];
        Chanel7 = data[6];
        Chanel8 = data[7];
    }

}
