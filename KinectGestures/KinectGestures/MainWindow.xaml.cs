using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Forms;
using KinectMouseController;
using System.Runtime.InteropServices;

namespace KinectGestures
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        Skeleton first;
        int latency = 0;
        KinectSensor sensor;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        const float MaxDepthDistance = 4095; // max value returned
        const float MinDepthDistance = 850; // min value returned
        const float MaxDepthDistanceOffset = MaxDepthDistance - MinDepthDistance;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                sensor = KinectSensor.KinectSensors[0];
            }
            if (sensor.Status == KinectStatus.Connected)
            {
                sensor.DepthStream.Enable();
                sensor.ColorStream.Enable();
                sensor.SkeletonStream.Enable();
                sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
                sensor.Start();                
            }
        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
        
            {
            
               if (frame == null)
                {
                    return;
                }
                byte[] pixels = new byte[frame.PixelDataLength];
                int stride = frame.Width * 4;
                frame.CopyPixelDataTo(pixels);
                imagecolor.Source = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);

                Skeleton first = GetFirstSkeleton(e);
                if (first == null)
                {
                    return;
                }
                //set scaled position
                //ScalePosition(lShoulderEllipse, first.Joints[JointType.ShoulderLeft]);
                //ScalePosition(rShoulderEllipse, first.Joints[JointType.ShoulderRight]);
                //ScalePosition(lKneeEllipse, first.Joints[JointType.KneeLeft]);
                //ScalePosition(rKneeEllipse, first.Joints[JointType.KneeRight]);
                //ScalePosition(rHandEllipse, first.Joints[JointType.HandRight]);
                GetCameraPoint(first, e);

            }
        }
        

        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            //Kinect can detect up to 6 skeleton , but we need one
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);           
                 first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();
                return first;
            }
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);

        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null)
                {
                    return;
                }

                //Map a joint location to a point on the depth map
                //Left Shoulder
                DepthImagePoint lShoulderDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.ShoulderLeft].Position);
                //right Shoulder
                DepthImagePoint rShoulderDepthPoint =
                depth.MapFromSkeletonPoint(first.Joints[JointType.ShoulderRight].Position);
                //left Knee
                DepthImagePoint lKneeDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.KneeLeft].Position);
                //right Knee
                DepthImagePoint rKneeDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.KneeRight].Position);
                //right hand
                DepthImagePoint rHandDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);

                DepthImagePoint lHandDepthPoint =
                   depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);

                //mouse events
                //checking for the mouse control gesture
                if (rShoulderDepthPoint.Depth - rHandDepthPoint.Depth > int.Parse(Pointer.Text))
                {
                    var tr = new TextRange(Console.Selection.Start, Console.Selection.End);
                    TextPointer caretPos = tr.Start;
                    Console.CaretPosition = caretPos.DocumentStart;
                    tr.Text = ("pointer" + "\r"); 
                    //Console.AppendText( "Cursor"+ "\r"); 
                    //and if detected now we check for left click gesture
                    if (Math.Abs(rHandDepthPoint.Depth - lHandDepthPoint.Depth) <= 10)
                    {
                        DoMouseClick();
                    }
                    else
                    {
                        ControlWindows();
                    }
                }


                // keyboard events
                // check the latency threshold
                if (latency >= 15)
                {
                    //checking for backward gesture
                    if (rKneeDepthPoint.Depth - lKneeDepthPoint.Depth > int.Parse(Backward.Text))
                    {
                        var tr = new TextRange(Console.Selection.Start, Console.Selection.End);
                        TextPointer caretPos = tr.Start;
                        Console.CaretPosition = caretPos.DocumentStart;
                        tr.Text = ("backward" + "\r"); 
                        SendKeys.SendWait("{DOWN}");
                        latency = 0;
                    }
                    else
                    {
                        if (lKneeDepthPoint.Depth - rKneeDepthPoint.Depth > int.Parse(Forward.Text))
                        {
                            //if not,is it forward gesture ?
                            var tr = new TextRange(Console.Selection.Start, Console.Selection.End);
                            TextPointer caretPos = tr.Start;
                            Console.CaretPosition = caretPos.DocumentStart;
                            tr.Text = ("Forward" + "\r"); 
                            SendKeys.SendWait("{UP}");
                            latency = 0;
                        }
                        else
                        {
                            if (rShoulderDepthPoint.Depth - lShoulderDepthPoint.Depth > int.Parse(Left.Text))
                            {
                                //if not,is it right gesture ?
                                var tr = new TextRange(Console.Selection.Start, Console.Selection.End);
                                TextPointer caretPos = tr.Start;
                                Console.CaretPosition = caretPos.DocumentStart;
                                tr.Text = ("right" + "\r"); 
                                SendKeys.SendWait("{RIGHT}");
                                latency = 0;
                            }
                            else
                            {
                                if (lShoulderDepthPoint.Depth - rShoulderDepthPoint.Depth > int.Parse(Right.Text))
                                {
                                    //if not,is it left gesture ?
                                    var tr = new TextRange(Console.Selection.Start, Console.Selection.End);
                                    TextPointer caretPos = tr.Start;
                                    Console.CaretPosition = caretPos.DocumentStart;
                                    tr.Text = ("left" + "\r"); 
                                    SendKeys.SendWait("{LEFT}");
                                    latency = 0;                 
                                }
                                else
                                    {
                                    //no gesture at all ? it is stand by gesture 
                                        var tr = new TextRange(Console.Selection.Start, Console.Selection.End);
                                        TextPointer caretPos = tr.Start;
                                        Console.CaretPosition = caretPos.DocumentStart;
                                        tr.Text = ("stand by" + "\r"); 
                                        latency = 0;
                                    }
                                }
                            }
                        }
                    }
                
                else
                {
                    latency++;
                }

      //Code that we used to plot the joints on RGB image
                    //Map a depth point to a point on the color image
                    //left shoulder
                    //ColorImagePoint lShoulderColorPoint =
                    //    depth.MapToColorImagePoint(lShoulderDepthPoint.X, lShoulderDepthPoint.Y,
                    //    ColorImageFormat.RgbResolution640x480Fps30);
                    ////right Shoulder
                    //ColorImagePoint rShoulderColorPoint =
                    //    depth.MapToColorImagePoint(rShoulderDepthPoint.X, rShoulderDepthPoint.Y,
                    //    ColorImageFormat.RgbResolution640x480Fps30);
                    ////left Knee
                    //ColorImagePoint lKneeColorPoint =
                    //    depth.MapToColorImagePoint(lKneeDepthPoint.X, lKneeDepthPoint.Y,
                    //    ColorImageFormat.RgbResolution640x480Fps30);
                    ////right Knee
                    //ColorImagePoint rKneeColorPoint =
                    //    depth.MapToColorImagePoint(rKneeDepthPoint.X, rKneeDepthPoint.Y,
                    //    ColorImageFormat.RgbResolution640x480Fps30);

                    //ColorImagePoint rHandColorPoint =
                    //    depth.MapToColorImagePoint(rHandDepthPoint.X, rHandDepthPoint.Y,
                    //ColorImageFormat.RgbResolution640x480Fps30);

                    //Set location
                    //CameraPosition(lShoulderEllipse, lShoulderColorPoint);
                    //CameraPosition(rShoulderEllipse, rShoulderColorPoint);
                    //CameraPosition(lKneeEllipse, lKneeColorPoint);
                    //CameraPosition(rKneeEllipse, rKneeColorPoint);
                    //CameraPosition(rHandEllipse, rHandColorPoint);
                }
            }

        public void DoMouseClick()
        {
            //Call the imported function with the cursor's current position
            
            int X = System.Windows.Forms.Cursor.Position.X;
            int Y = System.Windows.Forms.Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP,(uint) X, (uint)Y, 0, 0);
        }

        private void ControlWindows()
        {
      //Getting the right hand joint and scaled it according to the primary screen size
            Joint scaledRight = first.Joints[JointType.HandRight].ScaleTo((
                    int)SystemInformation.PrimaryMonitorSize.Width,
                    (int)SystemInformation.PrimaryMonitorSize.Height);
            //getting the positin 
                int cursorX = (int)scaledRight.Position.X;
                int cursorY = (int)scaledRight.Position.Y;

            //taking over the cursor
KinectMouseController.KinectMouseMethods.SendMouseInput
    (cursorX, cursorY, (int)SystemInformation.PrimaryMonitorSize.Width,
    (int)SystemInformation.PrimaryMonitorSize.Width, false); 
          }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            Joint scaledJoint = joint.ScaleTo(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Width);
            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);
        }



        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {

            //get the raw data from kinect with the depth for every pixel
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            //use depthFrame to create the image to display on-screen
            //depthFrame contains color information for all pixels in image
            //Height x Width x 4 (Red, Green, Blue, empty byte)
            Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            //hardcoded locations to Blue, Green, Red (BGR) index positions       
            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;


            //loop through all distances
            //pick a RGB color based on distance
            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
                depthIndex++, colorIndex += 4)
            {
                //get the player 
                int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;

                //gets the depth value
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                //.9M or 2.95'
                if (depth <= 900)
                {
                    //we are very close
                    pixels[colorIndex + BlueIndex] = 255;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;

                }
                // .9M - 2M or 2.95' - 6.56'
                else if (depth > 900 && depth < 2000)
                {
                    //we are a bit further away
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 255;
                    pixels[colorIndex + RedIndex] = 0;
                }
                // 2M+ or 6.56'+
                else if (depth > 2000)
                {
                    //we are the farthest
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 255;
                }


                ////equal coloring for monochromatic histogram
                byte intensity = CalculateIntensityFromDepth(depth);
                pixels[colorIndex + BlueIndex] = intensity;
                pixels[colorIndex + GreenIndex] = intensity;
                pixels[colorIndex + RedIndex] = intensity;


                //Color all players "gold"
                if (player > 0)
                {
                    pixels[colorIndex + BlueIndex] = Colors.Gold.B;
                    pixels[colorIndex + GreenIndex] = Colors.Gold.G;
                    pixels[colorIndex + RedIndex] = Colors.Gold.R;
                }

            }


            return pixels;
        }



        public static byte CalculateIntensityFromDepth(int distance)
        {
            //formula for calculating monochrome intensity for histogram
            return (byte)(255 - (255 * Math.Max(distance - MinDepthDistance, 0)
                / (MaxDepthDistanceOffset)));
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            sensor.Stop();
            sensor.AudioSource.Stop();
        }
    }
}
