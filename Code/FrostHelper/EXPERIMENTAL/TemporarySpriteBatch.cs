using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.EXPERIMENTAL;

/// <summary>
///   A temporary <see cref="SpriteBatch"/>.
/// </summary>
/// <remarks>
///   When constructed, the current <see cref="SpriteBatch"/> properties and <see cref="RenderTarget2D"/> are preserved.
///   Then, the <see cref="SpriteBatch"/> is ended, the <see cref="RenderTarget2D"/> is swapped if necessary,
///   and finally the <see cref="SpriteBatch"/> is restarted with the new properties.<br/>
///   When disposed, the previous <see cref="SpriteBatch"/> properties and <see cref="RenderTarget2D"/> are restored.
///   <br/>
///   Useful when interrupting a <see cref="SpriteBatch"/> mid-render to, for example, render a specific entity to a
///   temporary <see cref="RenderTarget2D"/> while applying a custom shader, all while preserving the previous
///   configuration.<br/>
///   <br/>
///   Note: Restarting a spritebatch flushes it to the GPU, which is costly.
/// </remarks>
internal ref struct TemporarySpriteBatch : IDisposable
{
    /// <summary>
    ///   The <see cref="SpriteSortMode"/> of this <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    public readonly SpriteSortMode CurrentSortMode;

    /// <summary>
    ///   The <see cref="BlendState"/> of this <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly BlendState CurrentBlendState;

    /// <summary>
    ///   The <see cref="SamplerState"/> of this <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly SamplerState CurrentSamplerState;

    /// <summary>
    ///   The <see cref="DepthStencilState"/> of this <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly DepthStencilState CurrentDepthStencilState;

    /// <summary>
    ///   The <see cref="RasterizerState"/> of this <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly RasterizerState CurrentRasterizerState;

    /// <summary>
    ///   The custom <see cref="Effect"/> of this <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [MaybeNull]
    public readonly Effect CurrentCustomEffect;

    /// <summary>
    ///   The transformation <see cref="Matrix"/> of this <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    public readonly Matrix CurrentTransformMatrix;

    /// <summary>
    ///   The <see cref="RenderTarget2D"/> swapped in for the duration of this <see cref="TemporarySpriteBatch"/>
    ///   or <c>null</c> to draw to the screen if <see cref="HasRenderTarget"/> is <c>true</c>; else always <c>null</c>.
    /// </summary>
    [MaybeNull]
    public readonly RenderTarget2D CurrentRenderTarget;


    /// <summary>
    ///   The <see cref="SpriteSortMode"/> that was used prior to the start of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    public readonly SpriteSortMode PreviousSortMode;

    /// <summary>
    ///   The <see cref="BlendState"/> that was used prior to the start of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly BlendState PreviousBlendState;

    /// <summary>
    ///   The <see cref="SamplerState"/> that was used prior to the start of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly SamplerState PreviousSamplerState;

    /// <summary>
    ///   The <see cref="DepthStencilState"/> that was used prior to the start of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly DepthStencilState PreviousDepthStencilState;

    /// <summary>
    ///   The <see cref="RasterizerState"/> that was used prior to the start of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [NotNull]
    public readonly RasterizerState PreviousRasterizerState;

    /// <summary>
    ///   The custom <see cref="Effect"/> that was used prior to the start of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    [MaybeNull]
    public readonly Effect PreviousCustomEffect;

    /// <summary>
    ///   The transformation <see cref="Matrix"/> that was used prior to the start of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    public readonly Matrix PreviousTransformMatrix;

    /// <summary>
    ///   The <see cref="RenderTargetBinding"/>s that were used prior to the start of this <see cref="TemporarySpriteBatch"/>
    ///   or <c>null</c> to draw to the screen if <see cref="HasRenderTarget"/> is <c>true</c>; else always <c>null</c>.
    /// </summary>
    [MaybeNull]
    public readonly RenderTargetBinding[] PreviousRenderTargets;


    /// <summary>
    ///   Whether a <see cref="RenderTarget2D"/> was swapped in for the duration of this
    ///   <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    public readonly bool HasRenderTarget;

    /// <summary>
    ///   Whether this <see cref="TemporarySpriteBatch"/> is still active.
    /// </summary>
    public bool Active { get; private set; }


    /// <summary>
    ///   Create and immediately begin a new <see cref="TemporarySpriteBatch"/>.
    /// </summary>
    /// <seealso cref="TemporarySpriteBatchBuilder"/>
    internal TemporarySpriteBatch(
        bool hasSortMode, SpriteSortMode? sortMode,
        bool hasBlendState, [MaybeNull] BlendState blendState,
        bool hasSamplerState, [MaybeNull] SamplerState samplerState,
        bool hasDepthStencilState, [MaybeNull] DepthStencilState depthStencilState,
        bool hasRasterizerState, [MaybeNull] RasterizerState rasterizerState,
        bool hasCustomEffect, [MaybeNull] Effect customEffect,
        bool hasTransformMatrix, Matrix? transformMatrix,
        bool hasRenderTarget, [MaybeNull] RenderTarget2D renderTarget)
    {
        GetSpriteBatchFields(
            out PreviousSortMode,
            out PreviousBlendState,
            out PreviousSamplerState,
            out PreviousDepthStencilState,
            out PreviousRasterizerState,
            out PreviousCustomEffect,
            out PreviousTransformMatrix);

        CurrentSortMode = hasSortMode ? sortMode!.Value : PreviousSortMode;
        CurrentBlendState = hasBlendState ? blendState : PreviousBlendState;
        CurrentSamplerState = hasSamplerState ? samplerState : PreviousSamplerState;
        CurrentDepthStencilState = hasDepthStencilState ? depthStencilState : PreviousDepthStencilState;
        CurrentRasterizerState = hasRasterizerState ? rasterizerState : PreviousRasterizerState;
        CurrentCustomEffect = hasCustomEffect ? customEffect : PreviousCustomEffect;
        CurrentTransformMatrix = hasTransformMatrix ? transformMatrix!.Value : PreviousTransformMatrix;

        HasRenderTarget = hasRenderTarget;

        GraphicsDevice graphicsDevice = Engine.Graphics.GraphicsDevice;
        if (hasRenderTarget)
        {
            int renderTargetCount = graphicsDevice.GetRenderTargetsNoAllocEXT(null);
            if (renderTargetCount > 0)
            {
                PreviousRenderTargets = new RenderTargetBinding[renderTargetCount];
                graphicsDevice.GetRenderTargetsNoAllocEXT(PreviousRenderTargets);
            }
            CurrentRenderTarget = renderTarget;
        }

        Active = true;
        Draw.SpriteBatch.End();
        if (hasRenderTarget)
            Engine.Graphics.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);
        Draw.SpriteBatch.Begin(
            CurrentSortMode,
            CurrentBlendState,
            CurrentSamplerState,
            CurrentDepthStencilState,
            CurrentRasterizerState,
            CurrentCustomEffect,
            CurrentTransformMatrix);
    }

    /// <summary>
    ///   End this <see cref="TemporarySpriteBatch"/>, restore the previous render targets if necessary, and restore
    ///   the previous <see cref="SpriteBatch"/> properties.
    /// </summary>
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(!Active, typeof(TemporarySpriteBatch));

        Active = false;
        Draw.SpriteBatch.End();
        if (HasRenderTarget)
            Engine.Graphics.GraphicsDevice.SetRenderTargets(PreviousRenderTargets);
        Draw.SpriteBatch.Begin(
            PreviousSortMode,
            PreviousBlendState,
            PreviousSamplerState,
            PreviousDepthStencilState,
            PreviousRasterizerState,
            PreviousCustomEffect,
            PreviousTransformMatrix);
    }

    private static void GetSpriteBatchFields(
        out SpriteSortMode sortMode,
        [NotNull] out BlendState blendState,
        [NotNull] out SamplerState samplerState,
        [NotNull] out DepthStencilState depthStencilState,
        [NotNull] out RasterizerState rasterizerState,
        [MaybeNull] out Effect customEffect,
        out Matrix transformMatrix)
    {
        // life would be good if we could just access these directly...

        DynamicData dynData = DynamicData.For(Draw.SpriteBatch);
        sortMode = dynData.Get<SpriteSortMode>("sortMode");
        blendState = dynData.Get<BlendState>("blendState");
        samplerState = dynData.Get<SamplerState>("samplerState");
        depthStencilState = dynData.Get<DepthStencilState>("depthStencilState");
        rasterizerState = dynData.Get<RasterizerState>("rasterizerState");
        customEffect = dynData.Get<Effect>("customEffect");
        transformMatrix = dynData.Get<Matrix>("transformMatrix");
    }
}

