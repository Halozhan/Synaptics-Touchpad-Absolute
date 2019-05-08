using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SYNCTRLLib;

namespace TouchpadClient
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        static SynAPICtrl api = new SynAPICtrl();
        static SynDeviceCtrl device = new SynDeviceCtrl();
        static SynPacketCtrl packet = new SynPacketCtrl();
        static int deviceHandle;
        static public int x = 0;
        static public int y = 0;
        //static public float Xmin = 1400, Xmax = 5500, Ymin = 1300, Ymax = 4400;
        static public float Xmin = 2800, Xmax = 5700, Ymin = 1850, Ymax = 4500;
        static public float targetx = 960, targety = 540;

        static void Main(string[] args)
        {
            try
            {
                new Program();
                api.Initialize();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error calling API, you didn't have Synaptics hardware or driver (if you just installed it you need to reboot)");
            loop:
                Console.WriteLine("Press enter to quit OR type \"info\"");
                if (Console.ReadLine().Contains("info"))
                {
                    Console.WriteLine("{0} Exception caught.", e);
                    goto loop;
                }
                return;
            }

            api.Activate();
            //select the first device found
            deviceHandle = api.FindDevice(SynConnectionType.SE_ConnectionAny, SynDeviceType.SE_DeviceTouchPad, -1);
            device.Select(deviceHandle);
            device.Activate();
            device.OnPacket += SynTP_Dev_OnPacket;
            //Console.Title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            Console.SetWindowSize(80, 25);
            Console.SetBufferSize(80, 25);
            Console.WriteLine("터치패드 좌표");
            Console.WriteLine("X:0 Y:0");
            Console.WriteLine("Xmin:" + Xmin + " Ymin:" + Ymin);
            Console.WriteLine("Xmax:" + Xmax + " Ymax:" + Ymax);
            Console.WriteLine("윈도우 좌표");
            Console.WriteLine("X:" + 0 + " Y:" + 0);
            Console.WriteLine("엔터키를 눌러 종료");
            Console.ReadLine();
        }

        public Program()
        {
            //new Thread(new ThreadStart(Client)).Start();
        }

        static public void SynTP_Dev_OnPacket()
        {
            var result = device.LoadPacket(packet);
            if (packet.X > 1)
            {
                Console.SetCursorPosition(0, 1);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 1);
                Console.WriteLine("X:" + packet.X + " Y:" + packet.Y);
                x = Screen.PrimaryScreen.Bounds.Width;
                y = Screen.PrimaryScreen.Bounds.Height;
                
                targetx = (int)((packet.X - Xmin) / (Xmax - Xmin) * x);
                targety = (int)(((Ymax - Ymin) - (packet.Y - Ymin)) / (Ymax - Ymin) * y);

                SetCursorPos((int)targetx, (int)targety);

                Console.SetCursorPosition(0, 5);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 5);
                Console.WriteLine("X:" + targetx + " Y:" + targety);
            }
        }

        public void Client()
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("192.168.137.1"), 8086);
            client.Connect(ipep);
            
            string cmd = string.Empty;
            while (targetx != 1920 && targety != 2048)
            {
                string x_pos = targetx.ToString();
                Send(client, x_pos);

                string y_pos = targety.ToString();
                Send(client, y_pos);
                Thread.Sleep(1);
            }

            client.Close();

        }

        public bool Send(Socket sock, String msg)

        {

            // 먼저 보내게될 데이터의 사이즈를 알아냅니다.

            byte[] data = Encoding.Default.GetBytes(msg);

            int size = data.Length;



            // 그렇게 알아낸 사이즈는 전송에 맞게 byte로 전환을 시켜줍니다.

            // 여기서 데이터 싸이즈는 4byte로 패킷을 설정을 했네요.

            byte[] data_size = new byte[4];

            data_size = BitConverter.GetBytes(size);

            // 그리고 먼저 사이즈를 전송

            sock.Send(data_size);

            // 사이즈 다음 데이터를 size 만큼 전송한다고 알려주면서 그것만큼 들어온뒤 전송을 종료하게끔.

            sock.Send(data, 0, size, SocketFlags.None);

            // 그럼 트루

            return true;

        }

        public bool Recieve(Socket sock, ref String msg)

        {

            // 받을때 전송되는 데이터의 사이즈를 저장받을 변수를 설정합니다.

            byte[] data_size = new byte[4];

            // 받는데 데이터의 사이즈는 전송시에 4byte만큼 보내겠다고 설정을 했기때문에 4까지만 읽습니다.

            sock.Receive(data_size, 0, 4, SocketFlags.None);

            // 전송받은 byte형 데이터를 사용하기 편하게 int 형으로 바꿔서 저장합니다.

            int size = BitConverter.ToInt32(data_size, 0);

            // 그리고 전송받을 데이터의 사이즈는 위에서 구한 size만큼만 할당!

            byte[] data = new byte[size];

            // 전송을 받습니다. size만큼만

            sock.Receive(data, 0, size, SocketFlags.None);

            // 그리고 전송받은 것을 call by reference에 의해 msg로 넘겨주게 됩니다.

            msg = Encoding.Default.GetString(data);

            // 그럼 트루

            return true;

        }
    }
}