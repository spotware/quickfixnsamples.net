using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickFix;
using QuickFix.Fields;

namespace ConsoleSample
{
    public class QuickFixNApp : MessageCracker, IApplication
    {
        private Session _session = null;

        // This variable is a kludge for developer test purposes.  Don't do this on a production application.
        public IInitiator MyInitiator = null;

        private readonly string _username;
        private readonly string _password;
        private readonly SessionID _sessionId;

        public QuickFixNApp(string username, string password, SessionID sessionId)
        {
            _username = username;
            _password = password;
            _sessionId = sessionId;
        }

        #region IApplication interface overrides

        public void OnCreate(SessionID sessionID)
        {
            _session = Session.LookupSession(sessionID);
        }

        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine();
            Console.WriteLine("Logon - " + sessionID.ToString());
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine();
            Console.WriteLine("Logout - " + sessionID.ToString());
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
            Console.WriteLine();
            Console.WriteLine($"Incoming: {message}");
            Console.WriteLine("--------------------------------------------");
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
            message.SetField(new StringField(49, _sessionId.SenderCompID));
            message.SetField(new StringField(56, _sessionId.TargetCompID));
            message.SetField(new StringField(50, _sessionId.SenderSubID));
            message.SetField(new StringField(52, DateTimeOffset.UtcNow.ToString("yyyyMMdd-HH:mm:ss")));
            message.SetField(new StringField(553, _username));
            message.SetField(new StringField(554, _password));
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine();
            Console.WriteLine($"Incoming: {message}");
            Console.WriteLine("--------------------------------------------");
        }

        public void ToApp(Message message, SessionID sessionID)
        {
            try
            {
                bool possDupFlag = false;
                if (message.Header.IsSetField(Tags.PossDupFlag))
                {
                    possDupFlag = QuickFix.Fields.Converters.BoolConverter.Convert(
                        message.Header.GetString(Tags.PossDupFlag)); /// FIXME
                }
                if (possDupFlag)
                    throw new DoNotSend();
            }
            catch (FieldNotFoundException)
            { }

            Console.WriteLine();
        }

        #endregion IApplication interface overrides

        #region MessageCracker handlers

        public void OnMessage(QuickFix.FIX44.ExecutionReport m, SessionID s)
        {
            Console.WriteLine("Received execution report");
        }

        public void OnMessage(QuickFix.FIX44.OrderCancelReject m, SessionID s)
        {
            Console.WriteLine("Received order cancel reject");
        }

        #endregion MessageCracker handlers

        public void Run()
        {
            while (true)
            {
                try
                {
                    var cmd = QueryAction();

                    if (cmd is null) return;

                    var action = cmd[0].ToCharArray()[0];

                    var fields = cmd.Skip(1).ToArray();

                    if (action == '1')
                        QueryEnterOrder(fields);
                    else if (action == '2')
                        QueryCancelOrder(fields);
                    else if (action == '3')
                        QueryReplaceOrder(fields);
                    else if (action == '4')
                        QueryMarketDataRequest(fields);
                    else if (action == 'g')
                    {
                        if (MyInitiator.IsStopped)
                        {
                            Console.WriteLine("Restarting initiator...");
                            MyInitiator.Start();
                        }
                        else
                            Console.WriteLine("Already started.");
                    }
                    else if (action == 'x')
                    {
                        if (MyInitiator.IsStopped)
                            Console.WriteLine("Already stopped.");
                        else
                        {
                            Console.WriteLine("Stopping initiator...");
                            MyInitiator.Stop();
                        }
                    }
                    else if (action == 'q' || action == 'Q')
                        break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Message Not Sent: " + e.Message);
                    Console.WriteLine("StackTrace: " + e.StackTrace);
                }
            }

            Console.WriteLine("Program shutdown.");
        }

        private void SendMessage(Message message)
        {
            if (_session != null)
            {
                _session.Send(message);
            }
            else
            {
                // This probably won't ever happen.
                Console.WriteLine("Can't send message: session not created.");
            }
        }

