using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Models.Database;

public class PLCSetting
{
    public string Name { get; set; }
    public string IPAddress { get; set; }
    public int Port { get; set; }
    public int AddressData { get; set; }
    public int TotalRegisterData { get; set; }
    public int AddressTrigger { get; set; }
    public int AddressConnfirmFinalReadData{ get; set; }
    public int AddressAlive { get; set; }
    public DateTime? TimeUpdate { get; set; }
}
