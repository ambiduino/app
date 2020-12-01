using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Drawing;
using Point = System.Drawing.Point;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Timers;
using System.Threading.Tasks;
using System.IO.Ports;

enum Position
{
    RIGHT,
    TOP,
    LEFT
}

class CustomData
{
    public Position Position;
    public byte[] Colors;
}

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        SerialPort port;

        byte[] allColors = new byte[(11 * 3) + 1];

        public MainWindow()
        {
            InitializeComponent();

            initSerial();

            overTime();
        }
        public void initSerial()
        {
            port = new SerialPort("COM8", 115200);

            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);

            port.Open();

            /*
            var r = Convert.ToByte("0", 10);
            var g = Convert.ToByte("0", 10);
            var b = Convert.ToByte("255", 10);

            char newline = '\n';
            var endln = Convert.ToByte(newline);
            */

            // port.Close();
        }

        public void sendSerial()
        {
            port.Write(allColors, 0, allColors.Length);
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Show all the incoming data in the port's buffer
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            // Console.WriteLine("\nData Received:");
            Console.Write(indata);
        }

        public void overTime()
        {
            Timer aTimer = new Timer();
            aTimer.Elapsed += new ElapsedEventHandler(captureScreen);
            aTimer.Interval = 50;
            aTimer.Enabled = true;
        }

        private Color getColor(Bitmap screen)
        {
            Color c = GetAverageColor(screen);

            return c;
        }

        private void captureScreen(object source, ElapsedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var resolution = getScreenResolution();

            Task<CustomData>[] taskArray = new Task<CustomData>[3];
            for (int i = 0; i < taskArray.Length; i++)
            {
                taskArray[i] = Task<CustomData>.Factory.StartNew((Object obj) => {
                    Position pos = (Position) obj;

                    int resWidth = Convert.ToInt32(resolution.width);
                    int resHeight = Convert.ToInt32(resolution.height);

                    int width;
                    int height;
                    int zoneWidth;
                    int zoneHeight;
                    int x = 0;
                    int y = 0;
                    int numberOfZones = 0;

                    if (pos == Position.LEFT || pos == Position.RIGHT)
                    {
                        width = 250;
                        height = resHeight;
                        numberOfZones = 3;

                        x = pos == Position.LEFT ? 0 : resWidth - 250;

                        zoneWidth = width;
                        zoneHeight = height / numberOfZones;
                    }
                    else
                    {
                        width = resWidth;
                        height = 250;
                        numberOfZones = 5;

                        zoneWidth = width / numberOfZones;
                        zoneHeight = height;
                    }

                    var screen = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var graphics = Graphics.FromImage(screen);

                    graphics.CopyFromScreen(
                        new Point(x, y),
                        new Point(0, 0),
                        new System.Drawing.Size(width, height)
                    );

                    var zoneScreen = new Bitmap(zoneWidth, zoneHeight, PixelFormat.Format32bppArgb);
                    var zoneGraphics = Graphics.FromImage(zoneScreen);
                    
                    byte[] colors = new byte[numberOfZones * 3];

                    for (int j = 0; j < numberOfZones; j += 1)
                    {
                        int recWidth = pos == Position.TOP ? zoneWidth * j : 0;
                        int recHeight = pos == Position.TOP ? 0 : zoneHeight * j;

                        zoneGraphics.DrawImage(screen, 0, 0, new Rectangle(recWidth, recHeight, zoneWidth, zoneHeight), GraphicsUnit.Pixel);

                        Color c = getColor(zoneScreen);

                        // Console.WriteLine(c.ToString());

                        int zoneNumber = (numberOfZones - 1) - j;

                        if (pos == Position.LEFT)
                        {
                            zoneNumber = j;
                        }

                        if (c.R < 100 && c.G < 100 && c.B < 100)
                        {
                            colors[zoneNumber * 3] = 0;
                            colors[(zoneNumber * 3) + 1] = 0;
                            colors[(zoneNumber * 3) + 2] = 0;
                        }
                        else
                        {
                            colors[zoneNumber * 3] = c.R;
                            colors[(zoneNumber * 3) + 1] = c.G;
                            colors[(zoneNumber * 3) + 2] = c.B;
                        }

                    }

                    return new CustomData() { Position = pos, Colors = colors };
                }, i);
            }
            Task.WaitAll(taskArray);

            foreach (var task in taskArray)
            {
                CustomData data = task.Result;

                if (data.Position == Position.RIGHT)
                {
                    Array.Copy(data.Colors, 0, allColors, 0, data.Colors.Length);
                }
                else if (data.Position == Position.TOP)
                {
                    Array.Copy(data.Colors, 0, allColors, 3 * 3, data.Colors.Length);
                }
                else if (data.Position == Position.LEFT)
                {
                    Array.Copy(data.Colors, 0, allColors, 8 * 3, data.Colors.Length);
                }

                /*
                Console.WriteLine(data.Position);

                for (int i = 0; i < data.Colors.Length; i++)
                {
                    Console.WriteLine(data.Colors[i]);
                }

                Console.WriteLine("=====================SSS===================");

                for (int i = 0; i < allColors.Length; i++)
                {
                    Console.WriteLine(allColors[i]);
                }

                Console.WriteLine("=====================\n");
                */
            }

            allColors[allColors.Length - 1] = Convert.ToByte("255", 10); ;

            sendSerial();

            /*
            for (int i = 0; i < allColors.Length; i++)
            {
                Console.WriteLine(allColors[i]);
            }
            */

            /*

            int width = 3440;
            int height = 300;

            var screen = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var gfx = Graphics.FromImage(screen);

            gfx.CopyFromScreen(
                new Point(0, 100),
                new Point(0, 0),
                new System.Drawing.Size(width, height)
            );

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            var screen1 = new Bitmap(144, height, PixelFormat.Format32bppArgb);
            var gfx1 = Graphics.FromImage(screen1);

            for (int i = 0; i < 24; i += 1)
            {
                gfx1.DrawImage(screen, 0, 0, new Rectangle(144 * i, 0, 144, 300), GraphicsUnit.Pixel);

                Color c = getColor(screen1);

                // Debug.Print(c.ToString());

                allColors[(23 - i) * 3] = c.R;
                allColors[((23 - i) * 3) + 1] = c.G;
                allColors[((23 - i) * 3) + 2] = c.B;
            }

            colorData[colorData.Length - 1] = Convert.ToByte("255", 10); ;

            sendSerial();


            gfx1.Dispose();

            sw1.Stop();
            Console.WriteLine("Elapsed inside ={0}", sw1.ElapsedMilliseconds);

            */

            /*

            Task[] taskArray = new Task[8];

            for (int i = 10; i < 17; i += 1)
            {
                taskArray[i] = Task.Factory.StartNew((Object obj) => {
                    Stopwatch sw1 = new Stopwatch();
                    sw1.Start();

                    CustomData data = obj as CustomData;

                    var screen = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var gfx = Graphics.FromImage(screen);

                    gfx.CopyFromScreen(
                        new Point(data.X, data.Y),
                        new Point(0, 0),
                        new System.Drawing.Size(data.Width, data.Height)
                    );

                    getColor(screen);

                    sw1.Stop();
                    Console.WriteLine("Elapsed inside ={0}", sw1.ElapsedMilliseconds);

                }, new CustomData() {
                    X = width * i,
                    Y = 0,
                    Width = width,
                    Height = height
                });
            }

            Task.WaitAll(taskArray);

            */

            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.ElapsedMilliseconds);

            Debug.Print("============================================\n");

            // Debug.Print(resolution.width.ToString());
            // Debug.Print(resolution.height.ToString());
        }

        public (double width, double height) getScreenResolution()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            return (screenWidth, screenHeight);
        }

        public unsafe Color GetAverageColor(Bitmap image, int sampleStep = 1)
        {
            var data = image.LockBits(
                new Rectangle(Point.Empty, image.Size),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var row = (int*)data.Scan0.ToPointer();
            var (sumR, sumG, sumB) = (0L, 0L, 0L);
            var stride = data.Stride / sizeof(int) * sampleStep;

            for (var y = 0; y < data.Height; y += sampleStep)
            {
                for (var x = 0; x < data.Width; x += sampleStep)
                {
                    var argb = row[x];
                    sumR += (argb & 0x00FF0000) >> 16;
                    sumG += (argb & 0x0000FF00) >> 8;
                    sumB += argb & 0x000000FF;
                }
                row += stride;
            }

            image.UnlockBits(data);

            var numSamples = data.Width / sampleStep * data.Height / sampleStep;
            var avgR = sumR / numSamples;
            var avgG = sumG / numSamples;
            var avgB = sumB / numSamples;
            return Color.FromArgb((int)avgR, (int)avgG, (int)avgB);
        }
    }
}
