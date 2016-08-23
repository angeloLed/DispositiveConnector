# DispositiveConnector (Beta)
( C# class ) With this class you can easily interface your application with RFID card reading devices between Serial and TCPIP connection.

## Example Usage
Include DispositiveConnector.cs library in your project and istantiate dispositive like:
```C#
public static List<DispositiveConnector> devices = new List<DispositiveConnector>();
```

Define method for read card:
```C#
public void onCodeReaded(string cardCode, int deviceID)
{
  base.Invoke((Action)delegate
  {
     MessageBox.Show(cardCode);
  });
}
```

Add some device:
```C#
devices.Add(new DispositiveConnector(DispositiveType.Tr515, DispositiveConnection.TcpIp));
devices[0].ipv4 = "10.0.0.125"; //Some ip
devices[0].CodeReaded += onCodeReaded;

devices.Add(new DispositiveConnector(DispositiveType.GP30, DispositiveConnection.Serial));
devices[1].comPort.PortName = "COM10"; //some port
devices[1].CodeReaded += onCodeReaded;
```

Open connections:
```C#
foreach (DispositiveConnector device in devices) {
  device.ConnectionOpen();
}
```

And now test cardRead with one card.
