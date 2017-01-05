using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Windows;
using OPCAutomation;
using System.Resources;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;

namespace UItest
{
    public partial class Form1 : Form
    {
        Image[] pics = new Image[] { };//背景图片
        int zhenshu = 0;//背景帧数
        int tempzhen = 0;//目前帧
        Form3 RawInfo;//材料设置页面
        Bitmap led_green;//运行状态图片
        Bitmap led_gray;//停止状态图片
        Label[] leds;//通断显示
        MyClient myclient;//我的OPC客户端
        MySaveData mysavedata;//保存的数据
        bool isBackruning;//是否有后台收集线程(计时器)运行中
        Thread thread_Query;
        long runtime;//运行时间
        int boshu;//显示的波形数目
        public Form1()
        {
            
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            
            InitializeComponent();
            RawInfo = new Form3();
            myclient = new MyClient();
            mysavedata = new MySaveData();
            isBackruning = false;
            runtime = 0;
            //label10.Visible = false;
            //pics = getFrames(label10.Image);
            thread_Query = new Thread(new ParameterizedThreadStart(thread_backrun));
            //thread_Query.Start(this);//新的后台查询方法
           // timer2.Enabled = true;
            myclient.boxing_event += boxing_refresh;
            myclient.pulse_event += pulse_refresh;
            myclient.frequency_event += frequence_refresh;
            
        }
        /// <summary>
        /// 界面优化之背景动图
        /// </summary>
        /// <param name="originalImg"></param>
        /// <returns></returns>
        Image[] getFrames(Image originalImg)
        {

            int numberOfFrames = originalImg.GetFrameCount(FrameDimension.Time);
            Image[] frames = new Image[numberOfFrames];
            zhenshu = numberOfFrames;
            for (int i = 0; i < numberOfFrames; i++)
            {
                originalImg.SelectActiveFrame(FrameDimension.Time, i);
                frames[i] = ((Image)originalImg.Clone());
            }

            return frames;
        }
        /// <summary>
        /// 收集运行时的数据，比如脉冲，频率，电机状态，弹片通断,并记录数据以便导出,延时是因为有刷新频率
        /// </summary>
        public void backrun()
        {
            myclient.Query_Pulse();
            Thread.Sleep(15);
            myclient.Query_EngineStatue();
            Thread.Sleep(15);
            myclient.Query_Frequncy();
            Thread.Sleep(15);
            myclient.Query_Leds();
            
        }
        /// <summary>
        /// 分线程后台查询，为计时器后台查询的优化，因为计时器中延时主线程还是会卡住
        /// </summary>
        /// <param name="data"></param>
        static void thread_backrun(object data){
            Form1 form = (Form1)data;
                form.backrun();
        }

        /// <summary>
        /// 定时刷新界面显示的数据时调用的,仅当OPC服务器连接上PLC时调用，否则全部初始值
        /// </summary>
        void backshow() {
            if (myclient.connecteds) {
                textBox5.Text = byteToHexStr(myclient.outdata);
                textBox6.Text = byteToHexStr(myclient.configdata);
                textBox7.Text = byteToHexStr(myclient.indata);
                textBox8.Text = byteToHexStr(myclient.chuandata);
                for (int i = 0; i < 14; i++)
                {
                    leds[i].Image = myclient.leds[i] ? led_green : led_gray;
                }
                if (myclient.running) runtime++;
                
                textBox2.Text = runtime.ToString();
                //textBox3.Text = myclient.pulse.ToString();
                //textBox4.Text = myclient.frequency.ToString();
                
                ledrunning.Image = myclient.running ? led_green : led_gray;
                ledcomplete.Image = !myclient.running ? led_green : led_gray;
            }
            else initValue();
        }
        /// <summary>
        /// 初始值函数，当OPC未连接上PLC时调用界面的显示
        /// </summary>
        void initValue() {
            textBox5.Text = "NULL";
            textBox6.Text = "NULL";
            textBox7.Text = "NULL";
            textBox8.Text = "NULL";
            for (int i = 0; i < 14; i++)
            {
                leds[i].Image = led_gray;
            }
            runtime++;
            textBox2.Text = "0";
            textBox3.Text = "0";
            textBox4.Text = "0";
            ledrunning.Image = led_gray;
            ledcomplete.Image = led_gray;
        }

