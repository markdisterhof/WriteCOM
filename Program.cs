//Program is modified from https://docs.microsoft.com/de-de/dotnet/api/system.io.ports.serialport?view=netframework-4.8


using System;
using System.IO.Ports;
using System.Threading;

public class PortChat
{
    static bool _continue;
    static SerialPort _serialPort;
    private static byte enc_length = 8;

    public static void Main()
    {
        string message;
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        Thread readThread = new Thread(Read);
        Console.SetWindowSize(90, 25);
        
        // Create a new SerialPort object with default settings.
        _serialPort = new SerialPort();
        _serialPort.PortName = GetCOMPort();
        
        // Set the read/write timeouts
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;

        _serialPort.Open();
        _continue = true;
        readThread.Start();

        while (_continue)
        {
            Console.WriteLine("New message (4 numbers seperated by comma) or quit:");
            message = Console.ReadLine();

            if (stringComparer.Equals("quit", message))
            {
                _continue = false;
            }
            else
            {
                Byte[] buf = new byte[1 + 3 * enc_length+1];
                String[] numstr = message.Replace(" ", "").Split(new[] {','});
                ulong[] nums = new ulong[4];
                bool argOk = true;
                for (int i = 0; i < numstr.Length; i++)
                {
                    argOk &= ulong.TryParse(numstr[i], out nums[i]);
                }

                if (!argOk || numstr.Length != 4) continue;
                for (int i = 0; i <= enc_length*3; i++)
                {
                    buf[buf.Length - 2 - i] = (Byte)
                        ((((ulong) nums[nums.Length - 1 - i / enc_length]) >> ((i % enc_length) * 8)) & Byte.MaxValue);
                }

                string bytestr = "";
                for (int i = 0; i < buf.Length-1; i++)
                {
                    bytestr += Convert.ToString(buf[i], 2).PadLeft(8, '0') +
                               (i == 0 || i == enc_length || i == enc_length * 2 ? "\n" : " ");
                }

                for (int i = 0; i < buf.Length-1; i++)
                {
                    buf[buf.Length-1] += buf[i];
                }
                Console.WriteLine(bytestr);
                Console.WriteLine("Checksum: " + buf[buf.Length-1]);
                _serialPort.Write(buf, 0, buf.Length);
            }
        }

        readThread.Join();
        _serialPort.Close();
    }

    public static void Read()
    {
        while (_continue)
        {
            try
            {
                string message = _serialPort.ReadLine();
                Console.WriteLine(":" + message);
            }
            catch (TimeoutException)
            {
            }
        }
    }

    public static string GetCOMPort()
    {
        int max = -1;
        do
        {
            foreach (string s in SerialPort.GetPortNames())
                max = ((int) int.Parse((string)s.Replace("COM",""))) > max ? 
                    ((int) int.Parse((string)s.Replace("COM",""))) : 
                    max;
                
            Console.WriteLine(max==-1?"No COM connected. Enter to try again":("Connect to COM"+max+"?[y/n]"));
            if (Console.ReadKey().KeyChar == 'n')
            {
                while (true)
                {
                    Console.WriteLine("Available Ports:");
                    foreach (string s in SerialPort.GetPortNames())
                    {
                        Console.WriteLine("   {0}", s);
                    }
                    Console.Write("\nSpecify other COM\nCOM");
                    max = int.Parse(Console.ReadLine().Replace("COM", ""));
                    foreach (string s in SerialPort.GetPortNames())
                    {
                        if(s.Contains("COM"+max)) return "COM" + max;
                    }
                    Console.WriteLine("No such COM");
                }
                
            }
        } while (max == -1);

        return "COM" + max;
    }
}