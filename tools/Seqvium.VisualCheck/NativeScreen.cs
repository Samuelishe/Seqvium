// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Seqvium.VisualCheck;

/// <summary>Optional screen capture of this exact foreground window. Never forces foreground.</summary>
internal static class NativeScreen
{
    public static string Capture(Window window, string path)
    {
        if (!OperatingSystem.IsWindows()) return "not-run: Windows only";
        nint handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        if (handle == 0 || GetForegroundWindow() != handle) return "refused: foreground guard";
        if (!GetClientRect(handle, out var rectangle)) return "unavailable: client bounds";
        var origin = new NativePoint(); if (!ClientToScreen(handle, ref origin)) return "unavailable: screen position";
        int width = rectangle.Right, height = rectangle.Bottom;
        if (width <= 0 || height <= 0) return "unavailable: empty client";
        nint source = GetDC(0), memory = 0, bitmap = 0, previous = 0;
        try
        {
            memory = CreateCompatibleDC(source);
            var header = new BitmapHeader { Size = (uint)Marshal.SizeOf<BitmapHeader>(), Width = width, Height = -height, Planes = 1, Bits = 32 };
            bitmap = CreateDIBSection(source, ref header, 0, out var pixels, 0, 0);
            if (memory == 0 || bitmap == 0 || pixels == 0) return "unavailable: GDI surface";
            previous = SelectObject(memory, bitmap);
            if (!BitBlt(memory, 0, 0, width, height, source, origin.X, origin.Y, 0x00CC0020)) return "unavailable: screen read";
            if (GetForegroundWindow() != handle) return "refused: foreground changed during capture";
            using var image = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
            using (var target = image.Lock())
            {
                var row = new byte[width * 4];
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(pixels + y * width * 4, row, 0, row.Length);
                    Marshal.Copy(row, 0, target.Address + y * target.RowBytes, row.Length);
                }
            }
            image.Save(path, PngBitmapEncoderOptions.Default); return "passed: actual Windows screen pixels";
        }
        finally
        {
            if (previous != 0) SelectObject(memory, previous);
            if (bitmap != 0) DeleteObject(bitmap);
            if (memory != 0) DeleteDC(memory);
            if (source != 0) ReleaseDC(0, source);
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapHeader { public uint Size; public int Width, Height; public ushort Planes, Bits; public uint Compression, SizeImage; public int XResolution, YResolution; public uint ColorsUsed, ColorsImportant; }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint window, ref NativePoint point);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateDIBSection(nint dc, ref BitmapHeader info, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint bitmap);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint dest, int x, int y, int width, int height, nint source, int sx, int sy, uint mode);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint item);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
}
