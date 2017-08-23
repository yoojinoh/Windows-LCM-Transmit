using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.DirectX.DirectInput;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using LCM;

namespace joystick_csharp
{
    public partial class Form1 : Form
    {
        private Joystick jst;
        private Joystick jst_prev;
        //private Joystick jst2;

        LogitechGSDK.LogiControllerPropertiesData logi_properties;
        //private Joystick jst3;
        public bool[] previousButtonValues;

        public string[] sticks;

        public string[] selected_sticks;   //array of joysticks in selected order
        private int stick_count;

        bool forwardReverse = true;
        byte[] received = new byte[57];
        int encoderInit = 0;
        bool sendLCM_enable = false;


        double prev_pedalInput = 0;
        double new_pedalInput = 0;
        Stopwatch stopwatch = new Stopwatch();

        // sender
        LCM.LCM.LCM senderLCM = new LCM.LCM.LCM("udpm://239.255.76.67:7667?ttl=1");
        wincontrollerlcm.windows_controller_data_t send_msg = new wincontrollerlcm.windows_controller_data_t();

        // receiver
        LCM.LCM.LCM receiverLCM = new LCM.LCM.LCM("udpm://239.255.76.67:7667?ttl=1");
        exlcm.example_t receive_msg = new exlcm.example_t();


        FeedbackSubscriber fsc = new FeedbackSubscriber();
        GlobalArray GAA = new GlobalArray();
        private NestedContainer m_Nested;

        public Form1()
        {
            InitializeComponent();

            this.serialPort1.PortName = "COM19";
            this.serialPort1.BaudRate = 9600;
            this.serialPort1.Parity = Parity.None;

            m_Nested = new NestedContainer(this);

            comboBox1.SelectedIndex = 0; ///newly added

            send_msg.mode = 0;
            send_msg.impedance_scale = 0; ////newly added
            send_msg.balance_scale = 0;   ////newly added

            send_msg.enable = 1;
            send_msg.emergency_damp = 0;

            send_msg.p_des = new double[3];
            send_msg.v_des = new double[3];

            send_msg.rpy_des = new double[3];
            send_msg.omega_des = new double[3];

            send_msg.p_des_slew_min = new double[3];
            send_msg.p_des_slew_max = new double[3];
            send_msg.rpy_des_slew_max = new double[3];
            send_msg.v_des_slew_min = new double[3];
            send_msg.v_des_slew_max = new double[3];
            send_msg.omegab_des_slew_max = new double[3];

            send_msg.bonus_knee_torque = 0;

            send_msg.p_des[2] = 0.54;

            send_msg.p_des_slew_min[0] = -0.2;
            send_msg.p_des_slew_min[1] = -0.2;
            send_msg.p_des_slew_min[2] = -0.2;

            send_msg.p_des_slew_max[0] = 0.2;
            send_msg.p_des_slew_max[1] = 0.2;
            send_msg.p_des_slew_max[2] = 0.2;

            send_msg.v_des_slew_min[0] = -0.2;
            send_msg.v_des_slew_min[1] = -0.1;
            send_msg.v_des_slew_min[2] = -0.1;

            send_msg.v_des_slew_max[0] = 0.2;
            send_msg.v_des_slew_max[1] = 0.1;
            send_msg.v_des_slew_max[2] = 0.1;
          
            send_msg.omegab_des_slew_max[0] = 0.3;
            send_msg.omegab_des_slew_max[1] = 0.3;
            send_msg.omegab_des_slew_max[2] = 0.3;

            send_msg.rpy_des_slew_max[0] = 1.6;
            send_msg.rpy_des_slew_max[1] = 1.6;
            send_msg.rpy_des_slew_max[2] = 1.6;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // grab the joystick
            jst = new Joystick(this.Handle);
            jst_prev = new Joystick(this.Handle);

            sticks = jst.FindJoysticks();

            if (sticks != null)
            {
                for (int i = 0; i < sticks.Length; i++)
                {
                    stick_count = sticks.Length;
                    jst_name.Text = sticks[0];
                }
            }
            else MessageBox.Show("No joystick connected!");
        }


        // lcm receiver codes
        internal class FeedbackSubscriber : LCM.LCM.LCMSubscriber
        {
            private Form1 m_parent;

            protected Form1 Parent
            {
                get { return m_parent; }
            }
            public double[] receivedDataddd = new double[10];  // it's important to be public

            GlobalArray GA = new GlobalArray();
            public void MessageReceived(LCM.LCM.LCM lcm, string channel, LCM.LCM.LCMDataInputStream dins)
            {
                exlcm.example_t msg = new exlcm.example_t(dins);
                receivedDataddd[0] = msg.position[0];
            }
            public void saveFeedback()
            {
                this.Parent.label5.Text = receivedDataddd[0].ToString();
            }
        }
        public class GlobalArray
        {
            public double[] receivedData = new double[10];  // it's important to be public

        }
        void Start()
        {
            LogitechGSDK.LogiSteeringInitialize(false);

            logi_properties.wheelRange = 300;
            logi_properties.forceEnable = true;
            logi_properties.overallGain = 80;
            logi_properties.springGain = 80;
            logi_properties.damperGain = 80;
            logi_properties.allowGameSettings = true;
            logi_properties.combinePedals = false;
            logi_properties.defaultSpringEnabled = true;
            logi_properties.defaultSpringGain = 80;

            logi_properties.defaultSpringEnabled = false;

            LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);
            initializeGraph3();

        }