        private string[] QueryAction()
        {
            // Commands 'g' and 'x' are intentionally hidden.
            Console.WriteLine("Fields with * are required and you must provide a value for them");

            Console.Write("\n"
                + "1) Enter New Order (*clOrdID|*symbolId|*tradeSide (buy/sell)|*orderType (market, limit, stop)|*volume|posMaintRptID|price|expireTime|designation, ex: 1|newOrder|1|buy|market|10000)\n"
                + "2) Cancel Order (*origClOrdID|*clOrdID|orderId, ex: 2|OrderClientID|CancelOrder)\n"
                + "3) Replace Order\n"
                + "4) Market data (*symoldID|*depth (y/n), ex: 4|1|n)\n"
                + "Q) Quit\n"
                + "Action: "
            );

            string cmd = Console.ReadLine().Trim();

            if (string.IsNullOrWhiteSpace(cmd)) return default;

            var cmdSplit = cmd.Split('|');

            var action = cmdSplit[0];

            HashSet<string> validActions = new("1,2,3,4,q,Q,g,x".Split(','));

            if (action.Length != 1 || validActions.Contains(action) == false) throw new InvalidOperationException("Invalid action");

            return cmdSplit;
        }

        private void QueryEnterOrder(string[] fields)
        {
            Console.WriteLine("\nNewOrderSingle");

            QuickFix.FIX44.NewOrderSingle m = QueryNewOrderSingle44(fields);

            if (m != null && QueryConfirm("Send order"))
            {
                m.Header.GetString(Tags.BeginString);

                SendMessage(m);
            }
        }

        private void QueryCancelOrder(string[] fields)
        {
            Console.WriteLine("\nOrderCancelRequest");

            QuickFix.FIX44.OrderCancelRequest m = QueryOrderCancelRequest44(fields);

            if (m != null && QueryConfirm("Cancel order"))
                SendMessage(m);
        }

        private void QueryReplaceOrder(string[] fields)
        {
            Console.WriteLine("\nCancelReplaceRequest");

            QuickFix.FIX44.OrderCancelReplaceRequest m = QueryCancelReplaceRequest44();

            if (m != null && QueryConfirm("Send replace"))
                SendMessage(m);
        }

        private void QueryMarketDataRequest(string[] fields)
        {
            Console.WriteLine("\nMarketDataRequest");

            QuickFix.FIX44.MarketDataRequest m = QueryMarketDataRequest44(fields[0], fields[1]);

            if (m != null && QueryConfirm("Send market data request"))
                SendMessage(m);
        }

        private bool QueryConfirm(string query)
        {
            Console.WriteLine();
            Console.WriteLine(query + "?: ");
            string line = Console.ReadLine().Trim();
            return (line[0].Equals('y') || line[0].Equals('Y'));
        }

        #region Message creation functions

        private QuickFix.FIX44.NewOrderSingle QueryNewOrderSingle44(string[] fields)
        {
            var ordType = QueryOrdType(fields[3]);

            QuickFix.FIX44.NewOrderSingle newOrderSingle = new QuickFix.FIX44.NewOrderSingle(
                new ClOrdID(fields[0]),
                new Symbol(fields[1]),
                new Side(fields[2].ToLowerInvariant().Equals("buy") ? '1' : '2'),
                new TransactTime(DateTime.Now),
                ordType);

            newOrderSingle.Set(new OrderQty(Convert.ToDecimal(fields[4])));

            if (ordType.getValue() != OrdType.MARKET)
            {
                newOrderSingle.Set(new TimeInForce('1'));

                if (fields.Length >= 7)
                {
                    if (ordType.getValue() == OrdType.LIMIT)
                    {
                        newOrderSingle.Set(new Price(Convert.ToDecimal(fields[6])));
                    }
                    else
                    {
                        newOrderSingle.Set(new StopPx(Convert.ToDecimal(fields[6])));
                    }
                }

                if (fields.Length >= 8 && DateTime.TryParse(fields[7], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiryTime))
                {
                    newOrderSingle.Set(new ExpireTime(expiryTime));
                }
            }
            else
            {
                newOrderSingle.Set(new TimeInForce('3'));

                if (fields.Length >= 6)
                {
                    newOrderSingle.SetField(new StringField(721, fields[5]));
                }
            }

            if (fields.Length >= 9)
            {
                newOrderSingle.Set(new Designation(fields[8]));
            }

            return newOrderSingle;
        }

