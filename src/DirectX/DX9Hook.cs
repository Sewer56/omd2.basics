using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using SharpDX.Direct3D9;

namespace omd2.basics.DirectX;

/// <summary>
/// Provides access to DirectX 9 VTables and function hooks.
/// </summary>
public class DX9Hook
{
    /// <summary>
    /// Contains the IDirect3DDevice9 VTable.
    /// </summary>
    public IVirtualFunctionTable DeviceVTable { get; private set; }

    /// <summary>
    /// Contains the IDirect3D9 VTable.
    /// </summary>
    public IVirtualFunctionTable Direct3D9VTable { get; private set; }

    /// <summary>
    /// Contains the IDirect3DDevice9Ex VTable.
    /// </summary>
    public IVirtualFunctionTable DeviceExVTable { get; private set; }

    /// <summary>
    /// Contains the IDirect3D9Ex VTable.
    /// </summary>
    public IVirtualFunctionTable Direct3D9ExVTable { get; private set; }

    public DX9Hook(IReloadedHooks hooks)
    {
        // Create temporary D3D9 device to extract VTables
        using var direct3D = new Direct3D();
        using var renderForm = new CustomWindow("OrcsMustModTemp");
        using var device = new Device(direct3D, 0, DeviceType.Hardware, IntPtr.Zero, 
            CreateFlags.HardwareVertexProcessing, GetParameters(direct3D, renderForm.Hwnd));
        
        Direct3D9VTable = hooks.VirtualFunctionTableFromObject(direct3D.NativePointer, 
            Enum.GetNames(typeof(IDirect3D9Methods)).Length);
        DeviceVTable = hooks.VirtualFunctionTableFromObject(device.NativePointer, 
            Enum.GetNames(typeof(IDirect3DDevice9Methods)).Length);

        // Create D3D9Ex device for Ex VTables
        using var direct3DEx = new Direct3DEx();
        using var renderFormEx = new CustomWindow("OrcsMustModTempEx");
        using var deviceEx = new DeviceEx(direct3DEx, 0, DeviceType.Hardware, IntPtr.Zero,
            CreateFlags.HardwareVertexProcessing, GetParameters(direct3DEx, renderFormEx.Hwnd));
        
        Direct3D9ExVTable = hooks.VirtualFunctionTableFromObject(direct3DEx.NativePointer,
            Enum.GetNames(typeof(IDirect3D9Methods)).Length);
        DeviceExVTable = hooks.VirtualFunctionTableFromObject(deviceEx.NativePointer,
            Enum.GetNames(typeof(IDirect3DDevice9Methods)).Length);
    }

    private static PresentParameters GetParameters(Direct3D d3d, IntPtr windowHandle)
    {
        var mode = d3d.GetAdapterDisplayMode(0);
        return new PresentParameters
        {
            BackBufferWidth = mode.Width,
            BackBufferHeight = mode.Height,
            BackBufferFormat = mode.Format,
            DeviceWindowHandle = windowHandle,
            SwapEffect = SwapEffect.Discard,
            Windowed = true,
            PresentationInterval = PresentInterval.Immediate
        };
    }

    #region Delegates

