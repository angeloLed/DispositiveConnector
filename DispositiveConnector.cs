using System;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;

    public class DispositiveConnector
    {
        #region __CONSTRUCTOR
        public DispositiveConnector() {
            this.Inizialize();
        }
        public DispositiveConnector(DispositiveType type,DispositiveConnection connection) { 
            this.Inizialize();
            this.type = type;
            this.connection = connection;
        }
        private void Inizialize()
        {
            //inizializza comunicazione seriale
            this.comPort = new SerialPort();
            this.comPort.BaudRate = 9600;
            this.comPort.Parity = Parity.None;
            this.comPort.StopBits = StopBits.One;
            this.comPort.DataBits = 8;
            this.comPort.Handshake = Handshake.None;
            this.comPort.DataReceived += new SerialDataReceivedEventHandler(SerialReciveData);

            //inizializza comunicazione TcpIP
            this.tpcClient = new TcpClient();
            this.tcpipPort = 1001;
        }
        #endregion

        #region EVENT & DELEGATE
        public delegate void ReciveDataEventHandler(string code,int DispositiveID);
        public event ReciveDataEventHandler CodeReaded;
        #endregion

        #region VARIABLES
        public int ID { get; set; }
        public string name { get; set; }
        public DispositiveType type { get; set; }
        public DispositiveConnection connection { get; set; }
        public string ipv4 { get; set; }
        public string ipv6 { get; set; }
        public int tcpipPort { get; set; }
        public SerialPort comPort { get; set; }
        private TcpClient tpcClient { get; set; }
        private StreamReader streamReader { get; set; }
        private Thread theread { get; set; }
        private byte[] ReadBuffer;
        private string tempCode;
        #endregion

        #region METHOD
        
        //gestione apertura/chiusura connessione
        public void ConnectionOpen()
        {
            if (this.connection == DispositiveConnection.Serial)
            {
                if (!this.comPort.IsOpen)
                {
                    this.comPort.Open();
                }
            }
            else if (this.connection == DispositiveConnection.TcpIp)
            {
                this.TcpIpSendCommand(TR515Commands.openConnection);
                Thread.Sleep(1000);
                this.TcpIpSendCommand(TR515Commands.extendedCmd);
            }
            else
            {
                throw new Exception("Type connection not specified!");
            }
        }
        public void ConnectionClose()
        {
            if (this.connection == DispositiveConnection.Serial)
            {
                if (this.comPort.IsOpen)
                    this.comPort.Close();
            }
            else if (this.connection == DispositiveConnection.TcpIp)
            {
                if (this.tpcClient.Connected)
                    this.tpcClient.Close();
            }
            else
            {
                throw new Exception("Type dispositive not specified!");
            }
        }
       
        //GP30 ( serial )
        private void SerialReciveData(object sender, SerialDataReceivedEventArgs e)
        {
            if (this.comPort.IsOpen)
            {
                SerialPort sp = (SerialPort)sender;
                if (sp.IsOpen)
                {
                    try
                    {
                        string codice = sp.ReadLine();//riceve in esadecimale
                        if (codice.Trim() != "")
                            Read(codice);
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("Operazione di I/O terminata a causa dell'uscita dal thread oppure della richiesta di un'applicazione."))
                            throw new Exception(ex.Message);
                    }
                }
            }
        }
        
        //TR515 (tcp/ip)
        public void TcpIpSendCommand(byte[] dati)
        {
            if (!this.tpcClient.Connected)
            {
                this.ReadBuffer = new byte[256];
                try { this.tpcClient.Connect(this.ipv4, this.tcpipPort); }
                catch {
                    this.tpcClient = new TcpClient();
                    this.tpcClient.Connect(this.ipv4, this.tcpipPort); 
                }
                
                this.tpcClient.Client.BeginReceive(this.ReadBuffer, 0, this.ReadBuffer.Length, SocketFlags.None, new AsyncCallback(TcpIpReceiveData), this.tpcClient);
                this.streamReader = null;
            }
            NetworkStream ns = this.tpcClient.GetStream();
            if (this.streamReader == null)
                this.streamReader = new StreamReader(this.tpcClient.GetStream());
            ns.Write(dati, 0, dati.Length);
        }
        private void TcpIpReceiveData(IAsyncResult res)
        {
            if (this.tpcClient.Connected)
            {
                TcpClient cl = (TcpClient)res.AsyncState;
                int bytes = 0;
                try
                {
                    bytes = cl.Client.EndReceive(res);
                    if (bytes > 0)
                    {
                        string received = ASCIIEncoding.ASCII.GetString(ReadBuffer, 0, bytes);
                        this.tempCode += received;

                        if (this.tempCode.Contains("\r"))
                        {
                            this.Read(this.tempCode);
                            this.tempCode = "";
                        }

                        TcpIpSendCommand(TR515Commands.acknowledge);
                    }
                    cl.Client.BeginReceive(ReadBuffer, 0, ReadBuffer.Length, SocketFlags.None, new AsyncCallback(TcpIpReceiveData), cl);
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("connessione in corso interrotta forzatamente dall'host remoto"))
                    {
                        this.TcpIpSendCommand(TR515Commands.openConnection);
                        Thread.Sleep(1000);
                        this.TcpIpSendCommand(TR515Commands.extendedCmd);
                    }
                }
            }
        }
        public void openRele(int seconds)
        {
            if (seconds > 0)
            {
                byte[] tempo = ASCIIEncoding.ASCII.GetBytes(seconds.ToString().PadLeft(2, '0'));
                byte[] cmd = new byte[] { 0x02, 0x6C, tempo[0], tempo[1], 0x0D };
                this.TcpIpSendCommand(cmd);
            }
            else
                throw new Exception("I secondi deveono essere maggiorni di zero");
        }

        protected virtual void Read(string code)
        {
            if (CodeReaded != null)
                CodeReaded(code,this.ID);
        }
        
        #endregion
    }

    public enum DispositiveType { 
        Tr515 = 0 ,
        GP30 = 1 ,
        PCR300 = 2,
        PCR330 = 3,
    }
    public enum DispositiveConnection
    {
        Serial = 0,
        TcpIp = 1,
    }

    public class TR515Commands
    {
        public static readonly byte[] openConnection = new byte[] { 0x02, 0x44, 0x30, 0x30, 0x0D };
        public static readonly byte[] extendedCmd = new byte[] { 0x02, 0x69, 0x36, 0x39, 0x0D };
        public static readonly byte[] acknowledge = new byte[] { 0x02, 0x61, 0x36, 0x31, 0x0D };
        public static readonly byte[] openrele = new byte[] { 0x02, 0x6C, 48, 49, 0x0D }; //speertura del relè per 1 secondo
    }
   