/// <summary>
///   A <see cref="TemporarySpriteBatch"/> builder.
/// </summary>
/// <remarks>
///   This class lets users configure the creation of a new <see cref="TemporarySpriteBatch"/>, which allows
///   users to interrupt an existing <see cref="SpriteBatch"/>, optionally swap in a <see cref="RenderTarget2D"/>
///   and restart the <see cref="SpriteBatch"/> with custom properties.<br/>
///   When done, the <see cref="SpriteBatch"/> is ended, the previous <see cref="RenderTarget2D"/> is restored and the
///   old <see cref="SpriteBatch"/> is resumed.<br/>
///   <br/>
///   Note: Restarting a spritebatch flushes it to the GPU, which is costly.
/// </remarks>
internal sealed class TemporarySpriteBatchBuilder
{
    /// <summary>
    ///   Whether the <see cref="SpriteBatch"/>'s
    ///   <see cref="Microsoft.Xna.Framework.Graphics.SpriteSortMode"/> should be overridden.
    /// </summary>
    /// <seealso cref="SortMode"/>
    public bool HasSortMode { get; private set; }

    /// <summary>
    ///   Whether the <see cref="SpriteBatch"/>'s
    ///   <see cref="Microsoft.Xna.Framework.Graphics.BlendState"/> should be overridden.
    /// </summary>
    /// <seealso cref="BlendState"/>
    public bool HasBlendState { get; private set; }