        //乱七八糟的界面初始化函数，不要问为什么，待优化...
        private void Form1_Load(object sender, EventArgs e)
        {
            if (button1.Image != null)
            {
                button1.Image = new Bitmap(button1.Image, button1.Height - 10, button1.Height - 10);
            }
            if (button2.Image != null)
            {
                button2.Image = new Bitmap(button2.Image, button2.Height - 10, button2.Height - 10);
            }
            if (button3.Image != null) {
                button3.Image = new Bitmap(button3.Image, button3.Height - 10, button3.Height - 10);
            }
            if (button4.Image != null)
            {
                button4.Image = new Bitmap(button4.Image, button4.Height - 10, button4.Height - 10);
            }
            if (button5.Image != null)
            {
                button5.Image = new Bitmap(button5.Image, button5.Height - 10, button5.Height - 10);
            }
            if (button6.Image != null)
            {
                button6.Image = new Bitmap(button6.Image, button6.Height - 10, button6.Height - 10);
            }
            led_green = new Bitmap(ledrunning.Image, ledrunning.Height, ledrunning.Height);
            led_gray = new Bitmap(ledcomplete.Image, ledcomplete.Height, ledcomplete.Height);
            ledcomplete.Image = led_gray;
            ledrunning.Image = led_green;
            led1.Image = led_green;
            led2.Image = led_green;
            led3.Image = led_green;
            led4.Image = led_green;
            led5.Image = led_green;
            led6.Image = led_green;
    
            led7.Image = led_green;
            led8.Image = led_green;
            led10.Image = led_green;
            led9.Image = led_green;
            led11.Image = led_green;
            led12.Image = led_green;
            led13.Image = led_green;
            led14.Image = led_green;
            leds = new Label[14];
            leds[0] = led1;
            leds[1] = led2;
            leds[2] = led3;
            leds[3] = led4;
            leds[4] = led5;
            leds[5] = led6;
            leds[6] = led7;
            leds[7] = led8;
            leds[8] = led9;
            leds[9] = led10;
            leds[10] = led11;
            leds[11] = led12;
            leds[12] = led13;
            leds[13] = led14;
            Bitmap tempm = new Bitmap(this.BackgroundImage, this.Size);
            tempm = AdjustTobBlur(tempm, groupBox1.Location, groupBox1.Size);
            tempm = AdjustTobBlur(tempm, groupBox2.Location, groupBox2.Size);
            tempm = AdjustTobBlur(tempm, groupBox3.Location, groupBox3.Size);
            BackgroundImage = tempm;
            
            //Color halftr = Color.FromArgb(20, 255, 255, 255);
            //groupBox1.BackColor = halftr;
            //pictureBox1.Image = new Bitmap(pictureBox1.Image, pictureBox1.Width, pictureBox1.Height);
            //Bitmap tempm = new Bitmap(this.BackgroundImage,this.Size);
            //tempm = AdjustTobBlur(tempm, groupBox2.Location, groupBox2.Size);
            //BackgroundImage = AdjustTobBlur(tempm, groupBox1.Location, groupBox1.Size);
            //this.BackgroundImage = tempm;

            
        }
        
        /// <summary>
        /// 启动电机,同时将速率值传入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            StartEginAysnc();
        }
        /// <summary>
        /// 异步实现启动方式，不造成界面延时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void StartEginAysnc() {
            comboBox1_SelectedIndexChanged(null, null);
            Thread.Sleep(15);
            myclient.Set_Start();
            //if (myclient.connecteds) timer1.Enabled = true;
            runtime = 0;
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {
            
        }
        /// <summary>
        /// 将指定区域模糊化的图片处理函数
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="location"></param>
        /// <param name="sizet"></param>
        /// <returns></returns>
        public Bitmap AdjustTobBlur(Bitmap bitmap,Point location,Size sizet)
        {
            int lasty = location.Y + sizet.Height+25;
            int lastx = location.X + sizet.Width+15;
            int effectRradius = 5;
            // 整体图片跑 Pixel 圈
            for (int heightOffset = location.Y; heightOffset < lasty; heightOffset++)
            {
                for (int widthOffset = location.X; widthOffset < lastx; widthOffset++)
                {
                    // 负责计算平均值
                    int avgR = 0, avgG = 0, avgB = 0;
                    int blurPixelCount = 0;

                    // 计算传入影响范围内 的 RGB 平均
                    for (int x = widthOffset; (x < widthOffset + effectRradius && x < lastx); x++)
                    {
                        for (int y = heightOffset; (y < heightOffset + effectRradius && y < lasty); y++)
                        {
                            System.Drawing.Color pixel = bitmap.GetPixel(x, y);

                            avgR += pixel.R;
                            avgG += pixel.G;
                            avgB += pixel.B;

                            blurPixelCount++;
                        }
                    }

                    // 计算个别平均
                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;


                    // 写回入新图片 
                    for (int x = widthOffset; (x < widthOffset + effectRradius && x < lastx ); x++)
                    {
                        for (int y = heightOffset; (y < heightOffset + effectRradius && y < lasty ); y++)
                        {
                            System.Drawing.Color newColor = System.Drawing.Color.FromArgb(avgR, avgG, avgB);
                            bitmap.SetPixel(x, y, newColor);
                        }
                    }

                }
            }

            return bitmap;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            groupBox4.Visible = !groupBox4.Visible;
            if (!groupBox4.Visible)
            {
                this.Width -= groupBox4.Size.Width;
            }
            else
            {
                // this.Width += groupBox4.Size.Width;
            }
            this.AutoSize = true;
        }
     
