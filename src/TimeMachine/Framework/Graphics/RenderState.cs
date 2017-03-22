using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkType]
    public sealed class RenderState {

        internal WeakReference<GraphicsDevice> INTERNAL_deviceRef;
        // d is so much shorter than INTERNAL_device...
        internal GraphicsDevice d {
            get {
                return INTERNAL_deviceRef.GetTarget();
            }
        }

        internal RenderState(WeakReference<GraphicsDevice> device) {
            INTERNAL_deviceRef = device;
        }


        internal bool INTERNAL_alphaBlendEnable;
        public bool AlphaBlendEnable {
            get { return d.BlendState != BlendState.Additive; }
            set {
                INTERNAL_alphaBlendEnable = value;
                if (!INTERNAL_alphaTestEnable)
                    d.BlendState = BlendState.Opaque;
                else
                    d.BlendState = value ? BlendState.AlphaBlend : BlendState.Additive;
            }
        }

        public BlendFunction AlphaBlendOperation {
            get { return d.BlendState.AlphaBlendFunction; }
            set { d.BlendState.AlphaBlendFunction = value; }
        }

        public Blend AlphaDestinationBlend {
            get { return d.BlendState.AlphaDestinationBlend; }
            set { d.BlendState.AlphaDestinationBlend = value; }
        }

        // Either per-effect or probably applied for the sprite batch.
        public CompareFunction AlphaFunction {
            get { return CompareFunction.Always; }
            set { }
        }

        public Blend AlphaSourceBlend {
            get { return d.BlendState.AlphaSourceBlend; }
            set { d.BlendState.AlphaSourceBlend = value; }
        }

        internal bool INTERNAL_alphaTestEnable;
        public bool AlphaTestEnable {
            get { return d.BlendState != BlendState.Opaque; }
            set {
                INTERNAL_alphaTestEnable = value;
                if (value && !INTERNAL_alphaBlendEnable)
                    d.BlendState = BlendState.Additive;
                else
                    d.BlendState = value ? BlendState.AlphaBlend : BlendState.Opaque;
            }
        }

        public Color BlendFactor {
            get { return d.BlendState.BlendFactor; }
            set { d.BlendState.BlendFactor = value; }
        }

        public BlendFunction BlendFunction {
            get { return d.BlendState.AlphaBlendFunction; }
            set { d.BlendState.AlphaBlendFunction = value; }
        }

        public ColorWriteChannels ColorWriteChannels {
            get { return d.BlendState.ColorWriteChannels; }
            set { d.BlendState.ColorWriteChannels = value; }
        }

        public ColorWriteChannels ColorWriteChannels1 {
            get { return d.BlendState.ColorWriteChannels1; }
            set { d.BlendState.ColorWriteChannels1 = value; }
        }

        public ColorWriteChannels ColorWriteChannels2 {
            get { return d.BlendState.ColorWriteChannels2; }
            set { d.BlendState.ColorWriteChannels2 = value; }
        }

        public ColorWriteChannels ColorWriteChannels3 {
            get { return d.BlendState.ColorWriteChannels3; }
            set { d.BlendState.ColorWriteChannels3 = value; }
        }

        public StencilOperation CounterClockwiseStencilDepthBufferFail {
            get { return d.DepthStencilState.CounterClockwiseStencilDepthBufferFail; }
            set { d.DepthStencilState.CounterClockwiseStencilDepthBufferFail = value; }
        }

        public StencilOperation CounterClockwiseStencilFail {
            get { return d.DepthStencilState.CounterClockwiseStencilFail; }
            set { d.DepthStencilState.CounterClockwiseStencilFail = value; }
        }

        public CompareFunction CounterClockwiseStencilFunction {
            get { return d.DepthStencilState.CounterClockwiseStencilFunction; }
            set { d.DepthStencilState.CounterClockwiseStencilFunction = value; }
        }

        public StencilOperation CounterClockwiseStencilPass {
            get { return d.DepthStencilState.CounterClockwiseStencilPass; }
            set { d.DepthStencilState.CounterClockwiseStencilPass = value; }
        }

        public CullMode CullMode {
            get { return d.RasterizerState.CullMode; }
            set { d.RasterizerState.CullMode = value; }
        }

        public float DepthBias {
            get { return d.RasterizerState.DepthBias; }
            set { d.RasterizerState.DepthBias = value; }
        }

        public bool DepthBufferEnable {
            get { return d.DepthStencilState.DepthBufferEnable; }
            set { d.DepthStencilState.DepthBufferEnable = value; }
        }

        public CompareFunction DepthBufferFunction {
            get { return d.DepthStencilState.DepthBufferFunction; }
            set { d.DepthStencilState.DepthBufferFunction = value; }
        }

        public bool DepthBufferWriteEnable {
            get { return d.DepthStencilState.DepthBufferWriteEnable; }
            set { d.DepthStencilState.DepthBufferWriteEnable = value; }
        }

        public Blend DestinationBlend {
            get { return d.BlendState.ColorDestinationBlend; }
            set { d.BlendState.ColorDestinationBlend = value; }
        }

        public FillMode FillMode {
            get { return d.RasterizerState.FillMode; }
            set { d.RasterizerState.FillMode = value; }
        }

        // Sadly, XNA 4.0 makes this per-effect.
        public Color FogColor {
            get { return OldColor.TransparentBlack; }
            set {  }
        }

        public float FogDensity {
            get { return 1f; }
            set {  }
        }

        public bool FogEnable {
            get { return false; }
            set {  }
        }

        public float FogEnd {
            get { return 1f; }
            set {  }
        }

        public float FogStart {
            get { return 0f; }
            set {  }
        }

        public FogMode FogTableMode {
            get { return FogMode.None; }
            set {  }
        }

        public FogMode FogVertexMode {
            get { return FogMode.None; }
            set {  }
        }

        public bool MultiSampleAntiAlias {
            get { return d.RasterizerState.MultiSampleAntiAlias; }
            set { d.RasterizerState.MultiSampleAntiAlias = true; }
        }

        public int MultiSampleMask {
            get { return d.MultiSampleMask; }
            set { d.MultiSampleMask = value; }
        }

        // RIP point rendering.
        public float PointSize {
            get { return 0f; }
            set {  }
        }

        public float PointSizeMax {
            get { return 64f; }
            set {  }
        }

        public float PointSizeMin {
            get { return 1f; }
            set {  }
        }

        public bool PointSpriteEnable {
            get { return false; }
            set {  }
        }

        public bool RangeFogEnable {
            get { return false; }
            set {  }
        }

        // Should be handled effect-side.
        public int ReferenceAlpha {
            get { return 0; }
            set {  }
        }

        public int ReferenceStencil {
            get { return 0; }
            set {  }
        }

        public bool ScissorTestEnable {
            get { return d.RasterizerState.ScissorTestEnable; }
            set { d.RasterizerState.ScissorTestEnable = value; }
        }

        // ???
        public bool SeparateAlphaBlendEnabled {
            get { return false; }
            set {  }
        }

        public float SlopeScaleDepthBias {
            get { return d.RasterizerState.SlopeScaleDepthBias; }
            set { d.RasterizerState.SlopeScaleDepthBias = value; }
        }

        public Blend SourceBlend {
            get { return d.BlendState.ColorSourceBlend; }
            set { d.BlendState.ColorSourceBlend = value; }
        }

        public StencilOperation StencilDepthBufferFail {
            get { return d.DepthStencilState.StencilDepthBufferFail; }
            set { d.DepthStencilState.StencilDepthBufferFail = value; }
        }

        public bool StencilEnable {
            get { return d.DepthStencilState.StencilEnable; }
            set { d.DepthStencilState.StencilEnable = value; }
        }

        public StencilOperation StencilFail {
            get { return d.DepthStencilState.StencilFail; }
            set { d.DepthStencilState.StencilFail = value; }
        }

        public CompareFunction StencilFunction {
            get { return d.DepthStencilState.StencilFunction; }
            set { d.DepthStencilState.StencilFunction = value; }
        }

        public int StencilMask {
            get { return d.DepthStencilState.StencilMask; }
            set { d.DepthStencilState.StencilMask = value; }
        }

        public StencilOperation StencilPass {
            get { return d.DepthStencilState.StencilPass; }
            set { d.DepthStencilState.StencilPass = value; }
        }

        public int StencilWriteMask {
            get { return d.DepthStencilState.StencilWriteMask; }
            set { d.DepthStencilState.StencilWriteMask = value; }
        }

        public bool TwoSidedStencilMode {
            get { return d.DepthStencilState.TwoSidedStencilMode; }
            set { d.DepthStencilState.TwoSidedStencilMode = value; }
        }

        // ???
        public TextureWrapCoordinates Wrap0 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap1 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap10 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap11 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap12 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap13 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap14 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap15 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap2 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap3 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap4 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap5 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap6 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap7 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap8 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

        public TextureWrapCoordinates Wrap9 {
            get { return TextureWrapCoordinates.Zero; }
            set {  }
        }

    }
}
