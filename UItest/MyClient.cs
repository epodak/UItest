using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OPCAutomation;
using System.Windows.Forms;
using System.Threading;
namespace UItest
{
    class MyClient
    {
        //如果只是采用订阅的方式，可能采集不到实时的频率和波形，因为它只在其变化时产生，所以可以通过定时读的方式
        //System.Windows.Forms.Timer pulse_timer;//实时每秒的调用计算频率和脉冲的函数
        public event EventHandler boxing_event;//波形刷新事件
        public event EventHandler pulse_event;//脉冲刷新事件
        public event EventHandler frequency_event;//频率刷新事件
        System.Windows.Forms.Timer boxing_timer;//实时记录波形
        //byte[] pulsedata;//显示脉冲的信息
        byte[] boxingdata;//波形的数据
        public bool[] leds;//弹片通断指示
        public bool running;//电机是否运行
        public long pulse;//电机脉冲数
        public long frequency;//真实频率
        public double amplitude;//振幅
        public List<int> boxing;//波形数据，20个起
        public byte[] indata;//各个item的数据
        public byte[] outdata;
        public byte[] configdata;
        public byte[] chuandata;
        long lastpulse;//上次脉冲数，用于辅助计算真实频率
        long lastpulse_5;//上5次脉冲数，用于辅助计算真实频率
        int countt;//让5次脉冲，计算一次频率,这个是获取脉冲的次数
        public bool connected;//是否连接上了OPC服务器
        public bool connecteds;//OPC服务器是否连接上PLC
        OPCServer myserver;//OPC服务器
        OPCGroups mygroups;//OPC组
        OPCGroup mygroup;//OPC单组
        OPCItems myitems;//OPCitems
        OPCItem item_in;//输入OPCitem
        OPCItem item_conf;//校验OPCitem
        OPCItem item_out;//输出OPCitem
        OPCItem item_chuan;//传感器，传输振幅
        OPCDataSource source;//OPC数据类型(对，暂时没用)
        byte[] lastorder;//order和ordertype任意用一个校验，先用order试下
        byte lastsetordertype;//需要判断是否下发完成的最后一个指令，目前先通过out是否改变判断
        
        bool isfirstuse;//是否是才连接，这时显示的数据会有PLC自带的数据
        ///   <summary>   
        ///   以下为指令集
        ///   </summary> 
        byte[] Engine1_start =      { 0x06, 0x67, 0x01, 0x01, 0x01, 0x47 };//电机1启动命令
        byte[] Engine1_stop =       { 0x06, 0x67, 0x01, 0x01, 0x00, 0x47 };//电机1停止命令
        byte[] Speed11 =            { 0x06, 0x67, 0x02, 0x01, 0x00, 0x47 };//电机1档位1
        byte[] Speed12 =            { 0x06, 0x67, 0x02, 0x01, 0x01, 0x47 };
        byte[] Speed13 =            { 0x06, 0x67, 0x02, 0x01, 0x02, 0x47 };
        byte[] Speed14 =            { 0x06, 0x67, 0x02, 0x01, 0x03, 0x47 };
        byte[] Pulseall =           { 0x06, 0x67, 0x03, 0x00, 0x03, 0x47 };//获取所有脉冲
        byte[] Breakall =           { 0x06, 0x67, 0x04, 0x00, 0x03, 0x47 };//获取所有通断
        byte[] Engine1_statue =     { 0x06, 0x67, 0x05, 0x01, 0x00, 0x47 };//电机是否运行
        byte[] Initall =            { 0x06, 0x67, 0x06, 0x04, 0x00, 0x47 };//电机1清零
        byte[] Frequency1 =         { 0x06, 0x67, 0x07, 0x00, 0x00, 0x47 };//获取电机实时频率
        byte[] Config =             { 0x67, 0xAA, 0x47, 0x00, 0x00, 0x00 };//校验响应清零
        byte[] wuxiao =             { 0x67, 0x00, 0x00, 0x00, 0x00, 0x00 };//一个无效指令
        ///   <summary>   
        ///   默认构造函数，当后续有多个电机和多个设备时可重构
        ///   </summary>  
        public MyClient()
        {
            InitOpc();
            lastorder = new byte[6];
            lastsetordertype = 0x00;
            leds = new bool[16];
            indata = new byte[]{};
            outdata = new byte[]{};
            configdata = new byte[]{ };
            boxing = new List<int> { };
            for (int i = 0; i < 200; i++) {
                boxing.Add(0);
            }
            //pulse_timer = new System.Windows.Forms.Timer();
            //pulse_timer.Interval = 1000;
            //pulse_timer.Enabled = true;
            //pulsedata = new byte[4];
            //pulse_timer.Tick += new System.EventHandler(this.intime_Tick);

            boxing_timer = new System.Windows.Forms.Timer();
            boxing_timer.Interval = 21;
            boxing_timer.Enabled = true;
            boxingdata = new byte[4];
            boxing_timer.Tick += new System.EventHandler(this.boxing_Tick);
            //可以让那边通过订阅的方式读数据

            countt = 0;
        }
        /// <summary>
        /// 延迟10ms写，以免冲突，暂时不用
        /// </summary>
        /// <param name="?"></param>
        void WriteLater(byte[] data) { 
            System.Threading.Thread.Sleep(10);
            item_out.Write(data);
        }
        /// <summary>
        /// 实时计时器的响应函数，会调用脉冲频率
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //void intime_Tick(object sender, EventArgs e)
        //{
            
