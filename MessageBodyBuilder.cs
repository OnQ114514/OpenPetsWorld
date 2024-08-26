using System.Drawing;
using Manganese.Text;
using Sora.Entities;
using Sora.Entities.Segment;

namespace OpenPetsWorld;

public class MessageBodyBuilder
{
    private readonly MessageBody _body = new();

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

    public MessageBodyBuilder Image(Image image)
    {
        _body.Add(SoraSegment.Image("base64://" + Tools.ToBase64(image)));
        return this;
    }
    
    public MessageBodyBuilder ImageFromBase64(string base64)
    {
        _body.Add(SoraSegment.Image("base64://" + base64));
        return this;
    }
}