       /// <summary>
       /// 字符串转换为16进制数组
       /// </summary>
       /// <param name="hexString"></param>
       /// <returns></returns>
        private byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
        /// <summary>
        /// 16进制数组转换为字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }
       /// <summary>
       /// 停止电机
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            myclient.Set_Stop();
            //timer1.Enabled = false;
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int speed = Convert.ToInt32(comboBox1.Text);
            switch (speed)
            {
                case 150:
                    myclient.Set_Frequency(1);
                    break;
                case 200:
                    myclient.Set_Frequency(2);
                    break;
                case 400:
                    myclient.Set_Frequency(3);
                    break;
                case 450:
                    myclient.Set_Frequency(4);
                    break;
                default:
                    MessageBox.Show("index错误");
                    break;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            myclient.Set_InitEngine();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            

        }

        ///
        private void button4_Click(object sender, EventArgs e)
        {
            RawInfo.Show();
        }
        /// <summary>
        /// 收集数据和刷新显示的定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <updata>
        /// 更新为新的后台查询方法
        /// </updata>
        private void timer1_Tick(object sender, EventArgs e)
        {
            thread_Query = new Thread(new ParameterizedThreadStart(thread_backrun));
            thread_Query.Start(this);
            backshow();
            Console.WriteLine(DateTime.Now.ToString());
        }
        
        private void timer2_Tick(object sender, EventArgs e)
        {
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mysavedata.Myhead1(RawInfo.textBox1.Text.ToString(), RawInfo.textBox2.Text.ToString(), RawInfo.textBox3.Text.ToString());
            mysavedata.excelport();
        }
        /// <summary>
        /// 设置存档数据的保存间隔时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
            {
                timer3.Interval = 1000*5;
            }
            else if (comboBox2.SelectedIndex == 1)
            {
                timer3.Interval = 1000 * 60 * 30;
            }
            else if (comboBox2.SelectedIndex == 2)
            {
                timer3.Interval = 1000 * 60 * 60;
            }
        }
        /// <summary>
        /// 定时存储数据，仅当电机启动并且OPC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer3_Tick(object sender, EventArgs e)
        {
            mysavedata.adddate(textBox2.Text.ToString(), textBox3.Text.ToString(), "1", textBox4.Text.ToString(), "1.433", comboBox1.Text.ToString(), "1.433", DateTime.Now.ToString(), myclient.leds);
        }
        /// <summary>
        /// 背景动图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Tick_1(object sender, EventArgs e)
        {
            tempzhen++;
            this.BackgroundImage = pics[tempzhen];
            if (tempzhen == zhenshu - 2) tempzhen = 0;
        }
        /// <summary>
        /// 5ms刷新一次波形的函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer4_Tick(object sender, EventArgs e)
        {
            //int boshu = myclient.boxing.Count;
            //if (boshu < 20)
            //{
            //    for (int i = boshu; i < 20; i++)
            //    {
            //        myclient.boxing.Add(0);
            //    }
            //}
            //Series temps = new Series();
            //temps.ChartType = SeriesChartType.Spline;
            //temps.Color = Color.Red;
            //temps.BorderWidth = 2;
            //temps.Name = "数据1";
            //double x=0;
            //for (int i = 0; i < 20; i++) {
            //    temps.Points.AddXY(x, myclient.boxing[i]);
            //    x+= 0.5;
            //}
            //chart1.Series[0] = temps;
            //myclient.boxing.RemoveAt(0);
        }
        /// <summary>
        /// 波形刷新事件的订阅处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void boxing_refresh(object sender, EventArgs e)
        {
            //int boshu = myclient.boxing.Count;

            Amplitude.Text = myclient.amplitude.ToString();
            Series temps = new Series();
            temps.ChartType = SeriesChartType.Spline;
            temps.Color = Color.Red;
            temps.BorderWidth = 2;
            temps.Name = "实时波形1";
            
            double x = 0;
            for (int i = 0; i < 10; i++)
            {
                temps.Points.AddXY(x, myclient.boxing[i]);
                x += 0.5;
            }
            Boxing.Text = myclient.boxing[0].ToString();
            chart1.Series[0] = temps;
            myclient.boxing.RemoveAt(0);
        }
        /// <summary>
        /// 刷新脉冲函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pulse_refresh(object sender, EventArgs e)
        {
            textBox3.Text = myclient.pulse.ToString();
            //textBox4.Text = myclient.frequency.ToString();
        }
        void frequence_refresh(object sender, EventArgs e)
        {
            textBox4.Text = myclient.frequency.ToString();
        }
    }
}
