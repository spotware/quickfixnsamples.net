namespace WpfSample.Models
{
    public class SymbolModel : BindableBaseModel
    {
        private decimal _bid;
        private decimal _ask;

        public int Id { get; init; }

        public string Name { get; init; }

        public int Digits { get; init; }

        public decimal Bid { get => _bid; set => SetProperty(ref _bid, value); }

        public decimal Ask { get => _ask; set => SetProperty(ref _ask, value); }
    }
}