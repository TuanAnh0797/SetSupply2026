using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Models.Database;

 public partial class Person : ObservableObject
{

    [NotMapped]
    public int STT { get; set; }

    public int? ID { get; set; }
    [ObservableProperty]
    public string? name;
    public string? TimeUpdate { get; set; }
    public Person()
    {

    }
}