    /// <summary>
    ///   Whether the <see cref="SpriteBatch"/>'s
    ///   <see cref="Microsoft.Xna.Framework.Graphics.SamplerState"/> should be overridden.
    /// </summary>
    /// <seealso cref="SamplerState"/>
    public bool HasSamplerState { get; private set; }

    /// <summary>
    ///   Whether the <see cref="SpriteBatch"/>'s
    ///   <see cref="Microsoft.Xna.Framework.Graphics.DepthStencilState"/> should be overridden.
    /// </summary>
    /// <seealso cref="DepthStencilState"/>
    public bool HasDepthStencilState { get; private set; }

    /// <summary>
    ///   Whether the <see cref="SpriteBatch"/>'s
    ///   <see cref="Microsoft.Xna.Framework.Graphics.RasterizerState"/> should be overridden.
    /// </summary>
    /// <seealso cref="RasterizerState"/>
    public bool HasRasterizerState { get; private set; }

    /// <summary>
    ///   Whether the <see cref="SpriteBatch"/>'s custom
    ///   <see cref="Microsoft.Xna.Framework.Graphics.Effect"/> should be overridden.
    /// </summary>
    /// <seealso cref="CustomEffect"/>
    public bool HasCustomEffect { get; private set; }

    /// <summary>
    ///   Whether the <see cref="SpriteBatch"/>'s transformation
    ///   <see cref="Microsoft.Xna.Framework.Matrix"/> should be overridden.
    /// </summary>
    /// <seealso cref="TransformMatrix"/>
    public bool HasTransformMatrix { get; private set; }

    /// <summary>
    ///   Whether to swap the current <see cref="Microsoft.Xna.Framework.Graphics.RenderTarget2D"/>
    ///   in-between <see cref="SpriteBatch"/>es.
    /// </summary>
    /// <seealso cref="RenderTarget"/>
    public bool HasRenderTarget { get; private set; }


