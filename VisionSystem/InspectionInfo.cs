using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VisionSystem
{
    public class InspectionInfo : INotifyPropertyChanged
    {
        private DateTime dateTIme;
        public DateTime DateTime
        {
            get
            {
                return dateTIme;
            }
            set
            {
                dateTIme = value;
                NotifyPropertyChanged("DateTime");
            }
        }

        private string kind = "";
        public string Kind
        {
            get
            {
                return kind;
            }
            set
            {
                kind = value;
                NotifyPropertyChanged("Kind");
            }
        }


        private string bodyNumber = "";
        public string BodyNumber
        {
            get
            {
                return bodyNumber;
            }
            set
            {
                bodyNumber = value;
                NotifyPropertyChanged("BodyNumber");
            }
        }

        private string seq = "";
        public string Seq
        {
            get
            {
                return seq;
            }
            set
            {
                seq = value;
                NotifyPropertyChanged("Seq");
            }
        }

        private string result = "";
        public string Result 
        {
            get
            {
                return result;
            }
            set
            {
                result = value;
                NotifyPropertyChanged("Result");
            }
        }

        private CarKindInfo carKindInfo = null;
        public CarKindInfo CarKindInfo
        {
            get
            {
                return carKindInfo;
            }
            set
            {
                carKindInfo = value;
                NotifyPropertyChanged("CarKindInfo");
            }
        }

        public InspectionInfo(string kind)
        {
            Kind = kind;
            DateTime = DateTime.Now;
            DateTimeStr = DateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string DateTimeStr { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
