# quickFIXnSamples.NET
.NET Samples for QuickFIXn library and Spotware FIX API

To use it please create a new copy of "Config.cfg" file and name it "Config-dev.cfg" in project root, then fill the credentials fields with your cTrader FIX API credentials:

```INI
[DEFAULT]
ConnectionType=initiator
ReconnectInterval=2
FileStorePath=store
FileLogPath=log
StartTime=00:00:00
EndTime=00:00:00
UseDataDictionary=Y
DataDictionary=./FIX44-CSERVER.xml
SocketConnectHost=cTrader_FIX_API_Host_Name
SocketConnectPort=cTrader_FIX_API_Port
LogoutTimeout=100
ResetOnLogon=Y
ResetOnDisconnect=Y
Username=Your_Username
Password=Your_Password

[SESSION]
# inherit ConnectionType, ReconnectInterval and SenderCompID from default
BeginString=FIX.4.4
SenderCompID=Your_SenderCompID
SenderSubID=TRADE
TargetSubID=TRADE
TargetCompID=cServer
HeartBtInt=30 
```

cTrader FIX API has two credentials, one for quote messages and another for trading (Trade) messages.

You have to change the SenderSubID, TargetSubID, SocketConnectHost, and SocketConnectPort to FIX Quotes credentials if you want to use quote messages.

