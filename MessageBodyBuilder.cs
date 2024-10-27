using Manganese.Text;
using SkiaSharp;
using Sora.Entities;
using Sora.Entities.Segment;

namespace OpenPetsWorld;

public class MessageBodyBuilder
{
    private readonly MessageBody _body = [];

    public MessageBody Build()
    {
        return _body;
    }

    public MessageBodyBuilder At(long uid)
    {
        _body.Add(SoraSegment.At(uid));
        return this;
    }
    
    public MessageBodyBuilder At(string uidText)
    {
        var uid = uidText.ToInt64();
        _body.Add(SoraSegment.At(uid));
        return this;
    }

    public MessageBodyBuilder Plain(string text)
    {
        _body.Add(text);
        return this;
    }

    public MessageBodyBuilder Image(string fullPath)
    {
        _body.Add(SoraSegment.Image(fullPath));
        return this;
    }
    
    public MessageBodyBuilder Image(SKImage image)
    {
        _body.Add(SoraSegment.Image("base64://" + ToBase64(image)));
        return this;
    }
    
    public MessageBodyBuilder Image(SKBitmap image)
    {
        _body.Add(SoraSegment.Image("base64://" + ToBase64(image)));
        return this;
    }

    private static string ToBase64(SKImage image)
    {
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using MemoryStream stream = new();
        data.SaveTo(stream);
        var bytes = stream.ToArray();
        return Convert.ToBase64String(bytes);
    }
    
    private static string ToBase64(SKBitmap image)
    {
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using MemoryStream stream = new();
        data.SaveTo(stream);
        var bytes = stream.ToArray();
        return Convert.ToBase64String(bytes);
    }
}