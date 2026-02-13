using System;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public struct Resolution : IYamlConvertible
{
    public int width;
    public int height;

    public Resolution(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        string[] parts = scalar.Value.Split('x');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid resolution format. Expected format: 'widthxheight'");
        }

        if (!int.TryParse(parts[0], out width) || !int.TryParse(parts[1], out height))
        {
            throw new FormatException("Invalid resolution format. Width and height must be integers.");
        }
    }

     public override string ToString()
    {
        return $"{width}x{height}";
    }

    public static implicit operator Resolution(string value)
    {
        string[] parts = value.Split('x');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid resolution format. Expected format: 'widthxheight'");
        }

        if (!int.TryParse(parts[0], out int width) || !int.TryParse(parts[1], out int height))
        {
            throw new FormatException("Invalid resolution format. Width and height must be integers.");
        }

        return new Resolution(width, height);
    }

     public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
    {
        emitter.Emit(new Scalar(this.ToString()));
    }
}
