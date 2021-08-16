using System;
using System.Threading.Tasks.Dataflow;
using QuickFix;
using QuickFix.Fields;

namespace ConsoleSample
{
    public class QuickFixNApp : MessageCracker, IApplication, IDisposable
    {
        private Session _session = null;

        private readonly string _username;
        private readonly string _password;
        private readonly SessionID _sessionId;
        private readonly BufferBlock<Message> _messagesBuffer = new();

        public QuickFixNApp(string username, string password, SessionID sessionId)
        {
            _username = username;
            _password = password;
            _sessionId = sessionId;
        }

        public ISourceBlock<Message> MessagesBuffer => _messagesBuffer;

        public bool IsDisposed { get; private set; }

        public event Action Logon;

        public event Action Logout;

        #region IApplication interface overrides

        public void OnCreate(SessionID sessionID)
        {
            _session = Session.LookupSession(sessionID);
        }

        public void OnLogon(SessionID sessionID)
        {
            Logon?.Invoke();
        }

        public void OnLogout(SessionID sessionID)
        {
            Logout?.Invoke();
        }

        public async void FromAdmin(Message message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
            var messageType = message.Header.GetString(35);

            if (messageType.Equals("0", StringComparison.OrdinalIgnoreCase) || messageType.Equals("1", StringComparison.OrdinalIgnoreCase)) return;

            message.SetField(new StringField(49, _sessionId.SenderCompID));
            message.SetField(new StringField(56, _sessionId.TargetCompID));
            message.SetField(new StringField(50, _sessionId.SenderSubID));
            message.SetField(new StringField(52, DateTimeOffset.UtcNow.ToString("yyyyMMdd-HH:mm:ss")));
            message.SetField(new StringField(553, _username));
            message.SetField(new StringField(554, _password));
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
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
        }

        #endregion IApplication interface overrides

        #region Message cracker

        public async void OnMessage(QuickFix.FIX44.NewOrderSingle message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.SecurityDefinition message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.SecurityList message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.MarketDataIncrementalRefresh message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.MarketDataSnapshotFullRefresh message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.MarketDataRequestReject message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.PositionReport message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.OrderCancelReject message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.ExecutionReport message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        public async void OnMessage(QuickFix.FIX44.BusinessMessageReject message, SessionID sessionID)
        {
            await _messagesBuffer.SendAsync(message);
        }

        #endregion Message cracker

        #region Message

        public void SendMessage(Message message)
        {
            if (_session != null)
            {
                _session.Send(message);
            }
            else
            {
                throw new InvalidOperationException("Can't send message: session not created.");
            }
        }

        #endregion Message

        #region IDisposable

        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            _messagesBuffer.Complete();
        }

        #endregion IDisposable
    }
}