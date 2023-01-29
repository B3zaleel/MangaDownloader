using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaDownloader;

public class CoverImage
{
    public ImageTypes Type { get; set; }
    public byte[] Data { get; set; }
    public string Address { get; set; }
}