    /// <summary>
    ///   The <see cref="Microsoft.Xna.Framework.Graphics.SpriteSortMode"/> that the new
    ///   <see cref="SpriteBatch"/> should use.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasSortMode"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    public SpriteSortMode? SortMode { get; private set; }

    /// <summary>
    ///   The <see cref="Microsoft.Xna.Framework.Graphics.BlendState"/> that the new
    ///   <see cref="SpriteBatch"/> should use.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasBlendState"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    [MaybeNull]
    public BlendState BlendState { get; private set; }

    /// <summary>
    ///   The <see cref="Microsoft.Xna.Framework.Graphics.SamplerState"/> that the new
    ///   <see cref="SpriteBatch"/> should use.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasSamplerState"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    [MaybeNull]
    public SamplerState SamplerState { get; private set; }

    /// <summary>
    ///   The <see cref="Microsoft.Xna.Framework.Graphics.DepthStencilState"/> that the new
    ///   <see cref="SpriteBatch"/> should use.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasDepthStencilState"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    [MaybeNull]
    public DepthStencilState DepthStencilState { get; private set; }

    /// <summary>
    ///   The <see cref="Microsoft.Xna.Framework.Graphics.RasterizerState"/> that the new
    ///   <see cref="SpriteBatch"/> should use.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasRasterizerState"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    [MaybeNull]
    public RasterizerState RasterizerState { get; private set; }

    /// <summary>
    ///   The custom <see cref="Microsoft.Xna.Framework.Graphics.Effect"/> that the new
    ///   <see cref="SpriteBatch"/> should use.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasCustomEffect"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    [MaybeNull]
    public Effect CustomEffect { get; private set; }

    /// <summary>
    ///   The transformation <see cref="Microsoft.Xna.Framework.Matrix"/> that the new
    ///   <see cref="SpriteBatch"/> should use.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasTransformMatrix"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    public Matrix? TransformMatrix { get; private set; }

    /// <summary>
    ///   The <see cref="Microsoft.Xna.Framework.Graphics.RenderTarget2D"/> that should be swapped to
    ///   in-between <see cref="SpriteBatch"/>es.
    /// </summary>
    /// <remarks>
    ///    Contains a value when <see cref="HasRenderTarget"/> is <c>true</c>; <c>null</c> otherwise.
    /// </remarks>
    [MaybeNull]
    public RenderTarget2D RenderTarget { get; private set; }


    // the defaults are the same as the ones in SpriteBatch.Begin

    /// <summary>
    ///   Override the new <see cref="SpriteBatch"/>'s <see cref="Microsoft.Xna.Framework.Graphics.SpriteSortMode"/>.
    /// </summary>
    /// <param name="sortMode">
    ///   The new sort mode.
    /// </param>
    public TemporarySpriteBatchBuilder WithSortMode(SpriteSortMode sortMode)
    {
        HasSortMode = true;
        SortMode = sortMode;
        return this;
    }

    /// <summary>
    ///   Override the new <see cref="SpriteBatch"/>'s <see cref="Microsoft.Xna.Framework.Graphics.BlendState"/>.
    /// </summary>
    /// <param name="blendState">
    ///   The new blend state. If <c>null</c>, defaults to <see cref="Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend"/>.
    /// </param>
    public TemporarySpriteBatchBuilder WithBlendState([AllowNull] BlendState blendState)
    {
        HasBlendState = true;
        BlendState = blendState ?? BlendState.AlphaBlend;
        return this;
    }

    /// <summary>
    ///   Override the new <see cref="SpriteBatch"/>'s <see cref="Microsoft.Xna.Framework.Graphics.SamplerState"/>.
    /// </summary>
    /// <param name="samplerState">
    ///   The new sampler state. If <c>null</c>, defaults to <see cref="Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp"/>.
    /// </param>
    public TemporarySpriteBatchBuilder WithSamplerState([AllowNull] SamplerState samplerState)
    {
        HasSamplerState = true;
        SamplerState = samplerState ?? SamplerState.LinearClamp;
        return this;
    }

