using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Editor.GameProject;

public class Component
{
}

public class NameComponent : Component
{
    public string Name { get; set; } = string.Empty;
}

public class Vector3(float x, float y, float z)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public float Z { get; set; } = z;

    public override string ToString() => $"{X}, {Y}, {Z}";
}

public class Quaternion(float x, float y, float z, float w)
{
    public static Quaternion Identity { get; } = new(0, 0, 0, 1);
    
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public float Z { get; set; } = z;
    public float W { get; set; } = w;

    public override string ToString() => $"{X}, {Y}, {Z}, {W}";
}

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected a JSON array for Vector3.");

        reader.Read();
        float x = reader.GetSingle();
        reader.Read();
        float y = reader.GetSingle();
        reader.Read();
        float z = reader.GetSingle();
        reader.Read(); // Move past EndArray

        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}

public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected a JSON array for Quaternion.");

        reader.Read();
        float x = reader.GetSingle();
        reader.Read();
        float y = reader.GetSingle();
        reader.Read();
        float z = reader.GetSingle();
        reader.Read();
        float w = reader.GetSingle();
        reader.Read(); // Move past EndArray

        return new Quaternion(x, y, z, w);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);
        writer.WriteEndArray();
    }
}

public class TransformComponent : Component
{
    public Vector3 Position { get; set; } = new(0, 0, 0);
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public float Scale { get; set; }
}

public class ModelComponent : Component
{
    public string Mesh { get; set; } = string.Empty;
    public string Texture { get; set; } = string.Empty;
}

public class CameraComponent : Component
{
    public float Fov { get; set; }
}

public class BoundingBoxComponent : Component
{
    public Vector3 MinLocal { get; set; } = new(0, 0, 0);
    public Vector3 MaxLocal { get; set; } = new(0, 0, 0);
}

public class Entity
{
    public EntityId Id { get; set; }
    public Dictionary<string, Component> Components { get; set; } = [];
}

public class Scene
{
    public Entity[] Entities { get; set; } = [];
}

public class EntityConverter : JsonConverter<Entity>
{
    public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var rootElement = doc.RootElement;

        var id = rootElement.GetProperty("id").GetUInt64();
        var components = new Dictionary<string, Component>();

        foreach (var componentProperty in rootElement.GetProperty("components").EnumerateObject())
        {
            var componentTypeName = componentProperty.Name;
            var componentType = Type.GetType($"Editor.GameProject.{componentTypeName}, {Assembly.GetExecutingAssembly().FullName}");
            if (componentType == null)
            {
                Console.WriteLine("Tried to parse a component with an unknown type: " + componentTypeName);
                continue;
            }

            var componentJson = componentProperty.Value;

            var component = (Component?)JsonSerializer.Deserialize(componentJson.GetRawText(), componentType, options);
            if (component != null)
                components[componentTypeName] = component;
        }

        return new Entity { Id = id, Components = components };
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}