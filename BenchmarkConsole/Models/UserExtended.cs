using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkConsole.Models
{
    public class UserExtended : User
    {
        [MerlinObject]
        public Client Client { get; set; }
    }
}
