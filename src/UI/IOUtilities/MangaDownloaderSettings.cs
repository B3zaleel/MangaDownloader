using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDXAMLUI.IOUtilities;

public enum SerializationType
{
    JSON,
    JSONMinified,
    MDBin,
}

public class MangaDownloaderSettings
{
    public static void Serialize(string path, List<Manga> mangas, SerializationType serializationType)
    {
        switch (serializationType)
        {
            case SerializationType.JSON:
                JSONFormat.Serialize(path, mangas, false);
                break;
            case SerializationType.JSONMinified:
                JSONFormat.Serialize(path, mangas, true);
                break;
            case SerializationType.MDBin:
                MDBinFormat.Serialize(path, mangas);
                break;
            default:
                throw new InvalidOperationException("Unimplemeted Serialization Type");
        }
    }

    public static List<Manga> Deserialize(string path, SerializationType serializationType)
    {
        switch (serializationType)
        {
            case SerializationType.JSON:
            case SerializationType.JSONMinified:
                return JSONFormat.Deserialize(path);
            case SerializationType.MDBin:
                return MDBinFormat.Deserialize(path);
            default:
                throw new InvalidOperationException("Unimplemeted Serialization Type");
        }
    }
}
