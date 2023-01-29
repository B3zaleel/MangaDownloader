using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MangaDownloader.Converters;

public class CoverImageToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
        {
            var cover = (CoverImage)value;
            if (cover.Data != null)
            {
                var imgStream = new MemoryStream(cover.Data);
                imgStream.Seek(0, 0);
                BitmapDecoder imgSrcBmp = null;

                switch (cover.Type)
                {
                    case ImageTypes.BMP:
                        imgSrcBmp = new BmpBitmapDecoder(imgStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        break;
                    case ImageTypes.GIF:
                        imgSrcBmp = new GifBitmapDecoder(imgStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        break;
                    case ImageTypes.JPEG:
                    case ImageTypes.JPG:
                        imgSrcBmp = new JpegBitmapDecoder(imgStream, BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
                        break;
                    case ImageTypes.PNG:
                        imgSrcBmp = new PngBitmapDecoder(imgStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        break;
                    case ImageTypes.TIFF:
                        imgSrcBmp = new TiffBitmapDecoder(imgStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        break;
                    case ImageTypes.None:
                    default:
                        break;
                }

                return imgSrcBmp?.Frames[0] ?? null;
            }
            return null;
        }
        else
        {
            return null;
        }
    }
    //Not complete
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
        {
            var imgSrc = (BitmapFrame)value;
            var imgType = (ImageTypes)Enum.Parse(typeof(ImageTypes), imgSrc.Decoder.CodecInfo.MimeTypes.Split('/')[1].ToUpper());

            //byte data not retrieved

            return new CoverImage()
            {
                Data = new byte[] { },
                Type = imgType
            };
        }
        return null;
    }
}

public class CoverImageToMenuItemVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var coverImage = (CoverImage)value;

        if (coverImage != null && coverImage.Type != ImageTypes.None && coverImage.Data.Length > 0)
        {
            return Visibility.Collapsed;
        }
        return Visibility.Visible;
    }
    //It shouldn't be allowed or possible to convert this change. It's a one-way conversion
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Cannot convert Visibilty to CoverImage type.");
    }
}

public class AvailabilityToLoadingIconVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(bool)value)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Collapsed;
    }
}

public class AvailabilityToMangaIconVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(bool)value)
        {
            return Visibility.Collapsed;
        }
        else
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}
