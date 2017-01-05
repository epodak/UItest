using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace UItest
{
    //这些枚举类型，暂时没用，之后如果设备多了，下指令麻烦可以考虑使用
    enum OrederType:byte { 
        SetStopbegin = 01,
        SetSpeed,
        GetPulse,
        GetBreak,
        GetStopbegin,
        SetInit = 06,
    };
    enum Slots : byte {
        Slotall = 0,
        Slot1 = 1,
        Slot2,
        Slot3,
        Initall
    };
    enum Orders : byte {
        Start = 1,
        Stop = 0,
        Speed1=0,
        Speed2 = 1,
        Speed3,
        Speed4,
        Default = 0
    };
    class MySaveData
    {

        string[] myhead1;
        string[] myhead2;
        List<string[]> mydates;
        public void Myhead1(string cailiao, string riqi, string pihao)
        {
            myhead1 = new string[6];
            myhead1[0] = "原材料信息：";
            myhead1[2] = "生产日期：";
            myhead1[4] = "批号：";
            myhead1[1] = cailiao;
            myhead1[3] = riqi;
            myhead1[5] = pihao;
        }
        public MySaveData()
        {
            char[] mysplit = { '\t' };
            string myheads = "电机1运行时间（秒）	循环次数1	测试穴位	实时频率1(Hz)	实时振幅(mm)	设定频率(Hz)	设定振幅(mm)	时间点	弹片1	弹片2	弹片3	弹片4	弹片5	弹片6	弹片7	弹片8	弹片9	弹片10	弹片11	弹片12	弹片13	弹片14";
            myhead2 = myheads.Split(mysplit);
            mydates = new List<string[]>();
        }
        public void adddate(string runtime, string cishu, string xuewei, string pinglv_now, string zhenfu_now, string pinglv_set, string zhenfu_set, string time_now, bool[] tanpian)
        {
            string[] mydate_temp = new string[22];
            mydate_temp[0] = runtime.ToString();
            mydate_temp[1] = cishu.ToString();
            mydate_temp[2] = xuewei.ToString();
            mydate_temp[3] = pinglv_now.ToString();
            mydate_temp[4] = zhenfu_now.ToString();
            mydate_temp[5] = pinglv_set.ToString();
            mydate_temp[6] = zhenfu_set.ToString();
            mydate_temp[7] = time_now.ToString();
            for (int i = 8; i < 22; i++)
            {
                mydate_temp[i] = tanpian[i - 8] ? "1" : "0";
            }
            mydates.Add(mydate_temp);
        }
        public void excelport()
        {
            FileStream f = new FileStream(@"C:\OPC\1.csv", FileMode.Create);
            StreamWriter n = new StreamWriter(f, Encoding.UTF8);
            n.WriteLine("");
            n.WriteLine(lineinfo(myhead1));
            n.WriteLine(lineinfo(myhead2));
            if (mydates != null)
            {
                foreach (var linedate in mydates)
                {
                    n.WriteLine(lineinfo(linedate));
                }
            }
            n.Close();
            f.Close();
        }
        string lineinfo(string[] infos)
        {
            string temp = null;
            int i = 0;
            for (; i < infos.Length - 1; i++)
            {
                temp += infos[i] + ",";
            }
            temp += infos[i];
            return temp;
        }
    };
}
