using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WpfSample.Models
{
    public class NewOrderModel : BindableBaseModel
    {
        private string _selectedOrderType;
        private string _selectedTradeSide;
        private Symbol _selectedSymbol;
        private double _quantity;
        private bool _isPendingOrder;
        private bool _isMarketOrder;
        private long _positionId;
        private string _clOrdId = "NewOrder";
        private DateTime? _expiry;
        private string _designation;
        private double _targetPrice;

        public string[] OrderTypes { get; } = new string[] { "Market", "Limit", "Stop" };

        public string SelectedOrderType
        {
            get => _selectedOrderType;
            set
            {
                if (SetProperty(ref _selectedOrderType, value))
                {
                    IsPendingOrder = "Market".Equals(value, StringComparison.InvariantCultureIgnoreCase) is false;
                    IsMarketOrder = "Market".Equals(value, StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }

        public string[] TradeSides { get; } = new string[] { "Buy", "Sell" };

        public string SelectedTradeSide
        {
            get => _selectedTradeSide;
            set => SetProperty(ref _selectedTradeSide, value);
        }

        public Symbol SelectedSymbol
        {
            get => _selectedSymbol;
            set => SetProperty(ref _selectedSymbol, value);
        }

        public double Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public bool IsPendingOrder
        {
            get => _isPendingOrder;
            set => SetProperty(ref _isPendingOrder, value);
        }

        public bool IsMarketOrder
        {
            get => _isMarketOrder;
            set => SetProperty(ref _isMarketOrder, value);
        }

        public long PositionId
        {
            get => _positionId;
            set => SetProperty(ref _positionId, value);
        }

        public string ClOrdId
        {
            get => _clOrdId;
            set => SetProperty(ref _clOrdId, value);
        }

        public DateTime? Expiry
        {
            get => _expiry;
            set => SetProperty(ref _expiry, value);
        }

        public string Designation
        {
            get => _designation;
            set => SetProperty(ref _designation, value);
        }

        public double TargetPrice
        {
            get => _targetPrice;
            set => SetProperty(ref _targetPrice, value);
        }
    }
}