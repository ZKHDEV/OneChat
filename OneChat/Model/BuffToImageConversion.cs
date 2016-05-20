using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace OneChat.Model
{
    public class BuffToImageConversion : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            byte[] buffer = value as byte[];
            IRandomAccessStream stream = new InMemoryRandomAccessStream();
            DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0UL));
            writer.WriteBytes(buffer);
            Task task = Task.Run(async () => await writer.StoreAsync());
            task.Wait();   //去掉后有时不能显示图片
            BitmapImage image = new BitmapImage();
            image.DecodePixelWidth = 50;
            stream.Seek(0UL);
            image.SetSource(stream);
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
