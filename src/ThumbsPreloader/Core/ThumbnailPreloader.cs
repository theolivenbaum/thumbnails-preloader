using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ThumbsPreloader.Core;

[SupportedOSPlatform("windows")]
public sealed class ThumbnailPreloader
{
    private static readonly Guid IIdIShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    private static readonly Guid ClsidLocalThumbnailCache = new("50ef4544-ac9f-4a8e-b21b-8a26180db13f");

    private readonly IThumbnailCache _cache;

    public ThumbnailPreloader()
    {
        var cacheType = Type.GetTypeFromCLSID(ClsidLocalThumbnailCache)
            ?? throw new InvalidOperationException("LocalThumbnailCache COM class is not available.");
        _cache = (IThumbnailCache)Activator.CreateInstance(cacheType)!;
    }

    public void PreloadThumbnail(string filePath)
    {
        IShellItem? shellItem = null;
        ISharedBitmap? bmp = null;
        try
        {
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, IIdIShellItem, out shellItem);
            _cache.GetThumbnail(shellItem, 256, WTS_FLAGS.WTS_EXTRACTINPROC, out bmp, out _, out _);
        }
        catch (Exception)
        {
            // Best-effort: a single file failing must not abort the batch.
        }
        finally
        {
            if (bmp != null) Marshal.ReleaseComObject(bmp);
            if (shellItem != null) Marshal.ReleaseComObject(shellItem);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        [In] IntPtr pbc,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [Out, MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("F676C15D-596A-4ce2-8234-33996F445DB1")]
    private interface IThumbnailCache
    {
        uint GetThumbnail(
            [In] IShellItem pShellItem,
            [In] uint cxyRequestedThumbSize,
            [In] WTS_FLAGS flags,
            [Out, MarshalAs(UnmanagedType.Interface)] out ISharedBitmap ppvThumb,
            [Out] out WTS_CACHEFLAGS pOutFlags,
            [Out] out WTS_THUMBNAILID pThumbnailID);

        void GetThumbnailByID(
            [In, MarshalAs(UnmanagedType.Struct)] WTS_THUMBNAILID thumbnailID,
            [In] uint cxyRequestedThumbSize,
            [Out, MarshalAs(UnmanagedType.Interface)] out ISharedBitmap ppvThumb,
            [Out] out WTS_CACHEFLAGS pOutFlags);
    }

    [Flags]
    private enum WTS_FLAGS : uint
    {
        WTS_EXTRACT = 0x00000000,
        WTS_INCACHEONLY = 0x00000001,
        WTS_FASTEXTRACT = 0x00000002,
        WTS_SLOWRECLAIM = 0x00000004,
        WTS_FORCEEXTRACTION = 0x00000008,
        WTS_EXTRACTDONOTCACHE = 0x00000020,
        WTS_SCALETOREQUESTEDSIZE = 0x00000040,
        WTS_SKIPFASTEXTRACT = 0x00000080,
        WTS_EXTRACTINPROC = 0x00000100,
    }

    [Flags]
    private enum WTS_CACHEFLAGS : uint
    {
        WTS_DEFAULT = 0x00000000,
        WTS_LOWQUALITY = 0x00000001,
        WTS_CACHED = 0x00000002,
    }

    [StructLayout(LayoutKind.Sequential, Size = 16), Serializable]
    private struct WTS_THUMBNAILID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        private byte[] _rgbKey;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppv);

        void GetParent(out IShellItem ppsi);
        void GetDisplayName(uint sigdnName, out IntPtr ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport]
    [Guid("091162a4-bc96-411f-aae8-c5122cd03363")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISharedBitmap
    {
        uint Detach(out IntPtr phbm);
        uint GetFormat(out uint pat);
        uint GetSharedBitmap(out IntPtr phbm);
        uint GetSize(out SIZE pSize);
        uint InitializeBitmap(IntPtr hbm, uint wtsAT);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int cx;
        public int cy;
    }
}