    /// <summary>
    ///   Override the new <see cref="SpriteBatch"/>'s <see cref="Microsoft.Xna.Framework.Graphics.DepthStencilState"/>.
    /// </summary>
    /// <param name="depthStencilState">
    ///   The new depth stencil state. If <c>null</c>, defaults to <see cref="Microsoft.Xna.Framework.Graphics.DepthStencilState.None"/>.
    /// </param>
    public TemporarySpriteBatchBuilder WithDepthStencilState([MaybeNull] DepthStencilState depthStencilState)
    {
        HasDepthStencilState = true;
        DepthStencilState = depthStencilState ?? DepthStencilState.None;
        return this;
    }

    /// <summary>
    ///   Override the new <see cref="SpriteBatch"/>'s <see cref="Microsoft.Xna.Framework.Graphics.RasterizerState"/>.
    /// </summary>
    /// <param name="rasterizerState">
    ///   The new rasterizer state. If <c>null</c>, defaults to <see cref="Microsoft.Xna.Framework.Graphics.RasterizerState.CullCounterClockwise"/>.
    /// </param>
    public TemporarySpriteBatchBuilder WithRasterizerState([MaybeNull] RasterizerState rasterizerState)
    {
        HasRasterizerState = true;
        RasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
        return this;
    }

    /// <summary>
    ///   Override the new <see cref="SpriteBatch"/>'s custom <see cref="Microsoft.Xna.Framework.Graphics.Effect"/>.
    /// </summary>
    /// <param name="customEffect">
    ///   The new custom effect or <c>null</c> if none should be used.
    /// </param>
    public TemporarySpriteBatchBuilder WithCustomEffect([AllowNull] Effect customEffect)
    {
        HasCustomEffect = true;
        CustomEffect = customEffect;
        return this;
    }

    /// <summary>
    ///   Override the new <see cref="SpriteBatch"/>'s transformation <see cref="Microsoft.Xna.Framework.Matrix"/>.
    /// </summary>
    /// <param name="transformMatrix">
    ///   The new transformation matrix.
    /// </param>
    public TemporarySpriteBatchBuilder WithTransformMatrix(Matrix transformMatrix)
    {
        HasTransformMatrix = true;
        TransformMatrix = transformMatrix;
        return this;
    }

    /// <summary>
    ///   Override the <see cref="RenderTarget2D"/> in-between <see cref="SpriteBatch"/>es.
    /// </summary>
    /// <param name="renderTarget">
    ///   The new render target or <c>null</c> to refer to the screen.
    /// </param>
    #nullable disable
    public TemporarySpriteBatchBuilder WithRenderTarget([AllowNull] RenderTarget2D renderTarget)
    {
        HasRenderTarget = true;
        RenderTarget = renderTarget;
        return this;
    }
    #nullable restore

    /// <summary>
    ///   Restart the <see cref="SpriteBatch"/> with the configured properties.
    /// </summary>
    /// <returns>
    ///   A <see cref="TemporarySpriteBatch"/> that will restore the previous <see cref="SpriteBatch"/> properties
    ///   when disposed. Remember to put it in a <c>using</c> block.
    /// </returns>
    public TemporarySpriteBatch Use()
        => new(
            HasSortMode, SortMode,
            HasBlendState, BlendState,
            HasSamplerState, SamplerState,
            HasDepthStencilState, DepthStencilState,
            HasRasterizerState, RasterizerState,
            HasCustomEffect, CustomEffect,
            HasTransformMatrix, TransformMatrix,
            HasRenderTarget, RenderTarget
        );
}

internal static class TemporarySpriteBatchBuilderExt {
    public static TemporarySpriteBatchBuilder CreateDefault() => new TemporarySpriteBatchBuilder()
        .WithSortMode(SpriteSortMode.Deferred)
        .WithBlendState(BlendState.AlphaBlend)
        .WithSamplerState(SamplerState.LinearClamp)
        .WithDepthStencilState(DepthStencilState.Default)
        .WithRasterizerState(RasterizerState.CullCounterClockwise)
        .WithCustomEffect(null)
        .WithTransformMatrix(Matrix.Identity);
}