        public static double ConvertRange(int originalStart, int originalEnd, // original range
                                      double newStart, double newEnd, // desired range
                                     double value) // value to convert
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (double)(newStart + ((value - originalStart) * scale));
        }

        private void tmrUpdateStick_Tick(object sender, EventArgs e)
        {
            this.UpdateControllerState();
            send_serial_command();
            if (sendLCM_enable == true)
            {

                senderLCM.Publish("YOOJIN_windows_controller_data", send_msg);
                receiverLCM.SubscribeAll(new FeedbackSubscriber());
            }
        }
        private void UpdateControllerState()
        {
            // get status

            if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
            {
                LogitechGSDK.LogiGetCurrentControllerProperties(0, ref logi_properties);

                label5.Text = logi_properties.wheelRange.ToString();
            }

            bool pressed = jst.Buttons[4];
            previousButtonValues = jst.Buttons;
            jst.UpdateStatus();
            if (jst.Buttons[4] && !previousButtonValues[4] == true)
            {
                if (forwardReverse == true)
                {
                    forwardReverse = false;
                    label8.Text = "Reverse";
                }
                else
                {
                    forwardReverse = true;
                    label8.Text = "Forward";
                }
            }
            label_brakeVal.Text = jst.AxisA.ToString();
            label_wheelVal.Text = jst.AxisC.ToString();
            label_accelVal.Text = jst.AxisD.ToString();

            //forceFeedback2();
            forceFeedback4();


        }

        private void send_serial_command()
        {
            int new_wheelVal = System.Convert.ToInt32(Math.Floor(ConvertRange(0, 65535, 0, 180, jst.AxisC))); //Wheel value convert
            int new_accelVal = System.Convert.ToInt32(Math.Floor(ConvertRange(0, 65535, 255, 0, jst.AxisD))); //Accel value convert
            int new_brakeVal = System.Convert.ToInt32(Math.Floor(ConvertRange(0, 65535, 255, 0, jst.AxisA))); //Brake value convert

            // try { this.serialPort1.Open(); }
            //catch {  };


            if (serialPort1.IsOpen)
            {
                label2.Text = "Connected";

                byte[] b = new byte[5];
                b[0] = Convert.ToByte(forwardReverse);
                b[1] = Convert.ToByte(new_wheelVal);
                b[2] = Convert.ToByte(new_accelVal);
                b[3] = Convert.ToByte(new_brakeVal);
                b[4] = Convert.ToByte(encoderInit);


                //b[4] = Convert.ToByte(jst.AxisC % 10);
                //b[5] = Convert.ToByte(1);
                //b[6] = Convert.ToByte(10);
                //b[7] = Convert.ToByte(255);
                this.serialPort1.Write(b, 0, b.Length);
                encoderInit = 0;

                //serialPort1.DiscardInBuffer();

                //try
                //{
                //    if (serialPort1.BytesToRead >= 0)
                //    {
                //        serialPort1.Read(received, 0, serialPort1.BytesToRead);
                //        Thread.Sleep(10);
                //    }
                //    Invoke(new EventHandler(dataReceive));
                //}
                //catch (System.Exception ex)
                //{ }

                //serialPort1.Read(received, 0,4);
                //this.serialPort1.Close();
                //label10.Text = (received[0] * 256 + received[1]).ToString();
                //label11.Text = (received[2] * 256 + received[3]).ToString();
            }
            else
            {
                this.label2.ForeColor = System.Drawing.Color.Red;
                label2.Text = "Disconnected";
            }
        }


        private void button_start_Click(object sender, EventArgs e)  // start button
        {
            Start();

            System.Threading.Thread.Sleep(50);
            // start updating positions
            tmrUpdateStick.Enabled = true;
            try { this.serialPort1.Open(); }
            catch { };

            encoderInit = 1;

            jst.AcquireJoystick(sticks[0]);


            label32.Text = jst.AxisCount.ToString();
            label33.Text = jst.btnCount.ToString();


        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            // v_des
            send_msg.v_des[0] = System.Convert.ToDouble(numericUpDown1.Value);
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            // v_des_slew
            send_msg.v_des_slew_min[0] = System.Convert.ToDouble(numericUpDown2.Value);
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            send_msg.v_des_slew_max[0] = System.Convert.ToDouble(numericUpDown5.Value);
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            // w_des
            send_msg.omega_des[2] = System.Convert.ToDouble(numericUpDown3.Value) * System.Math.PI / 180;
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            // w_des_slew
            send_msg.omegab_des_slew_max[2] = System.Convert.ToDouble(numericUpDown4.Value);
        }
        private void button_B_Click(object sender, EventArgs e)
        {
            if (LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_SOFTSTOP))
            {
                LogitechGSDK.LogiStopSoftstopForce(0);
            }
            else
            {
                LogitechGSDK.LogiPlaySoftstopForce(0, 10);
            }


            //if (LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_SPRING))
            //{
            //    LogitechGSDK.LogiStopSpringForce(0);
            //}
            //else
            //{
            //    LogitechGSDK.LogiPlaySpringForce(0, 0, 100, 30);
            //}

            //if (LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_DAMPER))
            //{
            //    LogitechGSDK.LogiStopDamperForce(0);
            //}
            //else
            //{
            //    LogitechGSDK.LogiPlayDamperForce(0, 50);
            //}


        }
        void initializeGraph()
        {
            /// case 1 ///
            chart1.Series["state1"].Points.Clear();
            chart1.Series["state1"].Points.AddXY(-150, 100);
            chart1.Series["state1"].Points.AddXY(-100, 0);
            chart1.Series["state1"].Points.AddXY(100, 0);
            chart1.Series["state1"].Points.AddXY(150, 100);

            /// case 2 ///
            chart1.Series["state2"].Points.Clear();
            chart1.Series["state2"].Points.AddXY(-150, 100);
            chart1.Series["state2"].Points.AddXY(-80, 10);
            chart1.Series["state2"].Points.AddXY(80, 10);
            chart1.Series["state2"].Points.AddXY(150, 100);

            /// case 3 ///
            chart1.Series["state3"].Points.Clear();
            chart1.Series["state3"].Points.AddXY(-150, 100);
            chart1.Series["state3"].Points.AddXY(-60, 20);
            chart1.Series["state3"].Points.AddXY(60, 20);
            chart1.Series["state3"].Points.AddXY(150, 100);

            /// case 4 ///
            chart1.Series["state4"].Points.Clear();
            chart1.Series["state4"].Points.AddXY(-150, 100);
            chart1.Series["state4"].Points.AddXY(-40, 30);
            chart1.Series["state4"].Points.AddXY(40, 30);
            chart1.Series["state4"].Points.AddXY(150, 100);

            /// case 5 ///
            chart1.Series["state5"].Points.Clear();
            chart1.Series["state5"].Points.AddXY(-150, 100);
            chart1.Series["state5"].Points.AddXY(-20, 40);
            chart1.Series["state5"].Points.AddXY(20, 40);
            chart1.Series["state5"].Points.AddXY(150, 100);
        }
        void initializeGraph2()
        {
            /// case 1 ///
            chart1.Series["state1"].Points.Clear();
            chart1.Series["state1"].Points.AddXY(-150, 100);
            chart1.Series["state1"].Points.AddXY(-150, 50);
            chart1.Series["state1"].Points.AddXY(-105, 0);
            chart1.Series["state1"].Points.AddXY(105, 0);
            chart1.Series["state1"].Points.AddXY(150, 50);
            chart1.Series["state1"].Points.AddXY(150, 100);

            /// case 2 ///
            chart1.Series["state2"].Points.Clear();
            chart1.Series["state2"].Points.AddXY(-137.5, 100);
            chart1.Series["state2"].Points.AddXY(-137.5, 50);
            chart1.Series["state2"].Points.AddXY(-96.25, 0);
            chart1.Series["state2"].Points.AddXY(96.25, 0);
            chart1.Series["state2"].Points.AddXY(137.5, 50);
            chart1.Series["state2"].Points.AddXY(137.5, 100);

            /// case 3 ///
            chart1.Series["state3"].Points.Clear();
            chart1.Series["state3"].Points.AddXY(-125, 100);
            chart1.Series["state3"].Points.AddXY(-125, 50);
            chart1.Series["state3"].Points.AddXY(-87.5, 00);
            chart1.Series["state3"].Points.AddXY(87.5, 00);
            chart1.Series["state3"].Points.AddXY(125, 50);
            chart1.Series["state3"].Points.AddXY(125, 100);

            /// case 4 ///
            chart1.Series["state4"].Points.Clear();
            chart1.Series["state4"].Points.AddXY(-115, 100);
            chart1.Series["state4"].Points.AddXY(-115, 50);
            chart1.Series["state4"].Points.AddXY(-80.5, 0);
            chart1.Series["state4"].Points.AddXY(80.5, 0);
            chart1.Series["state4"].Points.AddXY(115, 50);
            chart1.Series["state4"].Points.AddXY(115, 100);

            /// case 5 ///
            chart1.Series["state5"].Points.Clear();
            chart1.Series["state5"].Points.AddXY(-102.5, 100);
            chart1.Series["state5"].Points.AddXY(-102.5, 50);
            chart1.Series["state5"].Points.AddXY(-71.75, 0);
            chart1.Series["state5"].Points.AddXY(71.75, 00);
            chart1.Series["state5"].Points.AddXY(102.5, 50);
            chart1.Series["state5"].Points.AddXY(102.5, 100);

            /// case 6 ///
            chart1.Series["state6"].Points.Clear();
            chart1.Series["state6"].Points.AddXY(-72.5, 100);
            chart1.Series["state6"].Points.AddXY(-72.5, 50);
            chart1.Series["state6"].Points.AddXY(-50.75, 0);
            chart1.Series["state6"].Points.AddXY(50.75, 00);
            chart1.Series["state6"].Points.AddXY(72.5, 50);
            chart1.Series["state6"].Points.AddXY(72.5, 100);

            /// case 7 ///
            chart1.Series["state7"].Points.Clear();
            chart1.Series["state7"].Points.AddXY(-65, 100);
            chart1.Series["state7"].Points.AddXY(-65, 50);
            chart1.Series["state7"].Points.AddXY(-45.5, 0);
            chart1.Series["state7"].Points.AddXY(45.5, 00);
            chart1.Series["state7"].Points.AddXY(65, 50);
            chart1.Series["state7"].Points.AddXY(65, 100);


            chart2.Series["reference"].Points.AddXY(0, 0);
            chart2.Series["reference"].Points.AddXY(1, 40);

            chart2.Series["stiff1"].Points.AddXY(0.075, 3);
            chart2.Series["stiff2"].Points.AddXY(0.15, 6);
            chart2.Series["stiff3"].Points.AddXY(0.45, 18);
            chart2.Series["stiff4"].Points.AddXY(0.575, 23);
            chart2.Series["stiff5"].Points.AddXY(0.675, 27);
            chart2.Series["stiff6"].Points.AddXY(0.75, 30);
            chart2.Series["stiff7"].Points.AddXY(0.875, 35);

        }
        void initializeGraph3()
        {
            /// case 1 ///
            chart1.Series["state1"].Points.Clear();
            chart1.Series["state1"].Points.AddXY(-150, 100);
            //chart1.Series["state1"].Points.AddXY(-150, 50);
            chart1.Series["state1"].Points.AddXY(-100, 0);
            chart1.Series["state1"].Points.AddXY(100, 0);
            //chart1.Series["state1"].Points.AddXY(150, 50);
            chart1.Series["state1"].Points.AddXY(150, 100);

            /// case 2 ///
            chart1.Series["state2"].Points.Clear();
            chart1.Series["state2"].Points.AddXY(-150, 100);

            //chart1.Series["state2"].Points.AddXY(-137.5, 50);
            chart1.Series["state2"].Points.AddXY(-96, 0);
            chart1.Series["state2"].Points.AddXY(96, 0);
            //chart1.Series["state2"].Points.AddXY(137.5, 50);
            chart1.Series["state2"].Points.AddXY(150, 100);

            /// case 3 ///
            chart1.Series["state3"].Points.Clear();
            chart1.Series["state3"].Points.AddXY(-150, 100);

            //chart1.Series["state3"].Points.AddXY(-125, 50);
            chart1.Series["state3"].Points.AddXY(-87, 00);
            chart1.Series["state3"].Points.AddXY(87, 00);
            //chart1.Series["state3"].Points.AddXY(125, 50);
            chart1.Series["state3"].Points.AddXY(150, 100);

            /// case 4 ///
            chart1.Series["state4"].Points.Clear();
            chart1.Series["state4"].Points.AddXY(-150, 100);

            //chart1.Series["state4"].Points.AddXY(-115, 50);
            chart1.Series["state4"].Points.AddXY(-81, 0);
            chart1.Series["state4"].Points.AddXY(81, 0);
            //chart1.Series["state4"].Points.AddXY(115, 50);
            chart1.Series["state4"].Points.AddXY(150, 100);


            /// case 5 ///
            chart1.Series["state5"].Points.Clear();
            chart1.Series["state5"].Points.AddXY(-150, 100);

            //chart1.Series["state5"].Points.AddXY(-102.5, 50);
            chart1.Series["state5"].Points.AddXY(-72, 0);
            chart1.Series["state5"].Points.AddXY(72, 00);
            //chart1.Series["state5"].Points.AddXY(102.5, 50);
            chart1.Series["state5"].Points.AddXY(150, 100);


            /// case 6 ///
            chart1.Series["state6"].Points.Clear();
            chart1.Series["state6"].Points.AddXY(-150, 100);

            //chart1.Series["state6"].Points.AddXY(-72.5, 50);
            chart1.Series["state6"].Points.AddXY(-51, 0);
            chart1.Series["state6"].Points.AddXY(51, 00);
            //chart1.Series["state6"].Points.AddXY(72.5, 50);
            chart1.Series["state6"].Points.AddXY(150, 100);


            /// case 7 ///
            chart1.Series["state7"].Points.Clear();
            chart1.Series["state7"].Points.AddXY(-150, 100);

            //chart1.Series["state7"].Points.AddXY(-65, 50);
            chart1.Series["state7"].Points.AddXY(-45, 0);
            chart1.Series["state7"].Points.AddXY(45, 00);
            //chart1.Series["state7"].Points.AddXY(65, 50);
            chart1.Series["state7"].Points.AddXY(150, 100);



            chart2.Series["reference"].Points.AddXY(0, 0);
            chart2.Series["reference"].Points.AddXY(1, 40);

            chart2.Series["stiff1"].Points.AddXY(0.075, 3);
            chart2.Series["stiff2"].Points.AddXY(0.15, 6);
            chart2.Series["stiff3"].Points.AddXY(0.45, 18);
            chart2.Series["stiff4"].Points.AddXY(0.575, 23);
            chart2.Series["stiff5"].Points.AddXY(0.675, 27);
            chart2.Series["stiff6"].Points.AddXY(0.75, 30);
            chart2.Series["stiff7"].Points.AddXY(0.875, 35);

        }

        void forceFeedback()
        {
            double wheelPosition = 0;
            LogitechGSDK.LogiPlaySpringForce(0, 0, 100, 20);

            wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);

            chart1.Series["Current Position"].Points.Clear();

            if (jst.AxisD > 40000)
            {
                LogitechGSDK.LogiPlaySoftstopForce(0, 67);
                LogitechGSDK.LogiStopDamperForce(0);

                if (wheelPosition > 100)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 2 - 200);

                }
                else if (wheelPosition <= 100 && wheelPosition >= -100)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -100)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-2) - 200);


                }
            }
            else if (jst.AxisD <= 40000 && jst.AxisD > 30000)
            {
                LogitechGSDK.LogiPlaySoftstopForce(0, 8000 / 150);
                LogitechGSDK.LogiPlayDamperForce(0, 10);

                if (wheelPosition > 80)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 9 / 7 + 10 - 720 / 7);
                }
                else if (wheelPosition <= 80 && wheelPosition >= -80)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 10);
                }
                else if (wheelPosition < -80)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * -9 / 7 + 10 - 720 / 7);
                }
            }
            else if (jst.AxisD <= 30000 && jst.AxisD > 20000)
            {
                LogitechGSDK.LogiPlaySoftstopForce(0, 6000 / 150);
                LogitechGSDK.LogiPlayDamperForce(0, 20);

                if (wheelPosition > 60)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 8 / 9 + 20 - 480 / 9);
                }
                else if (wheelPosition <= 60 && wheelPosition >= -60)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 20);
                }
                else if (wheelPosition < -60)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * -8 / 9 + 20 - 480 / 9);
                }
            }
            else if (jst.AxisD <= 20000 && jst.AxisD > 10000)
            {
                LogitechGSDK.LogiPlaySoftstopForce(0, 4000 / 150);
                LogitechGSDK.LogiPlayDamperForce(0, 30);

                if (wheelPosition > 40)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 7 / 11 + 30 - 280 / 11);
                }
                else if (wheelPosition <= 40 && wheelPosition >= -40)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 30);
                }
                else if (wheelPosition < -40)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * -7 / 11 + 30 - 280 / 11);
                }
            }
            else if (jst.AxisD <= 10000)
            {
                LogitechGSDK.LogiPlaySoftstopForce(0, 90);
                LogitechGSDK.LogiPlayDamperForce(0, 40);
                //LogitechGSDK.LogiStopSoftstopForce(0);

                if (wheelPosition > 20)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 6 / 13 + 40 - 120 / 13);
                }
                else if (wheelPosition <= 20 && wheelPosition >= -20)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 40);
                }
                else if (wheelPosition < -20)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * -6 / 13 + 40 - 120 / 13);
                }
                logi_properties.wheelRange = 60;

                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

            }


        }
        void forceFeedback2()
        {
            chart1.Series["Current Position"].Points.Clear();

            double inversePedalInput = ConvertRange(0, 65535, 65535, 0, jst.AxisD);
            label_accelVal.Text = inversePedalInput.ToString();
            chart2.Series["Current State"].Points.Clear();

            if (inversePedalInput <= 200)
            {
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);

                chart2.Series["Current State"].Points.AddXY(0, 0);
                logi_properties.wheelRange = 300; //210deg w/o force
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);
                LogitechGSDK.LogiStopDamperForce(0);
                LogitechGSDK.LogiStopSoftstopForce(0);
                chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                send_msg.omega_des[2] = -0.3133333333 * wheelPosition * System.Math.PI / 180;

               // send_msg.omega_des[2] = Math.Round(-0.3133 * wheelPosition * System.Math.PI / 180,2)-0.01;
                

            }
            else if (inversePedalInput < 10000 && inversePedalInput > 200)
            {
                chart2.Series["Current State"].Points.AddXY(0.075, 3);
                logi_properties.wheelRange = 300; //210deg w/o force
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);
                LogitechGSDK.LogiPlayDamperForce(0, 3);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);

                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 105 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.1111 - 116.67);
                }
                else if (wheelPosition <= 105 && wheelPosition >= -105)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -105 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.1111) - 116.67);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);
                }

                send_msg.v_des[0] = 0.1;
                send_msg.omega_des[2] = -0.2466666667 * wheelPosition * System.Math.PI / 180;
            }
            else if (inversePedalInput >= 10000 && inversePedalInput < 20000)
            {
                chart2.Series["Current State"].Points.AddXY(0.15, 6);

                logi_properties.wheelRange = 275; ///192.5
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 6);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);
                double wheelPosition = ConvertRange(0, 65535, -137.5, 137.5, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 87.5 && wheelPosition < 136.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.2121 - 116.67);
                }
                else if (wheelPosition <= 96.25 && wheelPosition >= -87.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -96.25 && wheelPosition > -136.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.2121) - 116.67);
                }
                else if (wheelPosition >= 136.5 || wheelPosition <= -136.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);
                }
                send_msg.v_des[0] = 0.2;
                send_msg.omega_des[2] = -0.232727273 * wheelPosition * System.Math.PI / 180;


            }

            else if (inversePedalInput >= 20000 && inversePedalInput < 30000)
            {
                chart2.Series["Current State"].Points.AddXY(0.45, 18);

                logi_properties.wheelRange = 250; ///175
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 18);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);
                double wheelPosition = ConvertRange(0, 65535, -125, 125, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 87.5 && wheelPosition < 124)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.3333 - 116.67);
                }
                else if (wheelPosition <= 87.5 && wheelPosition >= -87.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -87.5 && wheelPosition > -124)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.3333) - 116.67);
                }
                else if (wheelPosition >= 124 || wheelPosition <= -124)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.3;
                send_msg.omega_des[2] = -0.232 * wheelPosition * System.Math.PI / 180;


            }
            else if (inversePedalInput >= 30000 && inversePedalInput < 40000)
            {
                chart2.Series["Current State"].Points.AddXY(0.575, 23);

                logi_properties.wheelRange = 230; ///161
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 23);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);
                double wheelPosition = ConvertRange(0, 65535, -115, 115, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 80.5 && wheelPosition < 114)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.4493 - 116.67);
                }
                else if (wheelPosition <= 80.5 && wheelPosition >= -80.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -80.5 && wheelPosition > -114)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.4493) - 116.67);
                }
                else if (wheelPosition >= 114 || wheelPosition <= -114)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.5;
                send_msg.omega_des[2] = -0.21739130434782608695652173913043 * wheelPosition * System.Math.PI / 180;


            }
            else if (inversePedalInput >= 40000 && inversePedalInput < 50000)
            {
                chart2.Series["Current State"].Points.AddXY(0.675, 27);

                logi_properties.wheelRange = 205; //143.5
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 27);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);
                double wheelPosition = ConvertRange(0, 65535, -102.5, 102.5, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 71.75 && wheelPosition < 101.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.626 - 116.67);
                }
                else if (wheelPosition <= 71.75 && wheelPosition >= -71.75)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -71.75 && wheelPosition > -101.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.626) - 116.67);
                }
                else if (wheelPosition >= 101.5 || wheelPosition <= -101.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.7;
                send_msg.omega_des[2] = -0.21463414634146341463414634146341 * wheelPosition * System.Math.PI / 180;

            }
            else if (inversePedalInput >= 50000 && inversePedalInput < 60000)
            {
                chart2.Series["Current State"].Points.AddXY(0.75, 30);

                logi_properties.wheelRange = 145; //101.5
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 30);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);
                double wheelPosition = ConvertRange(0, 65535, -72.5, 72.5, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 50.75 && wheelPosition < 71.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 2.2989 - 116.67);
                }
                else if (wheelPosition <= 50.75 && wheelPosition >= -50.75)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -50.75 && wheelPosition > -71.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-2.2989) - 116.67);
                }
                else if (wheelPosition >= 71.5 || wheelPosition <= -71.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);




                }
                send_msg.v_des[0] = 0.8;
                send_msg.omega_des[2] = -0.12413793103448275862068965517241 * wheelPosition * System.Math.PI / 180;

            }
            else if (inversePedalInput >= 60000)
            {
                chart2.Series["Current State"].Points.AddXY(0.875, 35);

                logi_properties.wheelRange = 130; //91 
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 35);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);
                double wheelPosition = ConvertRange(0, 65535, -65, 65, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 45.5 && wheelPosition < 64)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 2.5641 - 116.67);
                }
                else if (wheelPosition <= 45.5 && wheelPosition >= -45.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -45.5 && wheelPosition > -64)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-2.5641) - 116.67);
                }
                else if (wheelPosition >= 64 || wheelPosition <= -64)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.9;
                send_msg.omega_des[2] = -0.06153846153846153846153846153846 * wheelPosition * System.Math.PI / 180;

            }
        }
        void forceFeedback3()
        {
            chart1.Series["Current Position"].Points.Clear();

            double inversePedalInput = ConvertRange(0, 65535, 65535, 0, jst.AxisD);
            label_accelVal.Text = inversePedalInput.ToString();
            chart2.Series["Current State"].Points.Clear();

            if (inversePedalInput <= 200)
            {
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);

                chart2.Series["Current State"].Points.AddXY(0, 0);
                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);
                LogitechGSDK.LogiStopDamperForce(0);
                LogitechGSDK.LogiStopSoftstopForce(0);
                chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180,2);
                label40.Text = send_msg.omega_des[2].ToString();
               

            }
            else if (inversePedalInput < 10000 && inversePedalInput > 200)
            {
                chart2.Series["Current State"].Points.AddXY(0.075, 3);
                logi_properties.wheelRange = 300; //210deg w/o force
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);
                LogitechGSDK.LogiPlayDamperForce(0, 3);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);

                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 105 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.1111 - 116.67);
                }
                else if (wheelPosition <= 105 && wheelPosition >= -105)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -105 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.1111) - 116.67);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);
                }

                send_msg.v_des[0] = 0.1;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 10000 && inversePedalInput < 20000)
            {
                chart2.Series["Current State"].Points.AddXY(0.15, 6);

                logi_properties.wheelRange = 300; 
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 6);
                LogitechGSDK.LogiPlaySoftstopForce(0, 64);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 96.25 && wheelPosition < 136.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.2121 - 116.67);
                }
                else if (wheelPosition <= 96.25 && wheelPosition >= -96.25)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -96.25 && wheelPosition > -136.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.2121) - 116.67);
                }
                else if (wheelPosition >= 136.5 || wheelPosition <= -136.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);
                }
                send_msg.v_des[0] = 0.2;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }

            else if (inversePedalInput >= 20000 && inversePedalInput < 30000)
            {
                chart2.Series["Current State"].Points.AddXY(0.45, 18);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 18);
                LogitechGSDK.LogiPlaySoftstopForce(0, 58);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 87.5 && wheelPosition < 124)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.3333 - 116.67);
                }
                else if (wheelPosition <= 87.5 && wheelPosition >= -87.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -87.5 && wheelPosition > -124)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.3333) - 116.67);
                }
                else if (wheelPosition >= 124 || wheelPosition <= -124)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.3;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 30000 && inversePedalInput < 40000)
            {
                chart2.Series["Current State"].Points.AddXY(0.575, 23);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 23);
                LogitechGSDK.LogiPlaySoftstopForce(0, 54);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 80.5 && wheelPosition < 114)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.4493 - 116.67);
                }
                else if (wheelPosition <= 80.5 && wheelPosition >= -80.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -80.5 && wheelPosition > -114)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.4493) - 116.67);
                }
                else if (wheelPosition >= 114 || wheelPosition <= -114)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.5;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 40000 && inversePedalInput < 50000)
            {
                chart2.Series["Current State"].Points.AddXY(0.675, 27);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 27);
                LogitechGSDK.LogiPlaySoftstopForce(0, 48);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 71.75 && wheelPosition < 101.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.626 - 116.67);
                }
                else if (wheelPosition <= 71.75 && wheelPosition >= -71.75)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -71.75 && wheelPosition > -101.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.626) - 116.67);
                }
                else if (wheelPosition >= 101.5 || wheelPosition <= -101.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.7;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 50000 && inversePedalInput < 60000)
            {
                chart2.Series["Current State"].Points.AddXY(0.75, 30);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 30);
                LogitechGSDK.LogiPlaySoftstopForce(0, 34);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 50.75 && wheelPosition < 71.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 2.2989 - 116.67);
                }
                else if (wheelPosition <= 50.75 && wheelPosition >= -50.75)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -50.75 && wheelPosition > -71.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-2.2989) - 116.67);
                }
                else if (wheelPosition >= 71.5 || wheelPosition <= -71.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.8;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 60000)
            {
                chart2.Series["Current State"].Points.AddXY(0.875, 35);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 35);
                LogitechGSDK.LogiPlaySoftstopForce(0, 30);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 45.5 && wheelPosition < 64)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 2.5641 - 116.67);
                }
                else if (wheelPosition <= 45.5 && wheelPosition >= -45.5)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -45.5 && wheelPosition > -64)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-2.5641) - 116.67);
                }
                else if (wheelPosition >= 64 || wheelPosition <= -64)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.9;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
        }
        void forceFeedback4()
        {
            chart1.Series["Current Position"].Points.Clear();

            double inversePedalInput = ConvertRange(0, 65535, 65535, 0, jst.AxisD);
            new_pedalInput = inversePedalInput;


            label_accelVal.Text = inversePedalInput.ToString();
            chart2.Series["Current State"].Points.Clear();

            double reversePedal = Math.Round(ConvertRange(65535, 0, 0, -0.5, jst.AxisA),2);
            label43.Text = reversePedal.ToString();

            if (inversePedalInput <= 200)
            {
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);

                chart2.Series["Current State"].Points.AddXY(0, 0);
                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);
                LogitechGSDK.LogiStopDamperForce(0);
                LogitechGSDK.LogiStopSoftstopForce(0);
                chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);

                label40.Text = send_msg.omega_des[2].ToString();
            if (new_pedalInput != prev_pedalInput)
            {
                send_msg.v_des[0] = 0.0;
            }
            }
            else if (inversePedalInput < 10000 && inversePedalInput > 200)
            {
                chart2.Series["Current State"].Points.AddXY(0.075, 3);
                logi_properties.wheelRange = 300; //210deg w/o force
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);
                LogitechGSDK.LogiPlayDamperForce(0, 3);
                LogitechGSDK.LogiPlaySoftstopForce(0, 70);

                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 100 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 2 - 200);
                }
                else if (wheelPosition <= 100 && wheelPosition >= -100)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -100 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-2) -200);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);
                }

                send_msg.v_des[0] = 0.1;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 10000 && inversePedalInput < 20000)
            {
                chart2.Series["Current State"].Points.AddXY(0.15, 6);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 6);
                LogitechGSDK.LogiPlaySoftstopForce(0, 64);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 96.25 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.851852 - 177.778);
                }
                else if (wheelPosition <= 96 && wheelPosition >= -96)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -96&& wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition *(- 1.851852) - 177.778);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);
                }
                send_msg.v_des[0] = 0.2;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();
            }

            else if (inversePedalInput >= 20000 && inversePedalInput < 30000)
            {
                chart2.Series["Current State"].Points.AddXY(0.45, 18);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 18);
                LogitechGSDK.LogiPlaySoftstopForce(0, 58);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 87 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.587302 - 138.095);
                }
                else if (wheelPosition <= 87 && wheelPosition >= -87)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -87 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (- 1.587302) - 138.095);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.3;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 30000 && inversePedalInput < 40000)
            {
                chart2.Series["Current State"].Points.AddXY(0.575, 23);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 23);
                LogitechGSDK.LogiPlaySoftstopForce(0, 54);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 81 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.449275 -117.391);
                }
                else if (wheelPosition <= 81 && wheelPosition >= -81)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -81 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.449275) -117.391);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.5;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 40000 && inversePedalInput < 50000)
            {
                chart2.Series["Current State"].Points.AddXY(0.675, 27);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 27);
                LogitechGSDK.LogiPlaySoftstopForce(0, 48);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 72 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * 1.282051 - 92.3077);
                }
                else if (wheelPosition <= 72 && wheelPosition >= -72)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -72 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (- 1.282051) - 92.3077);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.7;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 50000 && inversePedalInput < 60000)
            {
                chart2.Series["Current State"].Points.AddXY(0.75, 30);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 30);
                LogitechGSDK.LogiPlaySoftstopForce(0, 34);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 51 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition *1.010101 - 51.5152);
                }
                else if (wheelPosition <= 51 && wheelPosition >= -51)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -51 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-1.010101) - 51.5152);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.8;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();

            }
            else if (inversePedalInput >= 60000)
            {
                chart2.Series["Current State"].Points.AddXY(0.875, 35);

                logi_properties.wheelRange = 300;
                LogitechGSDK.LogiSetPreferredControllerProperties(logi_properties);

                LogitechGSDK.LogiPlayDamperForce(0, 35);
                LogitechGSDK.LogiPlaySoftstopForce(0, 30);
                double wheelPosition = ConvertRange(0, 65535, -150, 150, jst.AxisC);
                label27.Text = wheelPosition.ToString();
                if (wheelPosition > 45 && wheelPosition < 149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * .0952381- 42.8571);
                }
                else if (wheelPosition <= 45 && wheelPosition >= -45)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 0);
                }
                else if (wheelPosition < -45 && wheelPosition > -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, wheelPosition * (-.0952381)- 42.8571);
                }
                else if (wheelPosition >= 149 || wheelPosition <= -149)
                {
                    chart1.Series["Current Position"].Points.AddXY(wheelPosition, 100);

                }
                send_msg.v_des[0] = 0.9;
                double omega_value = ConvertRange(-150, 150, 45, -45, wheelPosition);
                send_msg.omega_des[2] = Math.Round(omega_value * System.Math.PI / 180, 2);
                label40.Text = send_msg.omega_des[2].ToString();
            
                prev_pedalInput = inversePedalInput;

            }
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }
        void Stop()
        {
            tmrUpdateStick.Enabled = false;
            LogitechGSDK.LogiSteeringShutdown();
            Application.Exit();
            this.serialPort1.Close();

        }


        void dataReceive(object sender, System.EventArgs args)
        {
            if (received[0] == 121 && received[9] == 120)
            {
                int revolutions1 = (received[1] * 256 + received[2]);
                int revolutions2 = (received[3] * 256 + received[4]);
                int encoderAngle1 = (received[5] * 256 + received[6]);
                int encoderAngle2 = (received[7] * 256 + received[8]);


                label11.Text = encoderAngle1.ToString();
                label10.Text = encoderAngle2.ToString();

                label24.Text = revolutions1.ToString();
                label23.Text = revolutions2.ToString();

                label26.Text = (revolutions1 * 2 * Math.PI * 38.1 + Math.PI * 38.1 / 180 * encoderAngle1).ToString("0.###");
                label25.Text = (revolutions2 * 2 * Math.PI * 38.1 + Math.PI * 38.1 / 180 * encoderAngle2).ToString("0.###");


            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(5);

            try
            {
                if (serialPort1.BytesToRead >= 0)
                {
                    serialPort1.Read(received, 0, serialPort1.BytesToRead);
                }
                Invoke(new EventHandler(dataReceive));
            }
            catch (System.Exception ex)
            { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            time_millisec.Text = stopwatch.Elapsed.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer_stopwatch.Enabled = true;
            timer_stopwatch.Start();
            stopwatch.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stopwatch.Stop();
            stopwatch.Reset();
            time_millisec.Text = "00";
            encoderInit = 1;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer_stopwatch.Enabled = false;
            timer_stopwatch.Stop();
            stopwatch.Stop();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            sendLCM_enable = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            send_msg.v_des[0] = 0;
        }



        private void button4_Click(object sender, EventArgs e)
        {
            send_msg.emergency_damp = 1;
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            send_msg.impedance_scale = Convert.ToDouble(hScrollBar1.Value) * 0.01;
            label37.Text = send_msg.impedance_scale.ToString();
        }

        private void hScrollBar2_Scroll(object sender, ScrollEventArgs e)
        {
            send_msg.balance_scale = Convert.ToDouble(hScrollBar2.Value) * 0.01;
            label38.Text = send_msg.balance_scale.ToString();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    send_msg.mode = 0;
                    label45.Text = "Mode 0";
                    break;
                case 1:
                    send_msg.mode = 1;
                    label45.Text = "Mode 1";
                    break;
                case 2:
                    send_msg.mode = 2;
                    label45.Text = "Mode 2";
                    break;
                case 3:
                    send_msg.mode = 3;
                    label45.Text = "Mode 3";
                    break;
                case 4:
                    send_msg.mode = 6;
                    label45.Text = "Mode 6";
                    break;
                default:
                    break;
            }


        }


        public partial class Axis
        {
            public Axis()
            {

            }

            private int axisPos = 32767;
            public int AxisPos
            {
                set
                {
                    //lblAxisName.Text = "Axis: " + axisId + "  Value: " + value;
                    //tbAxisPos.Value = value;
                    axisPos = value;
                }
            }

            private int axisId = 0;
            public int AxisId
            {
                set
                {
                    //lblAxisName.Text = "Axis: " + value + "  Value: " + axisPos;
                    axisId = value;
                }
                get
                {
                    return axisId;
                }
            }
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (hScrollBar1.Value == 100 || hScrollBar1.Value == 100)
            {
                hScrollBar1.Value = 100;
                hScrollBar2.Value = 100;
            }
            else
            {
                hScrollBar1.Value++;
                hScrollBar2.Value++;
            }

            send_msg.impedance_scale = Convert.ToDouble(hScrollBar1.Value) * 0.01;
            label37.Text = send_msg.impedance_scale.ToString();
            send_msg.balance_scale = Convert.ToDouble(hScrollBar2.Value) * 0.01;
            label38.Text = send_msg.balance_scale.ToString();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (hScrollBar1.Value == 0 || hScrollBar1.Value == 0)
            {
                hScrollBar1.Value = 0;
                hScrollBar2.Value = 0;
            }
            else
            {
                hScrollBar1.Value--;
                hScrollBar2.Value--;
            }

            send_msg.impedance_scale = Convert.ToDouble(hScrollBar1.Value) * 0.01;
            label37.Text = send_msg.impedance_scale.ToString();
            send_msg.balance_scale = Convert.ToDouble(hScrollBar2.Value) * 0.01;
            label38.Text = send_msg.balance_scale.ToString();
        }

        private void button8_MouseDown(object sender, MouseEventArgs e)
        {
            timer1.Enabled = true;
        }

        private void button8_MouseUp(object sender, MouseEventArgs e)
        {
            timer1.Enabled = false;
        }

        private void button7_MouseDown(object sender, MouseEventArgs e)
        {
            timer2.Enabled = true;
        }

        private void button7_MouseUp(object sender, MouseEventArgs e)
        {
            timer2.Enabled = false;
        }

        private void label_wheelVal_Click(object sender, EventArgs e)
        {

        }

        private void label27_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            send_msg.p_des[2] = System.Convert.ToDouble(numericUpDown6.Value);

        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            send_msg.p_des[0] = System.Convert.ToDouble(numericUpDown7.Value);

        }

        private void button9_Click(object sender, EventArgs e)
        {
            send_msg.mode = 3;
            label45.Text = "Mode 3";
        }

        private void button10_Click(object sender, EventArgs e)
        {
            send_msg.mode = 6;
            label45.Text = "Mode 6";
        }
    }
}
