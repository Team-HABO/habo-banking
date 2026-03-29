using System;
using System.Collections.Generic;
using System.Text;

namespace service_synchronize.Models
{
    public class User
    {
        public string Id { get; set; }
        public List<Account> Accounts { get; set; }
    }
}
