using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {

    [RelinkType]
    public enum VertexElementMethod {
        Default = 0,
        UV = 4,
        LookUp = 5,
        LookUpPresampled = 6
    }

    [Flags]
    [RelinkType]
    public enum CompilerOptions {
        None = 0,
        Debug = 1,
        SkipValidation = 2,
        SkipOptimization = 4,
        PackMatrixRowMajor = 8,
        PackMatrixColumnMajor = 16,
        PartialPrecision = 32,
        ForceVertexShaderSoftwareNoOptimizations = 64,
        ForcePixelShaderSoftwareNoOptimizations = 128,
        NoPreShader = 256,
        AvoidFlowControl = 512,
        PreferFlowControl = 1024,
        NotCloneable = 2048
    }

    [RelinkType]
    public enum ShaderProfile {
        PS_1_1,
        PS_1_2,
        PS_1_3,
        PS_1_4,
        PS_2_0,
        PS_2_A,
        PS_2_B,
        PS_2_SW,
        PS_3_0,
        XPS_3_0,
        VS_1_1,
        VS_2_0,
        VS_2_A,
        VS_2_SW,
        VS_3_0,
        XVS_3_0,
        Unknown
    }

    [RelinkType]
    public enum FogMode {
        None,
        Exponent,
        ExponentSquared,
        Linear
    }

    [Flags]
    [RelinkType]
    public enum TextureWrapCoordinates {
        None = 0,
        Zero = 1,
        One = 2,
        Two = 4,
        Three = 8
    }

    [RelinkType]
    public enum MultiSampleType {
        None,
        NonMaskable,
        TwoSamples,
        ThreeSamples,
        FourSamples,
        FiveSamples,
        SixSamples,
        SevenSamples,
        EightSamples,
        NineSamples,
        TenSamples,
        ElevenSamples,
        TwelveSamples,
        ThirteenSamples,
        FourteenSamples,
        FifteenSamples,
        SixteenSamples
    }

    [Flags]
    [RelinkType]
    public enum CreateOptions {
        None = 0,
        SoftwareVertexProcessing = 32,
        HardwareVertexProcessing = 64,
        MixedVertexProcessing = 128,
        NoWindowChanges = 2048,
        SingleThreaded = 268435456,
        SinglePrecision = 536870912,
    }

    [RelinkType]
    public enum DeviceType {
        Hardware = 1,
        Reference = 2,
        NullReference = 4
    }

    [Flags]
    [RelinkType]
    public enum TextureUsage {
        AutoGenerateMipMap = 0x400,
        Linear = 0x40000000,
        None = 0,
        Tiled = unchecked((int) 0xFFFFFFFF)
    }

    [Flags]
    [RelinkType]
    public enum QueryUsages {
        None = 0,
        SrgbRead = 0x10000,
        Filter = 0x20000,
        SrgbWrite = 0x40000,
        PostPixelShaderBlending = 0x80000,
        VertexTexture = 0x100000,
        WrapAndMip = 0x200000
    }

    [RelinkType]
    public enum ResourceType {
        DepthStencilBuffer = 1,
        Texture3DVolume,
        Texture2D,
        Texture3D,
        TextureCube,
        VertexBuffer,
        IndexBuffer,
        RenderTarget
    }

    [RelinkType]
    public enum FilterOptions {
        None = 1,
        Point = 2,
        Linear = 3,
        Triangle = 4,
        Box = 5,
        MirrorU = 65536,
        MirrorV = 131072,
        MirrorW = 262144,
        Mirror = 458752,
        Dither = 524288,
        DitherDiffusion = 1048576,
        SrgbIn = 2097152,
        SrgbOut = 4194304,
        Srgb = 6291456
    }

}