    /// <summary>
    /// IDirect3D9::CreateDevice
    /// </summary>
    [FunctionHookOptions(PreferRelativeJump = true)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [Function(CallingConventions.Stdcall)]
    public unsafe delegate int CreateDevice(
        IntPtr direct3DPointer,
        uint adapter,
        DeviceType deviceType,
        IntPtr hFocusWindow,
        CreateFlags behaviorFlags,
        ref PresentParameters presentParameters,
        IntPtr* ppReturnedDeviceInterface);

    /// <summary>
    /// IDirect3DDevice9::Reset
    /// </summary>
    [FunctionHookOptions(PreferRelativeJump = true)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [Function(CallingConventions.Stdcall)]
    public delegate int Reset(IntPtr device, ref PresentParameters presentParameters);

    /// <summary>
    /// IDirect3DDevice9::EndScene
    /// </summary>
    [FunctionHookOptions(PreferRelativeJump = true)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [Function(CallingConventions.Stdcall)]
    public delegate int EndScene(IntPtr device);

    /// <summary>
    /// IDirect3DDevice9::CreateTexture
    /// </summary>
    [FunctionHookOptions(PreferRelativeJump = true)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [Function(CallingConventions.Stdcall)]
    public unsafe delegate int CreateTexture(
        IntPtr devicePointer,
        int width,
        int height,
        int levels,
        Usage usage,
        Format format,
        Pool pool,
        void** ppTexture,
        void* sharedHandle);

    /// <summary>
    /// IDirect3DDevice9::CreateVertexBuffer
    /// </summary>
    [FunctionHookOptions(PreferRelativeJump = true)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [Function(CallingConventions.Stdcall)]
    public unsafe delegate int CreateVertexBuffer(
        IntPtr devicePointer,
        uint length,
        Usage usage,
        VertexFormat vertexFormat,
        Pool pool,
        void** ppVertexBuffer,
        void* sharedHandle);

    /// <summary>
    /// IDirect3DDevice9::CreateIndexBuffer
    /// </summary>
    [FunctionHookOptions(PreferRelativeJump = true)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [Function(CallingConventions.Stdcall)]
    public unsafe delegate int CreateIndexBuffer(
        IntPtr devicePointer,
        uint length,
        Usage usage,
        Format format,
        Pool pool,
        void** ppIndexBuffer,
        void* sharedHandle);

    #endregion
}

/// <summary>
/// IDirect3D9 method indices for VTable hooking.
/// </summary>
public enum IDirect3D9Methods
{
    QueryInterface,
    AddRef,
    Release,
    RegisterSoftwareDevice,
    GetAdapterCount,
    GetAdapterIdentifier,
    GetAdapterModeCount,
    EnumAdapterModes,
    GetAdapterDisplayMode,
    CheckDeviceType,
    CheckDeviceFormat,
    CheckDeviceMultiSampleType,
    CheckDepthStencilMatch,
    CheckDeviceFormatConversion,
    GetDeviceCaps,
    GetAdapterMonitor,
    CreateDevice
}

/// <summary>
/// IDirect3DDevice9 method indices for VTable hooking.
/// </summary>
public enum IDirect3DDevice9Methods
{
    QueryInterface,
    AddRef,
    Release,
    TestCooperativeLevel,
    GetAvailableTextureMem,
    EvictManagedResources,
    GetDirect3D,
    GetDeviceCaps,
    GetDisplayMode,
    GetCreationParameters,
    SetCursorProperties,
    SetCursorPosition,
    ShowCursor,
    CreateAdditionalSwapChain,
    GetSwapChain,
    GetNumberOfSwapChains,
    Reset,
    Present,
    GetBackBuffer,
    GetRasterStatus,
    SetDialogBoxMode,
    SetGammaRamp,
    GetGammaRamp,
    CreateTexture,
    CreateVolumeTexture,
    CreateCubeTexture,
    CreateVertexBuffer,
    CreateIndexBuffer,
    CreateRenderTarget,
    CreateDepthStencilSurface,
    UpdateSurface,
    UpdateTexture,
    GetRenderTargetData,
    GetFrontBufferData,
    StretchRect,
    ColorFill,
    CreateOffscreenPlainSurface,
    SetRenderTarget,
    GetRenderTarget,
    SetDepthStencilSurface,
    GetDepthStencilSurface,
    BeginScene,
    EndScene,
    Clear,
    SetTransform,
    GetTransform,
    MultiplyTransform,
    SetViewport,
    GetViewport,
    SetMaterial,
    GetMaterial,
    SetLight,
    GetLight,
    LightEnable,
    GetLightEnable,
    SetClipPlane,
    GetClipPlane,
    SetRenderState,
    GetRenderState,
    CreateStateBlock,
    BeginStateBlock,
    EndStateBlock,
    SetClipStatus,
    GetClipStatus,
    GetTexture,
    SetTexture,
    GetTextureStageState,
    SetTextureStageState,
    GetSamplerState,
    SetSamplerState,
    ValidateDevice,
    SetPaletteEntries,
    GetPaletteEntries,
    SetCurrentTexturePalette,
    GetCurrentTexturePalette,
    SetScissorRect,
    GetScissorRect,
    SetSoftwareVertexProcessing,
    GetSoftwareVertexProcessing,
    SetNPatchMode,
    GetNPatchMode,
    DrawPrimitive,
    DrawIndexedPrimitive,
    DrawPrimitiveUP,
    DrawIndexedPrimitiveUP,
    ProcessVertices,
    CreateVertexDeclaration,
    SetVertexDeclaration,
    GetVertexDeclaration,
    SetFVF,
    GetFVF,
    CreateVertexShader,
    SetVertexShader,
    GetVertexShader,
    SetVertexShaderConstantF,
    GetVertexShaderConstantF,
    SetVertexShaderConstantI,
    GetVertexShaderConstantI,
    SetVertexShaderConstantB,
    GetVertexShaderConstantB,
    SetStreamSource,
    GetStreamSource,
    SetStreamSourceFreq,
    GetStreamSourceFreq,
    SetIndices,
    GetIndices,
    CreatePixelShader,
    SetPixelShader,
    GetPixelShader,
    SetPixelShaderConstantF,
    GetPixelShaderConstantF,
    SetPixelShaderConstantI,
    GetPixelShaderConstantI,
    SetPixelShaderConstantB,
    GetPixelShaderConstantB,
    DrawRectPatch,
    DrawTriPatch,
    DeletePatch,
    CreateQuery
}
