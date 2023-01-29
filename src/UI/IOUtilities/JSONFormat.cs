using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MangaDownloader.IOUtilities;

public static class JSONFormat
{
    public static void Serialize(string path, List<Manga> mangas, bool minified = false)
    {
        var stringWriter = new StringWriter();
        var jsonTextWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = minified ? Formatting.None : Formatting.Indented,
            IndentChar = '\t',
            Indentation = 1,
        };
        jsonTextWriter.WriteStartObject();

        SerializeMangas(jsonTextWriter, mangas);

        jsonTextWriter.WriteEndObject();
        File.WriteAllText(path, stringWriter.ToString());
    }

    private static void SerializeMangas(JsonTextWriter jsonTextWriter, IEnumerable<Manga> mangas)
    {
        jsonTextWriter.WritePropertyName("Mangas");
        jsonTextWriter.WriteStartArray();
        foreach (var manga in mangas)
        {
            jsonTextWriter.WriteStartObject();
            jsonTextWriter.WritePropertyName("Title");
            jsonTextWriter.WriteValue(manga.Title ?? "");
            jsonTextWriter.WritePropertyName("BookFormat");
            jsonTextWriter.WriteValue(manga.BookFormat.ToString());
            jsonTextWriter.WritePropertyName("RetrieverName");
            jsonTextWriter.WriteValue(manga.RetrieverName ?? "");

            if (manga.Cover != null)
            {
                jsonTextWriter.WritePropertyName("CoverImage");
                jsonTextWriter.WriteStartObject();
                jsonTextWriter.WritePropertyName("Type");
                jsonTextWriter.WriteValue(manga.Cover.Type.ToString());
                jsonTextWriter.WritePropertyName("Data");
                jsonTextWriter.WriteValue(Convert.ToBase64String(manga.Cover.Data));
                jsonTextWriter.WritePropertyName("Address");
                jsonTextWriter.WriteValue(manga.Cover.Address);
                jsonTextWriter.WriteEndObject();
            }
            else
            {
                jsonTextWriter.WritePropertyName("CoverImage");
                jsonTextWriter.WriteValue(manga.Cover);
            }

            jsonTextWriter.WritePropertyName("OtherProps");
            jsonTextWriter.WriteStartArray();
            foreach (var otherProp in manga.OtherProps)
            {
                jsonTextWriter.WriteStartObject();
                jsonTextWriter.WritePropertyName("Key");
                jsonTextWriter.WriteValue(otherProp.Key ?? "");
                jsonTextWriter.WritePropertyName("Value");
                jsonTextWriter.WriteValue(otherProp.Value ?? "");
                jsonTextWriter.WriteEndObject();
            }
            jsonTextWriter.WriteEndArray();

            SerializeChapters(jsonTextWriter, manga.Chapters);

            jsonTextWriter.WritePropertyName("Description");
            jsonTextWriter.WriteValue(manga.Description ?? "");

            jsonTextWriter.WritePropertyName("Genres");
            jsonTextWriter.WriteStartArray();
            foreach (var genre in manga.Genres)
            {
                jsonTextWriter.WriteValue(genre ?? "");
            }
            jsonTextWriter.WriteEndArray();

            jsonTextWriter.WritePropertyName("Address");
            jsonTextWriter.WriteValue(manga.Address ?? "");

            jsonTextWriter.WriteEndObject();
        }
        jsonTextWriter.WriteEndArray();
    }

    private static void SerializeChapters(JsonTextWriter jsonTextWriter, IEnumerable<Chapter> chapters)
    {
        jsonTextWriter.WritePropertyName("Chapters");
        jsonTextWriter.WriteStartArray();
        foreach (var chapter in chapters)
        {
            jsonTextWriter.WriteStartObject();
            jsonTextWriter.WritePropertyName("Id");
            jsonTextWriter.WriteValue(chapter.Id);
            jsonTextWriter.WritePropertyName("Title");
            jsonTextWriter.WriteValue(chapter.Title ?? "");

            SerializePages(jsonTextWriter, chapter.Pages);

            jsonTextWriter.WritePropertyName("Address");
            jsonTextWriter.WriteValue(chapter.Address ?? "");

            jsonTextWriter.WriteEndObject();
        }
        jsonTextWriter.WriteEndArray();
    }

    private static void SerializePages(JsonTextWriter jsonTextWriter, IEnumerable<Page> pages)
    {
        jsonTextWriter.WritePropertyName("Pages");
        jsonTextWriter.WriteStartArray();
        foreach (var page in pages)
        {
            jsonTextWriter.WriteStartObject();
            jsonTextWriter.WritePropertyName("Saved");
            jsonTextWriter.WriteValue(page.Saved);
            jsonTextWriter.WritePropertyName("Address");
            jsonTextWriter.WriteValue(page.Address ?? "");
            jsonTextWriter.WriteEndObject();
        }
        jsonTextWriter.WriteEndArray();
    }

    public static List<Manga> Deserialize(string path)
    {
        var stringReader = new StringReader(File.ReadAllText(path));
        var jsonTextReader = new JsonTextReader(stringReader);
        IEnumerable<Manga> mangas = null;
        ReadToken(ref jsonTextReader, JsonToken.StartObject);

        DeserializeMangas(jsonTextReader, ref mangas);
        ReadToken(ref jsonTextReader, JsonToken.EndObject);

        return new List<Manga>(mangas);
    }

    private static void DeserializeMangas(JsonTextReader jsonTextReader, ref IEnumerable<Manga> mangas)
    {
        mangas = new List<Manga>();
        bool isEndOfMangasArray = false;
        ReadProperty(ref jsonTextReader, "Mangas", JsonToken.StartArray);

        while (!isEndOfMangasArray)
        {
            jsonTextReader.Read();
            if (jsonTextReader.TokenType == JsonToken.StartObject)
            {
                var manga = new Manga();
                ReadProperty(ref jsonTextReader, "Title", JsonToken.String);
                manga.Title = (string)jsonTextReader.Value ?? "";

                ReadProperty(ref jsonTextReader, "BookFormat", JsonToken.String);

                if (!Enum.TryParse((string)jsonTextReader.Value, out BookFormats bookFormat))
                    throw new InvalidDataException("Unexpected token");
                manga.BookFormat = bookFormat;
                ReadProperty(ref jsonTextReader, "RetrieverName", JsonToken.String);
                manga.RetrieverName = (string)jsonTextReader.Value ?? "";

                ReadToken(ref jsonTextReader, JsonToken.PropertyName);
                if (!((string)jsonTextReader.Value).Equals("CoverImage"))
                    throw new InvalidDataException("Unexpected token");
                jsonTextReader.Read();
                if (jsonTextReader.TokenType != JsonToken.Null)
                {
                    ReadProperty(ref jsonTextReader, "Type", JsonToken.String);
                    if (!Enum.TryParse((string)jsonTextReader.Value, out ImageTypes imgType))
                        throw new InvalidDataException("Unexpected token");
                    ReadProperty(ref jsonTextReader, "Data", JsonToken.String);
                    var imgData = Convert.FromBase64String((string)jsonTextReader.Value);
                    ReadProperty(ref jsonTextReader, "Address", JsonToken.String);
                    var imgAddress = (string)jsonTextReader.Value;
                    manga.Cover = new CoverImage()
                    {
                        Type = imgType,
                        Data = imgData,
                        Address = imgAddress
                    };
                    ReadToken(ref jsonTextReader, JsonToken.EndObject);
                }
                else
                {
                    manga.Cover = new CoverImage()
                    {
                        Type = ImageTypes.None,
                        Data = new byte[0],
                        Address = ""
                    };
                }

                manga.OtherProps = new Dictionary<string, string>();
                bool isEndOfOtherPropsArray = false;
                ReadProperty(ref jsonTextReader, "OtherProps", JsonToken.StartArray);
                while (!isEndOfOtherPropsArray)
                {
                    jsonTextReader.Read();
                    if (jsonTextReader.TokenType == JsonToken.StartObject)
                    {
                        ReadProperty(ref jsonTextReader, "Key", JsonToken.String);
                        var propKey = (string)jsonTextReader.Value ?? "";
                        ReadProperty(ref jsonTextReader, "Value", JsonToken.String);
                        var propValue = (string)jsonTextReader.Value ?? "";
                        ReadToken(ref jsonTextReader, JsonToken.EndObject);
                        manga.OtherProps.Add(propKey, propValue);
                    }
                    else if (jsonTextReader.TokenType == JsonToken.EndArray)
                        isEndOfOtherPropsArray = true;
                    else
                        throw new InvalidDataException("Unexpected token");
                }

                DeserializeChapters(jsonTextReader, manga);

                ReadProperty(ref jsonTextReader, "Description", JsonToken.String);
                manga.Description = (string)jsonTextReader.Value ?? "";

                //genres
                var genres = new List<string>();
                bool isEndOfGenresArray = false;
                ReadProperty(ref jsonTextReader, "Genres", JsonToken.StartArray);
                while (!isEndOfGenresArray)
                {
                    jsonTextReader.Read();
                    if (jsonTextReader.TokenType == JsonToken.String)
                        genres.Add((string)jsonTextReader.Value ?? "");
                    else if (jsonTextReader.TokenType == JsonToken.EndArray)
                        isEndOfGenresArray = true;
                    else
                        throw new InvalidDataException("Unexpected token");
                }
                manga.Genres = genres.ToArray();

                ReadProperty(ref jsonTextReader, "Address", JsonToken.String);
                manga.Address = (string)jsonTextReader.Value ?? "";
                ReadToken(ref jsonTextReader, JsonToken.EndObject);
                ((List<Manga>)mangas).Add(manga);
            }
            else if (jsonTextReader.TokenType == JsonToken.EndArray)
                isEndOfMangasArray = true;
            else
                throw new InvalidDataException("Unexpected token");
        }
    }

    private static void DeserializeChapters(JsonTextReader jsonTextReader, Manga parent)
    {
        parent.Chapters = new List<Chapter>();
        ReadProperty(ref jsonTextReader, "Chapters", JsonToken.StartArray);
        bool isEndOfChaptersArray = false;

        while (!isEndOfChaptersArray)
        {
            jsonTextReader.Read();
            if (jsonTextReader.TokenType == JsonToken.StartObject)
            {
                var chapter = new Chapter(parent);
                ReadProperty(ref jsonTextReader, "Id", JsonToken.Float);
                //Debug.WriteLine(jsonTextReader.Value);//
                chapter.Id = float.Parse(jsonTextReader.Value.ToString());
                ReadProperty(ref jsonTextReader, "Title", JsonToken.String);
                chapter.Title = (string)jsonTextReader.Value ?? "";

                DeserializePages(jsonTextReader, chapter);

                ReadProperty(ref jsonTextReader, "Address", JsonToken.String);
                chapter.Address = (string)jsonTextReader.Value ?? "";
                chapter.IsComplete = chapter.Pages.All(item => item.Saved);
                var savedPages = chapter.Pages.FindAll(item => item.Saved).Count();
                chapter.Progress = (byte)Math.Floor((savedPages / (float)chapter.Pages.Count) * 100d);
                ReadToken(ref jsonTextReader, JsonToken.EndObject);
                parent.Chapters.Add(chapter);
            }
            else if (jsonTextReader.TokenType == JsonToken.EndArray)
                isEndOfChaptersArray = true;
            else
                throw new InvalidDataException("Unexpected token");
        }
    }

    private static void DeserializePages(JsonTextReader jsonTextReader, Chapter parent)
    {
        parent.Pages = new List<Page>();
        ReadProperty(ref jsonTextReader, "Pages", JsonToken.StartArray);
        bool isEndOfPagesArray = false;

        while (!isEndOfPagesArray)
        {
            jsonTextReader.Read();
            if (jsonTextReader.TokenType == JsonToken.StartObject)
            {
                var page = new Page(parent);
                ReadProperty(ref jsonTextReader, "Saved", JsonToken.Boolean);
                page.Saved = (bool)jsonTextReader.Value;
                ReadProperty(ref jsonTextReader, "Address", JsonToken.String);
                page.Address = (string)jsonTextReader.Value ?? "";
                ReadToken(ref jsonTextReader, JsonToken.EndObject);
                parent.Pages.Add(page);
            }
            else if (jsonTextReader.TokenType == JsonToken.EndArray)
                isEndOfPagesArray = true;
            else
                throw new InvalidDataException("Unexpected token");
        }
    }

    private static void ReadToken(ref JsonTextReader jsonTextReader, JsonToken jsonToken)
    {
        jsonTextReader.Read();
        if (jsonTextReader.TokenType != jsonToken)
        {
            throw new InvalidDataException($"Unexpected token. Expected a token of type {jsonToken}");
        }
    }

    private static void ReadProperty(ref JsonTextReader jsonTextReader, string propName, JsonToken valueToken)
    {
        ReadToken(ref jsonTextReader, JsonToken.PropertyName);
        if ((string)jsonTextReader.Value == propName)
        {
            ReadToken(ref jsonTextReader, valueToken);
        }
        else
            throw new InvalidDataException($"Unexpected token. Expected a property with the name {propName}");
    }
}
