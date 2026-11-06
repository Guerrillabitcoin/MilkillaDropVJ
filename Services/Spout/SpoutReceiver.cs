using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace MilkillaDropVJ.Services.Spout;

/// <summary>
/// Receptor Spout para la vista previa: abre la textura compartida del emisor con Direct3D 11,
/// la copia a una textura de lectura y la vuelca en un WriteableBitmap de WPF.
/// Sirve para emisores DX9 (como BeatDrop) y DX11. Debe usarse siempre desde el hilo de la interfaz.
/// </summary>
public sealed class SpoutReceiver : IDisposable
{
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private ID3D11Texture2D? _shared;
    private ID3D11Texture2D? _staging;
    private Mutex? _accessMutex;
    private string? _openSender;
    private uint _openHandle;
    private uint _openWidth;
    private uint _openHeight;
    private bool _swapRedBlue;
    private byte[]? _swizzleBuffer;

    /// <summary>Imagen con el último fotograma recibido. Cambia de objeto si cambia el tamaño.</summary>
    public WriteableBitmap? Bitmap { get; private set; }

    /// <summary>Formato DXGI de la textura abierta, para mostrarlo.</summary>
    public string FormatName { get; private set; } = "";

    /// <summary>
    /// Copia el fotograma actual del emisor. Devuelve false si no hay fotograma nuevo (mutex ocupado).
    /// Lanza excepción si la textura no se puede abrir o el formato no está soportado.
    /// </summary>
    public bool Receive(SpoutSenderInfo info)
    {
        if (info.ShareHandle == 0)
        {
            throw new NotSupportedException("El emisor no comparte textura de GPU (modo memoria de Spout no soportado todavía).");
        }

        EnsureDevice();

        if (_shared == null
            || info.Name != _openSender
            || info.ShareHandle != _openHandle
            || info.Width != _openWidth
            || info.Height != _openHeight)
        {
            Open(info);
        }

        var context = _context!;
        bool locked = false;
        try
        {
            if (_accessMutex != null)
            {
                try
                {
                    locked = _accessMutex.WaitOne(4);
                }
                catch (AbandonedMutexException)
                {
                    locked = true;
                }
                if (!locked)
                {
                    return false;
                }
            }
            context.CopyResource(_staging!, _shared!);
        }
        finally
        {
            if (locked)
            {
                _accessMutex!.ReleaseMutex();
            }
        }

        var mapped = context.Map(_staging!, 0, MapMode.Read, MapFlags.None);
        try
        {
            CopyToBitmap(mapped.DataPointer, (int)mapped.RowPitch);
        }
        finally
        {
            context.Unmap(_staging!, 0);
        }
        return true;
    }

    private void EnsureDevice()
    {
        if (_device != null)
        {
            return;
        }

        FeatureLevel[] levels =
        {
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0,
        };

        D3D11.D3D11CreateDevice(
            IntPtr.Zero,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            levels,
            out ID3D11Device device,
            out FeatureLevel _,
            out ID3D11DeviceContext context).CheckError();

        _device = device;
        _context = context;
    }

    private void Open(SpoutSenderInfo info)
    {
        ReleaseTextures();

        // Spout guarda el handle en 32 bits y lo amplía con signo (LongToHandle).
        var handle = (IntPtr)(int)info.ShareHandle;
        _shared = _device!.OpenSharedResource<ID3D11Texture2D>(handle);

        var desc = _shared.Description;
        _swapRedBlue = desc.Format switch
        {
            Format.B8G8R8A8_UNorm or Format.B8G8R8A8_UNorm_SRgb or
            Format.B8G8R8X8_UNorm or Format.B8G8R8X8_UNorm_SRgb => false,
            Format.R8G8B8A8_UNorm or Format.R8G8B8A8_UNorm_SRgb => true,
            _ => throw new NotSupportedException($"Formato de textura Spout no soportado en la vista previa: {desc.Format}."),
        };
        FormatName = desc.Format.ToString();

        var stagingDesc = new Texture2DDescription
        {
            Width = desc.Width,
            Height = desc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = desc.Format,
            SampleDescription = SampleDescription.Default,
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None,
        };
        _staging = _device.CreateTexture2D(stagingDesc);

        if (Bitmap == null || Bitmap.PixelWidth != (int)desc.Width || Bitmap.PixelHeight != (int)desc.Height)
        {
            // Bgr32: se ignora el alfa, que en algunos emisores viene a 0 y dejaría la vista en negro.
            Bitmap = new WriteableBitmap((int)desc.Width, (int)desc.Height, 96, 96, PixelFormats.Bgr32, null);
        }

        _accessMutex = Mutex.TryOpenExisting(info.Name + "_SpoutAccessMutex", out var mutex) ? mutex : null;

        _openSender = info.Name;
        _openHandle = info.ShareHandle;
        _openWidth = info.Width;
        _openHeight = info.Height;
    }

    private void CopyToBitmap(IntPtr data, int rowPitch)
    {
        var bitmap = Bitmap!;
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        var rect = new Int32Rect(0, 0, width, height);

        if (!_swapRedBlue)
        {
            bitmap.WritePixels(rect, data, rowPitch * height, rowPitch);
            return;
        }

        int stride = width * 4;
        if (_swizzleBuffer == null || _swizzleBuffer.Length != stride * height)
        {
            _swizzleBuffer = new byte[stride * height];
        }
        var buffer = _swizzleBuffer;
        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(data + y * rowPitch, buffer, y * stride, stride);
        }
        for (int i = 0; i < buffer.Length; i += 4)
        {
            (buffer[i], buffer[i + 2]) = (buffer[i + 2], buffer[i]);
        }
        bitmap.WritePixels(rect, buffer, stride, 0);
    }

    /// <summary>Suelta la textura abierta (por ejemplo, al cambiar de emisor o tras un error).</summary>
    public void ReleaseTextures()
    {
        _staging?.Dispose();
        _staging = null;
        _shared?.Dispose();
        _shared = null;
        _accessMutex?.Dispose();
        _accessMutex = null;
        _openSender = null;
        _openHandle = 0;
        _openWidth = 0;
        _openHeight = 0;
    }

    public void Dispose()
    {
        ReleaseTextures();
        _context?.Dispose();
        _context = null;
        _device?.Dispose();
        _device = null;
    }
}