        private QuickFix.FIX44.OrderCancelRequest QueryOrderCancelRequest44(string[] fields)
        {
            QuickFix.FIX44.OrderCancelRequest orderCancelRequest = new()
            {
                OrigClOrdID = new OrigClOrdID(fields[0].Trim()),
                ClOrdID = new ClOrdID(fields[1].Trim())
            };

            if (fields.Length >= 3)
            {
                orderCancelRequest.OrderID = new OrderID(fields[2].Trim());
            }

            return orderCancelRequest;
        }

        private QuickFix.FIX44.OrderCancelReplaceRequest QueryCancelReplaceRequest44()
        {
            QuickFix.FIX44.OrderCancelReplaceRequest ocrr = new QuickFix.FIX44.OrderCancelReplaceRequest(
                QueryOrigClOrdID(),
                QueryClOrdID(),
                QuerySymbol(),
                QuerySide(),
                new TransactTime(DateTime.Now),
                QueryOrdType(null));

            ocrr.Set(new HandlInst('1'));
            if (QueryConfirm("New price"))
                ocrr.Set(QueryPrice());
            if (QueryConfirm("New quantity"))
                ocrr.Set(QueryOrderQty());

            return ocrr;
        }

        private QuickFix.FIX44.MarketDataRequest QueryMarketDataRequest44(string symbolId, string depth)
        {
            MDReqID mdReqID = new MDReqID("MARKETDATAID");
            SubscriptionRequestType subType = new SubscriptionRequestType('1');
            MarketDepth marketDepth = new MarketDepth(depth.ToLowerInvariant().Equals("y") ? 1 : 0);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup marketDataEntryGroup = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
            marketDataEntryGroup.Set(new MDEntryType('1'));

            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup symbolGroup = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();
            symbolGroup.Set(new Symbol(symbolId));

            QuickFix.FIX44.MarketDataRequest message = new QuickFix.FIX44.MarketDataRequest(mdReqID, subType, marketDepth);
            message.AddGroup(marketDataEntryGroup);
            message.AddGroup(symbolGroup);

            return message;
        }

        #endregion Message creation functions

        #region field query private methods

        private ClOrdID QueryClOrdID()
        {
            Console.WriteLine();
            Console.Write("ClOrdID? ");
            return new ClOrdID(Console.ReadLine().Trim());
        }

        private OrigClOrdID QueryOrigClOrdID()
        {
            Console.WriteLine();
            Console.Write("OrigClOrdID? ");
            return new OrigClOrdID(Console.ReadLine().Trim());
        }

        private Symbol QuerySymbol()
        {
            Console.WriteLine();
            Console.Write("Symbol? ");
            return new Symbol(Console.ReadLine().Trim());
        }

        private Side QuerySide()
        {
            Console.WriteLine();
            Console.WriteLine("1) Buy");
            Console.WriteLine("2) Sell");
            Console.WriteLine("3) Sell Short");
            Console.WriteLine("4) Sell Short Exempt");
            Console.WriteLine("5) Cross");
            Console.WriteLine("6) Cross Short");
            Console.WriteLine("7) Cross Short Exempt");
            Console.Write("Side? ");
            string s = Console.ReadLine().Trim();

            char c = ' ';
            switch (s)
            {
                case "1": c = Side.BUY; break;
                case "2": c = Side.SELL; break;
                case "3": c = Side.SELL_SHORT; break;
                case "4": c = Side.SELL_SHORT_EXEMPT; break;
                case "5": c = Side.CROSS; break;
                case "6": c = Side.CROSS_SHORT; break;
                case "7": c = 'A'; break;
                default: throw new Exception("unsupported input");
            }
            return new Side(c);
        }

        private OrdType QueryOrdType(string orderType)
        {
            var result = orderType.ToLowerInvariant() switch
            {
                "market" => OrdType.MARKET,
                "limit" => OrdType.LIMIT,
                "stop" => OrdType.STOP,
                _ => throw new Exception("unsupported input"),
            };

            return new OrdType(result);
        }

        private OrderQty QueryOrderQty()
        {
            Console.WriteLine();
            Console.Write("OrderQty? ");
            return new OrderQty(Convert.ToDecimal(Console.ReadLine().Trim()));
        }

        private Price QueryPrice()
        {
            Console.WriteLine();
            Console.Write("Price? ");
            return new Price(Convert.ToDecimal(Console.ReadLine().Trim()));
        }

        #endregion field query private methods
    }
}