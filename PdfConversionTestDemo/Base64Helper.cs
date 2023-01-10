namespace PdfConversionTestDemo
{
    public static class Base64Helper
    {
        public static Stream FromBase64String(this string base64String)
        {
            var bytes = Convert.FromBase64String(base64String);
            var stream = new MemoryStream(bytes);
            return stream;
        }

        public static string ToBase64String(this Stream stream)
        {
            byte[] bytes;
            if (stream is MemoryStream ms)
            {
                bytes = ms.ToArray();
            }
            else
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            var base64String = Convert.ToBase64String(bytes);
            return base64String;
        }
    }
}
