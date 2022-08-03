using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Downloader
{
    public class TrexVideo
    {
        public string Title { get; set; }
        public string AddTime { get; set; }
        public double Duration { get; set; }
        public int Views { get; set; }
        public int Rating { get; set; }
        public int Quality { get; set; }
        public string Type { get; set; }
        public int Id { get; set; }
        public string Link { get; set; }
        private int year { get; set; }
        public int Year
        {
            get
            {
                return year;
            }
            set
            {
                if (year != value)
                {
                    year = value;
                    ChangeEvent?.Invoke(this);
                }
            }
        }
        private string notes;
        public string Notes
        {
            get
            {
                if (notes == null)
                    return null;
                return notes;
            }
            set
            {
                if (notes != value)
                {
                    notes = value;
                    ChangeEvent?.Invoke(this);
                }
            }
        }
        public delegate void TrexVideoHandler(TrexVideo video);
        public event TrexVideoHandler ChangeEvent;

    }
    [Serializable, XmlRoot("TrexVideos")]
    public class TrexVideos
    {
        public List<TrexVideo> TrexVideosList { get; set; }
        public TrexVideos() { }
        public TrexVideos(List<TrexVideo> trexVideos)
        {
            TrexVideosList = trexVideos;            
        }

    }
}
