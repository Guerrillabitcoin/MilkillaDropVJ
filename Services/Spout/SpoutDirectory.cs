using System.IO.MemoryMappedFiles;
using System.Text;

namespace MilkillaDropVJ.Services.Spout;

/// <summary>Datos de un emisor Spout (estructura SharedTextureInfo de Spout 2, 280 bytes).</summary>
public sealed record SpoutSenderInfo(
    string Name,
    uint ShareHandle,
    uint Width,
    uint Height,
    uint Format,
    uint Usage,
    string Description);

/// <summary>
/// Lee la lista de emisores Spout de la memoria compartida de Windows, sin librerías nativas.
/// Mapas usados por Spout 2: "SpoutSenderNames" (nombres de 256 bytes seguidos),
/// "ActiveSenderName" (256 bytes) y uno por emisor, con su nombre, que guarda SharedTextureInfo.
/// </summary>
public static class SpoutDirectory
{
    private const int MaxNameLength = 256;
    private const int SharedTextureInfoSize = 280;

    public static IReadOnlyList<string> GetSenderNames()
    {
        var names = new List<string>();
        try
        {
            using var map = MemoryMappedFile.OpenExisting("SpoutSenderNames", MemoryMappedFileRights.Read);
            using var view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            var slot = new byte[MaxNameLength];
            for (long offset = 0; offset + MaxNameLength <= view.Capacity; offset += MaxNameLength)
            {
                view.ReadArray(offset, slot, 0, MaxNameLength);
                var name = ReadCString(slot, 0, MaxNameLength);
                if (name.Length == 0)
                {
                    break;
                }
                names.Add(name);
            }
        }
        catch (IOException)
        {
            // No hay ningún emisor Spout registrado.
        }
        catch (UnauthorizedAccessException)
        {
        }
        return names;
    }

    public static string? GetActiveSenderName()
    {
        try
        {
            using var map = MemoryMappedFile.OpenExisting("ActiveSenderName", MemoryMappedFileRights.Read);
            using var view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            var buffer = new byte[MaxNameLength];
            view.ReadArray(0, buffer, 0, MaxNameLength);
            var name = ReadCString(buffer, 0, MaxNameLength);
            return name.Length == 0 ? null : name;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static SpoutSenderInfo? GetSenderInfo(string senderName)
    {
        try
        {
            using var map = MemoryMappedFile.OpenExisting(senderName, MemoryMappedFileRights.Read);
            using var view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            if (view.Capacity < SharedTextureInfoSize)
            {
                return null;
            }
            var raw = new byte[SharedTextureInfoSize];
            view.ReadArray(0, raw, 0, SharedTextureInfoSize);
            return new SpoutSenderInfo(
                senderName,
                BitConverter.ToUInt32(raw, 0),
                BitConverter.ToUInt32(raw, 4),
                BitConverter.ToUInt32(raw, 8),
                BitConverter.ToUInt32(raw, 12),
                BitConverter.ToUInt32(raw, 16),
                ReadCString(raw, 20, 256));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Emisores registrados que siguen vivos (su mapa de información existe).</summary>
    public static IReadOnlyList<string> GetLiveSenderNames() =>
        GetSenderNames().Where(n => GetSenderInfo(n) != null).ToList();

    private static string ReadCString(byte[] buffer, int start, int maxLength)
    {
        int end = start;
        int limit = Math.Min(buffer.Length, start + maxLength);
        while (end < limit && buffer[end] != 0)
        {
            end++;
        }
        return Encoding.Latin1.GetString(buffer, start, end - start);
    }
}