        //    Todo_in(pulsedata);
        //}
        /// <summary>
        /// 实时读取波形数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void boxing_Tick(object sender, EventArgs e)
        {
            object value;
            object quality;
            object timeStamp;
            if (connecteds)
            {
                item_chuan.Read((short)OPCDataSource.OPCCache, out value, out quality, out timeStamp);
                chuandata = (byte[])value;
            }
            else {
                chuandata = new byte[]{0x00,0x00,0x00,0x00};
            }
            Todo_chuan(chuandata);
        }
        
        ///   <summary>   
        ///   初始化OPC服务器
        ///   </summary>     
        void InitOpc() {
            connected = false;
            connecteds = false;
            isfirstuse = true;
            try
            {
                string severname = "KEPware.KEPServerEx.V4";
                string groupname = "S7-200.S7-200-1";
                string outname = "S7-200.S7-200-1.OUT";
                string inname = "S7-200.S7-200-1.IN";
                string chuanname = "S7-200.S7-200-1.CHUAN";
                string confname = "S7-200.S7-200-1.CONF";
                
                myserver = new OPCServer();
                myserver.Connect(severname);
                mygroups = myserver.OPCGroups;
                mygroup = mygroups.Add(groupname);
                mygroup.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(ObjOPCGroup_DataChange);
                myitems = mygroup.OPCItems;
                item_out = myitems.AddItem(outname, 1);//opcitem标识，1out,2config,3in,4chuan
                item_conf = myitems.AddItem(confname, 2);
                item_in = myitems.AddItem(inname.ToString(), 3);
                item_chuan = myitems.AddItem(chuanname.ToString(), 4);
                mygroup.UpdateRate = 10;
                mygroup.IsActive = true;
                mygroup.IsSubscribed = true;
                connected = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());//如果连接不上，说明运行库有问题，关了软件重新安装必要组件再运行
            }
        }
        ///   <summary>   
        ///   当有数据发生改变时，订阅的处理函数,不能有延时函数或者耗时过久的函数
        ///   </summary>
        private void ObjOPCGroup_DataChange(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        {
            Byte[] temp;
            for (int i = 1; i <= NumItems; i++) {//没错，这玩意是从1开始的 
                temp = (Byte[])ItemValues.GetValue(i);
                if (Convert.ToInt32(Qualities.GetValue(i)) != 192) {
                    connecteds = false;
                    isfirstuse = true;
                    indata = configdata = outdata = new byte[]{};//通过Length是否为0也能判断是否连接上PLC
                    return;//连接上了服务器，但是服务器没有连接上PLC
                }
                connecteds = true;
                if (isfirstuse)
                {
                    if (Convert.ToInt32(ClientHandles.GetValue(i)) == 1) lastorder = temp;
                    if (Convert.ToInt32(ClientHandles.GetValue(i)) == 2) lastsetordertype = temp[1];
                }
                    switch (Convert.ToInt32(ClientHandles.GetValue(i)))
                    {
                        //1out,2config,3in
                        case 1:
                            outdata = temp;
                            Todo_out(temp);
                            break;
                        case 2:
                            configdata = temp;
                            Todo_config(temp);
                            break;
                        case 3:
                            indata = temp;
                         //   Console.WriteLine("{0}", indata);
                            Todo_in(temp);
                            break;
                        //case 4:
                            //chuandata = temp;
                            //Todo_chuan(temp);
                            //break;
                            //采用了实时阅读的方式，不需要

                    }
            }
            isfirstuse = false;
        }

        ///   <summary>   
        ///   通断处理
        ///   </summary>     
        void Todo_Break(byte[] data) {
            leds = new bool[14];
            for (int i = 0; i < 14; i++)
            {
                leds[i] = data[i] == 0x01 ? true : false;
            }
            Console.WriteLine("通断");
        }
        ///   <summary>   
        ///   脉冲处理
        ///   </summary>  
        void Todo_Pulse(byte[] data) {
            //1S一次
            long temp = 0;
            for (int i = 0; i < data.Length; i++) {
                temp *= 256;
                temp += data[i];
            }
            lastpulse = pulse;
            pulse = temp;
            //frequency = (pulse - lastpulse);
            //string temps = frequency.ToString();
            pulse_event(this, null);
            //Console.WriteLine(pulse);
            //countt = countt < 4 ? countt + 1 : 0;
            //if (countt == 0)
            //{
            //    使用脉冲计算的频率
            //    Todo_Frequency();
            //    lastpulse_5 = pulse;
            //}
        }
        /// <summary>
        /// 使用命令返回的频率
        /// </summary>
        /// <param name="data"></param>
        void Todo_Frequency(byte[] data)
        {
            frequency = data[0] * 256 +data[1];
            frequency_event(this, null);
            Console.WriteLine("{0} {1}", data[0], data[1]);
           
        }
        /// <summary>
        /// 计算振幅的处理函数
        /// </summary>
        void Todo_Ampnitude()
        {
            amplitude = (boxing.Max() - boxing.Min()) / 2.0;

        }
        /// <summary>
        /// 使用脉冲计算的频率
        /// </summary>
        void Todo_Frequency()
        {
            long temp = pulse - lastpulse_5;
            frequency = temp > 0 ? temp/5 : 0;
            frequency_event(this, null);
        }
        ///   <summary>   
        ///   电机状态处理
        ///   </summary>  
        void Todo_EngineStatue(byte[] data) {
            running = data[0] == 0x01 ? true : false;
            Console.WriteLine("电机状态");
        }
        ///   <summary>   
        ///   设置命令响应校验处理
        ///   </summary>  
        void Todo_config(byte[] data) {
            if (data[1] == 0x00) return;
            if (data[1] != lastsetordertype) MessageBox.Show("设置指令未成功响应，请重试");
            else {
                item_out.Write(Config);
                lastorder = Config;
                
            }  
        }
        ///   <summary>   
        ///   请求命令响应处理,暂时不用
        ///   </summary>  
        void Todo_in(byte[] data) {

            ///<tips>
            /// 一定是请求类指令传回的数据，所以直接调用请求类处理函数，目前先根据返回的长度判断
            ///data:67,nx,47,0y n-任意16进制数,x,y表位数 且 x+y = 16-2=14位,通过n判断传回的数据
            ///</tips>
            int n = data.Length - 1;
            for (; n > 1; n--)
            {
                if (data[n] == 0x47) break;
            }
            if (n <= 1) return;//说明数据错误
            n--;
            byte[] data_rel = new byte[n];
            for (int i = 0; i < n; i++)
            {
                data_rel[i] = data[i + 1];
            }

            //n:2-6频率,1-3马达启停状态,4-脉冲，14-通断//暂时马达只有台，所以是1个字节
            switch (n)
            {
                case 2:
                    Todo_Frequency(data_rel);
                    break;
                case 4:
                    Todo_Pulse(data_rel);
                    //pulsedata = data_rel;
                    break;
                case 1:
                    Todo_EngineStatue(data_rel);
                    break;
                case 14:
                    Todo_Break(data_rel);
                    break;
            }
        }
        ///   <summary>   
        ///   确认是否发送成功的处理，暂时不用
        ///   </summary>  
        void Todo_out(byte[] data)
        {
            //if (lastorder != null && data!=null && !ArrayEqual(data, lastorder)) {
            //    MessageBox.Show("发送消息失败！请重新发送");
            //}
        }
        /// <summary>
        /// 传感器的处理函数，传入的是振幅
        /// </summary>
        /// <param name="data"></param>
        void Todo_chuan(byte[] data) {
            //foreach (byte temp in data) {
            //    boxing.Add(temp);
            //}
            //认为它是一个脉冲先
            int temp = 0;
            foreach (byte tempa in data)
            {
                temp *= 256;
                temp += tempa;
            }
            boxing.Add(temp);
            Todo_Ampnitude();
            boxing_event(this,null);
            
        }
        ///   <summary>   
        ///   判断byte数组是否相等
        ///   </summary>  
        bool ArrayEqual(byte[] data1, byte[] data2) {//默认传入非null值
           
            if (data1.Length != data2.Length) return false;
            for (int i = 0; i < data1.Length; i++)
            {
                if (data1[i] != data2[i]) return false;
            }
            return true;
        }
        ///   <summary>   
        ///   设置类命令，启动
        ///   </summary>  
        public void Set_Start() {
            if (connecteds)
            {
                item_out.Write(Engine1_start);
                lastorder = Engine1_start;
                lastsetordertype = 0x01;
            }
        }
        ///   <summary>   
        ///   设置类命令，停止
        ///   </summary>  
        public void Set_Stop() {
            if (connecteds)
            {
                item_out.Write(Engine1_stop);
                lastorder = Engine1_start;
                lastsetordertype = 0x01;
            }
        }
        ///   <summary>   
        ///   设置类命令，设置频率（并非直接设置显示频率，是发送一个指令）
        ///   </summary>  
        public void Set_Frequency(int i) {
            //赋值只能是1-4
            if (connecteds)
            {
                byte[] mes = new byte[]{};
                switch (i)
                {
                    case 1:
                        mes = Speed11;
                        break;
                    case 2:
                        mes = Speed12;
                        break;
                    case 3:
                        mes = Speed13;
                        break;
                    case 4:
                        mes = Speed14;
                        break;
                    default:
                        MessageBox.Show("频率参数错误！");
                        return;
                }
                item_out.Write(mes);
                lastorder = mes;
                lastsetordertype = 0x02;
            }
        }
        ///   <summary>   
        ///   查询类命令，查询当前脉冲
        ///   </summary> 
        public void Query_Pulse() {
            if (connecteds)
            {
                item_out.Write(Pulseall);
                lastorder = Pulseall;
                //lastsetordertype = 0x03;//不属于设置类命令
                Console.Write("脉冲指令发送");
                //item_out.Write(wuxiao);
            }
        }
        ///   <summary>   
        ///   查询类命令，查询当前弹片通断
        ///   </summary> 
        public void Query_Leds() {
            if (connecteds)
            {
                item_out.Write(Breakall);
                lastorder = Breakall;
                //item_out.Write(wuxiao);
                //lastsetordertype = 0x04;//不属于设置类命令
            }
        }
        ///   <summary>   
        ///   查询类命令，查询当前频率,因为考虑到线程布线长度问题，可能导致频率显示不正确，现增添一条查询频率指令
        ///   </summary> 
        public void Query_Frequncy() {
            if (connecteds)
            {
                item_out.Write(Frequency1);
                lastorder = Frequency1;
                //item_out.Write(wuxiao);
                //lastsetordertype = 0x07;//不属于设置类命令
            }

        }
        ///   <summary>   
        ///   查询类命令，查询电机状态(在运行还是没有)
        ///   </summary> 
        public void Query_EngineStatue() {
            if (connecteds)
            {
                item_out.Write(Engine1_statue);
                lastorder = Engine1_statue;
                //item_out.Write(wuxiao);
                //lastsetordertype = 0x05;//不属于设置类命令
            }
        }
        ///   <summary>   
        ///   特殊的设置类命令，它没有确认校验(不知道为啥)，理论上设置完需要判断电机状态是否真的停止，脉冲是否清零
        ///   </summary> 
        public void Set_InitEngine() {
            if (connecteds)
            {
                item_out.Write(Initall);
                lastorder = Initall;
                //lastsetordertype = 0x06;//不属于设置类命令
            }
        }

        
    }
}
