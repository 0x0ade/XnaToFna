using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkType]
    public sealed class GraphicsDeviceCapabilities {

        internal WeakReference<GraphicsDevice> INTERNAL_deviceRef;
        // d is so much shorter than INTERNAL_device...
        internal GraphicsDevice d {
            get {
                return INTERNAL_deviceRef.GetTarget();
            }
        }

        internal GraphicsDeviceCapabilities(WeakReference<GraphicsDevice> device) {
            INTERNAL_deviceRef = device;
        }

        public int AdapterOrdinalInGroup {
            get { throw new NotImplementedException(); }
        }

        public CompareCaps AlphaCompareCapabilities {
            get { throw new NotImplementedException(); }
        }

        public FilterCaps CubeTextureFilterCapabilities {
            get { throw new NotImplementedException(); }
        }

        public CursorCaps CursorCapabilities {
            get { throw new NotImplementedException(); }
        }

        public DeclarationTypeCaps DeclarationTypeCapabilities {
            get { throw new NotImplementedException(); }
        }

        public CompareCaps DepthBufferCompareCapabilities {
            get { throw new NotImplementedException(); }
        }

        public BlendCaps DestinationBlendCapabilities {
            get { throw new NotImplementedException(); }
        }

        public DeviceCaps DeviceCapabilities {
            get { throw new NotImplementedException(); }
        }

        public DeviceType DeviceType {
            get { throw new NotImplementedException(); }
        }

        public DriverCaps DriverCapabilities {
            get { throw new NotImplementedException(); }
        }

        public float ExtentsAdjust {
            get { throw new NotImplementedException(); }
        }

        public float GuardBandBottom {
            get { throw new NotImplementedException(); }
        }

        public float GuardBandLeft {
            get { throw new NotImplementedException(); }
        }

        public float GuardBandRight {
            get { throw new NotImplementedException(); }
        }

        public float GuardBandTop {
            get { throw new NotImplementedException(); }
        }

        public LineCaps LineCapabilities {
            get { throw new NotImplementedException(); }
        }

        public int MasterAdapterOrdinal {
            get { throw new NotImplementedException(); }
        }

        public int MaxAnisotropy {
            get { throw new NotImplementedException(); }
        }

        public int MaxPixelShader30InstructionSlots {
            get { throw new NotImplementedException(); }
        }

        public ShaderProfile MaxPixelShaderProfile {
            get { return ShaderProfile.Unknown; }
        }

        public float MaxPointSize {
            get { throw new NotImplementedException(); }
        }

        public int MaxPrimitiveCount {
            get { throw new NotImplementedException(); }
        }

        public int MaxSimultaneousRenderTargets {
            get { return int.MaxValue; }
        }

        public int MaxSimultaneousTextures {
            get { throw new NotImplementedException(); }
        }

        public int MaxStreams {
            get { throw new NotImplementedException(); }
        }

        public int MaxStreamStride {
            get { throw new NotImplementedException(); }
        }

        public int MaxTextureAspectRatio {
            get { throw new NotImplementedException(); }
        }

        public int MaxTextureHeight {
            get { throw new NotImplementedException(); }
        }

        public int MaxTextureRepeat {
            get { throw new NotImplementedException(); }
        }

        public int MaxTextureWidth {
            get { throw new NotImplementedException(); }
        }

        public int MaxUserClipPlanes {
            get { throw new NotImplementedException(); }
        }

        public int MaxVertexIndex {
            get { throw new NotImplementedException(); }
        }

        public int MaxVertexShader30InstructionSlots {
            get { throw new NotImplementedException(); }
        }

        public int MaxVertexShaderConstants {
            get { throw new NotImplementedException(); }
        }

        public ShaderProfile MaxVertexShaderProfile {
            get { throw new NotImplementedException(); }
        }

        public float MaxVertexW {
            get { throw new NotImplementedException(); }
        }

        public int MaxVolumeExtent {
            get { throw new NotImplementedException(); }
        }

        public int NumberOfAdaptersInGroup {
            get { throw new NotImplementedException(); }
        }

        public int NumberSimultaneousRenderTargets {
            get { throw new NotImplementedException(); }
        }

        public float PixelShader1xMaxValue {
            get { throw new NotImplementedException(); }
        }

        public PixelShaderCaps PixelShaderCapabilities {
            get { throw new NotImplementedException(); }
        }

        public Version PixelShaderVersion {
            get { throw new NotImplementedException(); }
        }

        public PresentInterval PresentInterval {
            get { throw new NotImplementedException(); }
        }

        public PrimitiveCaps PrimitiveCapabilities {
            get { throw new NotImplementedException(); }
        }

        public RasterCaps RasterCapabilities {
            get { throw new NotImplementedException(); }
        }

        public ShadingCaps ShadingCapabilities {
            get { throw new NotImplementedException(); }
        }

        public BlendCaps SourceBlendCapabilities {
            get { throw new NotImplementedException(); }
        }

        public StencilCaps StencilCapabilities {
            get { throw new NotImplementedException(); }
        }

        public AddressCaps TextureAddressCapabilities {
            get { throw new NotImplementedException(); }
        }

        public TextureCaps TextureCapabilities {
            get { throw new NotImplementedException(); }
        }

        public FilterCaps TextureFilterCapabilities {
            get { throw new NotImplementedException(); }
        }

        public VertexFormatCaps VertexFormatCapabilities {
            get { throw new NotImplementedException(); }
        }

        public VertexProcessingCaps VertexProcessingCapabilities {
            get { throw new NotImplementedException(); }
        }

        public VertexShaderCaps VertexShaderCapabilities {
            get { throw new NotImplementedException(); }
        }

        public Version VertexShaderVersion {
            get { throw new NotImplementedException(); }
        }

        public FilterCaps VertexTextureFilterCapabilities {
            get { throw new NotImplementedException(); }
        }

        public AddressCaps VolumeTextureAddressCapabilities {
            get { throw new NotImplementedException(); }
        }

        public FilterCaps VolumeTextureFilterCapabilities {
            get { throw new NotImplementedException(); }
        }

        public struct VertexFormatCaps {
            public short NumberSimultaneousTextureCoordinates { get { throw new NotImplementedException(); } }

            public bool SupportsDoNotStripElements { get { throw new NotImplementedException(); } }

            public bool SupportsPointSize { get { throw new NotImplementedException(); } }
        }

        public struct CursorCaps {
            public bool SupportsColor { get { throw new NotImplementedException(); } }

            public bool SupportsLowResolution { get { throw new NotImplementedException(); } }
        }

        public struct ShadingCaps {
            public bool SupportsAlphaGouraudBlend { get { throw new NotImplementedException(); } }

            public bool SupportsColorGouraudRgb { get { throw new NotImplementedException(); } }

            public bool SupportsFogGouraud { get { throw new NotImplementedException(); } }

            public bool SupportsSpecularGouraudRgb { get { throw new NotImplementedException(); } }
        }

        public struct StencilCaps {
            public bool SupportsDecrement { get { throw new NotImplementedException(); } }

            public bool SupportsDecrementSaturation { get { throw new NotImplementedException(); } }

            public bool SupportsIncrement { get { throw new NotImplementedException(); } }

            public bool SupportsIncrementSaturation { get { throw new NotImplementedException(); } }

            public bool SupportsInvert { get { throw new NotImplementedException(); } }

            public bool SupportsKeep { get { throw new NotImplementedException(); } }

            public bool SupportsReplace { get { throw new NotImplementedException(); } }

            public bool SupportsTwoSided { get { throw new NotImplementedException(); } }

            public bool SupportsZero { get { throw new NotImplementedException(); } }
        }

        public struct VertexProcessingCaps {
            public bool SupportsLocalViewer { get { throw new NotImplementedException(); } }

            public bool SupportsNoTextureGenerationNonLocalViewer { get { throw new NotImplementedException(); } }

            public bool SupportsTextureGeneration { get { throw new NotImplementedException(); } }

            public bool SupportsTextureGenerationSphereMap { get { throw new NotImplementedException(); } }

            public override string ToString() { throw new NotImplementedException(); }
        }

        public struct VertexShaderCaps {
            public const int MaxDynamicFlowControlDepth = 24;

            public const int MaxNumberTemps = 32;

            public const int MaxStaticFlowControlDepth = 4;

            public const int MinDynamicFlowControlDepth = 0;

            public const int MinNumberTemps = 12;

            public const int MinStaticFlowControlDepth = 1;

            public int DynamicFlowControlDepth { get { throw new NotImplementedException(); } }

            public int NumberTemps { get { throw new NotImplementedException(); } }

            public int StaticFlowControlDepth { get { throw new NotImplementedException(); } }

            public bool SupportsPredication { get { throw new NotImplementedException(); } }

            public override string ToString() { throw new NotImplementedException(); }
        }

        public struct AddressCaps {
            public bool SupportsBorder { get { throw new NotImplementedException(); } }

            public bool SupportsClamp { get { throw new NotImplementedException(); } }

            public bool SupportsIndependentUV { get { throw new NotImplementedException(); } }

            public bool SupportsMirror { get { throw new NotImplementedException(); } }

            public bool SupportsMirrorOnce { get { throw new NotImplementedException(); } }

            public bool SupportsWrap { get { throw new NotImplementedException(); } }
        }

        public struct CompareCaps {
            public bool SupportsAlways { get { throw new NotImplementedException(); } }

            public bool SupportsEqual { get { throw new NotImplementedException(); } }

            public bool SupportsGreater { get { throw new NotImplementedException(); } }

            public bool SupportsGreaterEqual { get { throw new NotImplementedException(); } }

            public bool SupportsLess { get { throw new NotImplementedException(); } }

            public bool SupportsLessEqual { get { throw new NotImplementedException(); } }

            public bool SupportsNever { get { throw new NotImplementedException(); } }

            public bool SupportsNotEqual { get { throw new NotImplementedException(); } }
        }

        public struct DeclarationTypeCaps {
            public bool SupportsByte4 { get { throw new NotImplementedException(); } }

            public bool SupportsHalfVector2 { get { throw new NotImplementedException(); } }

            public bool SupportsHalfVector4 { get { throw new NotImplementedException(); } }

            public bool SupportsNormalized101010 { get { throw new NotImplementedException(); } }

            public bool SupportsNormalizedShort2 { get { throw new NotImplementedException(); } }

            public bool SupportsNormalizedShort4 { get { throw new NotImplementedException(); } }

            public bool SupportsRg32 { get { throw new NotImplementedException(); } }

            public bool SupportsRgba32 { get { throw new NotImplementedException(); } }

            public bool SupportsRgba64 { get { throw new NotImplementedException(); } }

            public bool SupportsUInt101010 { get { throw new NotImplementedException(); } }

            public override string ToString() { throw new NotImplementedException(); }
        }

        public struct LineCaps {
            public bool SupportsAlphaCompare { get { throw new NotImplementedException(); } }

            public bool SupportsAntiAlias { get { throw new NotImplementedException(); } }

            public bool SupportsBlend { get { throw new NotImplementedException(); } }

            public bool SupportsDepthBufferTest { get { throw new NotImplementedException(); } }

            public bool SupportsFog { get { throw new NotImplementedException(); } }

            public bool SupportsTextureMapping { get { throw new NotImplementedException(); } }
        }

        public struct TextureCaps {
            public bool RequiresCubeMapPower2 { get { throw new NotImplementedException(); } }

            public bool RequiresPower2 { get { throw new NotImplementedException(); } }

            public bool RequiresSquareOnly { get { throw new NotImplementedException(); } }

            public bool RequiresVolumeMapPower2 { get { throw new NotImplementedException(); } }

            public bool SupportsAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsCubeMap { get { throw new NotImplementedException(); } }

            public bool SupportsMipCubeMap { get { throw new NotImplementedException(); } }

            public bool SupportsMipMap { get { throw new NotImplementedException(); } }

            public bool SupportsMipVolumeMap { get { throw new NotImplementedException(); } }

            public bool SupportsNonPower2Conditional { get { throw new NotImplementedException(); } }

            public bool SupportsNoProjectedBumpEnvironment { get { throw new NotImplementedException(); } }

            public bool SupportsPerspective { get { throw new NotImplementedException(); } }

            public bool SupportsProjected { get { throw new NotImplementedException(); } }

            public bool SupportsTextureRepeatNotScaledBySize { get { throw new NotImplementedException(); } }

            public bool SupportsVolumeMap { get { throw new NotImplementedException(); } }
        }

        public struct BlendCaps {
            public bool SupportsBlendFactor { get { throw new NotImplementedException(); } }

            public bool SupportsBothInverseSourceAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsBothSourceAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsDestinationAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsDestinationColor { get { throw new NotImplementedException(); } }

            public bool SupportsInverseDestinationAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsInverseDestinationColor { get { throw new NotImplementedException(); } }

            public bool SupportsInverseSourceAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsInverseSourceColor { get { throw new NotImplementedException(); } }

            public bool SupportsOne { get { throw new NotImplementedException(); } }

            public bool SupportsSourceAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsSourceAlphaSat { get { throw new NotImplementedException(); } }

            public bool SupportsSourceColor { get { throw new NotImplementedException(); } }

            public bool SupportsZero { get { throw new NotImplementedException(); } }
        }

        public struct DeviceCaps {
            public bool CanDrawSystemToNonLocal { get { throw new NotImplementedException(); } }

            public bool CanRenderAfterFlip { get { throw new NotImplementedException(); } }

            public bool IsDirect3D9Driver { get { throw new NotImplementedException(); } }

            public bool SupportsDrawPrimitives2 { get { throw new NotImplementedException(); } }

            public bool SupportsDrawPrimitives2Ex { get { throw new NotImplementedException(); } }

            public bool SupportsDrawPrimitivesTransformedVertex { get { throw new NotImplementedException(); } }

            public bool SupportsExecuteSystemMemory { get { throw new NotImplementedException(); } }

            public bool SupportsExecuteVideoMemory { get { throw new NotImplementedException(); } }

            public bool SupportsHardwareRasterization { get { throw new NotImplementedException(); } }

            public bool SupportsHardwareTransformAndLight { get { throw new NotImplementedException(); } }

            public bool SupportsSeparateTextureMemories { get { throw new NotImplementedException(); } }

            public bool SupportsStreamOffset { get { throw new NotImplementedException(); } }

            public bool SupportsTextureNonLocalVideoMemory { get { throw new NotImplementedException(); } }

            public bool SupportsTextureSystemMemory { get { throw new NotImplementedException(); } }

            public bool SupportsTextureVideoMemory { get { throw new NotImplementedException(); } }

            public bool SupportsTransformedVertexSystemMemory { get { throw new NotImplementedException(); } }

            public bool SupportsTransformedVertexVideoMemory { get { throw new NotImplementedException(); } }

            public bool VertexElementScanSharesStreamOffset { get { throw new NotImplementedException(); } }
        }

        public struct DriverCaps {
            public bool CanAutoGenerateMipMap { get { throw new NotImplementedException(); } }

            public bool CanCalibrateGamma { get { throw new NotImplementedException(); } }

            public bool CanManageResource { get { throw new NotImplementedException(); } }

            public bool ReadScanLine { get { throw new NotImplementedException(); } }

            public bool SupportsAlphaFullScreenFlipOrDiscard { get { throw new NotImplementedException(); } }

            public bool SupportsCopyToSystemMemory { get { throw new NotImplementedException(); } }

            public bool SupportsCopyToVideoMemory { get { throw new NotImplementedException(); } }

            public bool SupportsDynamicTextures { get { throw new NotImplementedException(); } }

            public bool SupportsFullScreenGamma { get { throw new NotImplementedException(); } }

            public bool SupportsLinearToSrgbPresentation { get { throw new NotImplementedException(); } }
        }

        public struct FilterCaps {
            public bool SupportsMagnifyAnisotropic { get { throw new NotImplementedException(); } }

            public bool SupportsMagnifyGaussianQuad { get { throw new NotImplementedException(); } }

            public bool SupportsMagnifyLinear { get { throw new NotImplementedException(); } }

            public bool SupportsMagnifyPoint { get { throw new NotImplementedException(); } }

            public bool SupportsMagnifyPyramidalQuad { get { throw new NotImplementedException(); } }

            public bool SupportsMinifyAnisotropic { get { throw new NotImplementedException(); } }

            public bool SupportsMinifyGaussianQuad { get { throw new NotImplementedException(); } }

            public bool SupportsMinifyLinear { get { throw new NotImplementedException(); } }

            public bool SupportsMinifyPoint { get { throw new NotImplementedException(); } }

            public bool SupportsMinifyPyramidalQuad { get { throw new NotImplementedException(); } }

            public bool SupportsMipMapLinear { get { throw new NotImplementedException(); } }

            public bool SupportsMipMapPoint { get { throw new NotImplementedException(); } }
        }

        public struct PixelShaderCaps {
            public const int MaxDynamicFlowControlDepth = 24;
            public const int MaxNumberInstructionSlots = 512;
            public const int MaxNumberTemps = 32;
            public const int MaxStaticFlowControlDepth = 4;
            public const int MinDynamicFlowControlDepth = 0;
            public const int MinNumberInstructionSlots = 96;
            public const int MinNumberTemps = 12;
            public const int MinStaticFlowControlDepth = 0;

            public int DynamicFlowControlDepth { get { throw new NotImplementedException(); } }

            public int NumberInstructionSlots { get { throw new NotImplementedException(); } }

            public int NumberTemps { get { throw new NotImplementedException(); } }

            public int StaticFlowControlDepth { get { throw new NotImplementedException(); } }

            public bool SupportsArbitrarySwizzle { get { throw new NotImplementedException(); } }

            public bool SupportsGradientInstructions { get { throw new NotImplementedException(); } }

            public bool SupportsNoDependentReadLimit { get { throw new NotImplementedException(); } }

            public bool SupportsNoTextureInstructionLimit { get { throw new NotImplementedException(); } }

            public bool SupportsPredication { get { throw new NotImplementedException(); } }
        }

        public struct PrimitiveCaps {
            public bool HasFogVertexClamped { get { throw new NotImplementedException(); } }

            public bool IsNullReference { get { throw new NotImplementedException(); } }

            public bool SupportsBlendOperation { get { throw new NotImplementedException(); } }

            public bool SupportsClipPlaneScaledPoints { get { throw new NotImplementedException(); } }

            public bool SupportsClipTransformedVertices { get { throw new NotImplementedException(); } }

            public bool SupportsColorWrite { get { throw new NotImplementedException(); } }

            public bool SupportsCullClockwiseFace { get { throw new NotImplementedException(); } }

            public bool SupportsCullCounterClockwiseFace { get { throw new NotImplementedException(); } }

            public bool SupportsCullNone { get { throw new NotImplementedException(); } }

            public bool SupportsFogAndSpecularAlpha { get { throw new NotImplementedException(); } }

            public bool SupportsIndependentWriteMasks { get { throw new NotImplementedException(); } }

            public bool SupportsMaskZ { get { throw new NotImplementedException(); } }

            public bool SupportsMultipleRenderTargetsIndependentBitDepths { get { throw new NotImplementedException(); } }

            public bool SupportsMultipleRenderTargetsPostPixelShaderBlending { get { throw new NotImplementedException(); } }

            public bool SupportsPerStageConstant { get { throw new NotImplementedException(); } }

            public bool SupportsSeparateAlphaBlend { get { throw new NotImplementedException(); } }

            public bool SupportsTextureStageStateArgumentTemp { get { throw new NotImplementedException(); } }
        }

        public struct RasterCaps {
            public bool SupportsAnisotropy { get { throw new NotImplementedException(); } }

            public bool SupportsColorPerspective { get { throw new NotImplementedException(); } }

            public bool SupportsDepthBias { get { throw new NotImplementedException(); } }

            public bool SupportsDepthBufferLessHsr { get { throw new NotImplementedException(); } }

            public bool SupportsDepthBufferTest { get { throw new NotImplementedException(); } }

            public bool SupportsDepthFog { get { throw new NotImplementedException(); } }

            public bool SupportsFogRange { get { throw new NotImplementedException(); } }

            public bool SupportsFogTable { get { throw new NotImplementedException(); } }

            public bool SupportsFogVertex { get { throw new NotImplementedException(); } }

            public bool SupportsMipMapLevelOfDetailBias { get { throw new NotImplementedException(); } }

            public bool SupportsMultisampleToggle { get { throw new NotImplementedException(); } }

            public bool SupportsScissorTest { get { throw new NotImplementedException(); } }

            public bool SupportsSlopeScaleDepthBias { get { throw new NotImplementedException(); } }

            public bool SupportsWFog { get { throw new NotImplementedException(); } }
        }

    }
}
