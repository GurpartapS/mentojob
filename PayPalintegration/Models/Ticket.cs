using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PayPalintegration.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime TourDate { get; set; }
        public string PayPalReference { get; set; }
    }
}