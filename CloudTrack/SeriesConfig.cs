using System.Linq;
using System.Windows.Media;

namespace Loouq.CloudTrack
{
    public class SeriesConfig
    {
        public string Name { get; set; }
        public string Units { get; set; }
        public string Path { get; set; }
        public Brush StrokeColor { get; set; }
        public double LineSmoothness { get; set; }
        public int PrefetchMinutes { get; set; }


        public string GetName()
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;
            var pathName = Path.Split('.').Last();
            return pathName;
        }
    }
}
