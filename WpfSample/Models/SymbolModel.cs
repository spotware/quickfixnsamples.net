using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfSample.Models
{
    public class SymbolModel : BindableBaseModel
    {
        private double _bid;
        private double _ask;

        public long Id { get; init; }

        public string Name { get; init; }

        public int Digits { get; init; }

        public double Bid { get => _bid; set => SetProperty(ref _bid, value); }

        public double Ask { get => _ask; set => SetProperty(ref _ask, value); }
    }
}