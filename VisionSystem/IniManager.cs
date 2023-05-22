using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUtill;

namespace VisionSystem
{
    public class IniManager
    {
        private IniFile config  = new IniFile(Environment.CurrentDirectory + "\\Config.ini");

        public string ResultPath
        {
            get
            {
                return config.GetString("Result", "Path", "E:\\Result");
            }
        }

        public int LogicalStationNumber
        {
            get
            {
                return config.GetInt32("PLC", "LogicalStationNumber", 1);
            }
        }

        public int ReadTickInterval
        {
            get
            {
                return config.GetInt32("PLC", "Read Tick Interval", 300);
            }
        }

        public string ReadAddr
        {
            get { return config.GetString("PLC", "Read Addr", "D3000"); }
        }

        public int ReadSize
        {
            get
            {
                return config.GetInt32("PLC", "Read Size", 1);
            }
        }

        public string ReadAddrHeartBeat { get { return config.GetString("PLC Read Addr", "HeartBeat", "D3000"); } }
        public string ReadAddrKind { get { return config.GetString("PLC Read Addr", "Kind", "D3001"); } }
        public string ReadAddrSeq { get { return config.GetString("PLC Read Addr", "SEQ", "D3002"); } }
        public string ReadAddrBodyNumber { get { return config.GetString("PLC Read Addr", "BodyNumber", "D3003"); } }
        public string ReadAddrVisionStart { get { return config.GetString("PLC Read Addr", "VisionStart", "D3004"); } }

        public int ReadSizeHeartBeat { get { return config.GetInt32("PLC Read Size", "HeartBeat", 1); } }
        public int ReadSizeKind { get { return config.GetInt32("PLC Read Size", "Kind", 1); } }
        public int ReadSizeSeq { get { return config.GetInt32("PLC Read Size", "SEQ", 1); } }
        public int ReadSizeBodyNumber { get { return config.GetInt32("PLC Read Size", "BodyNumber", 1); } }
        public int ReadSizeVisionStart { get { return config.GetInt32("PLC Read Size", "VisionStart", 1); } }

        public string WriteAddrHeartBeat { get { return config.GetString("PLC Write Addr", "HeartBeat", "D3100"); } }
        public string WriteAddrComplete { get { return config.GetString("PLC Write Addr", "Complete", "D3101"); } }
        public string WriteAddrResult { get { return config.GetString("PLC Write Addr", "Result", "D3102"); } }
        public string WriteAddrLight1 { get { return config.GetString("PLC Write Addr", "LIGHT 1", "D3103"); } }
        public string WriteAddrLight2 { get { return config.GetString("PLC Write Addr", "LIGHT 2", "D3103"); } }
        public string WriteAddrLight3 { get { return config.GetString("PLC Write Addr", "LIGHT 3", "D3103"); } }
        public string WriteAddrLight4 { get { return config.GetString("PLC Write Addr", "LIGHT 4", "D3103"); } }
    }
}
