/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiOption.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Numerics;

namespace SlideGenerator.Image.Application.Models;

/// <summary>
///     Base type for ROI / crop options. Use <see cref="AnchorOption" /> for anchor-point-based
///     cropping or <see cref="InterestOption" /> for interest content-aware cropping.
/// </summary>
public abstract record RoiOption
{
    /// <summary>Gets the top-level ROI strategy discriminator.</summary>
    public abstract RoiMode Mode { get; }
}

/// <summary>
///     Configures an anchor-point-based ROI crop.
/// </summary>
/// <remarks>
///     <para>
///         <b>Coordinate system</b>: <c>(0, 0)</c> is the origin of the chosen <see cref="Type" />;
///         1 unit equals the natural scale of that anchor type (see <see cref="Type" /> docs).
///     </para>
///     <para>
///         <b>Pivot</b>: determines where in the resulting crop rectangle the resolved anchor point lands
///         (0 = left/top edge, 1 = right/bottom edge of the crop region).
///     </para>
/// </remarks>
public sealed record AnchorOption : RoiOption
{
    /// <inheritdoc />
    public override RoiMode Mode => RoiMode.Anchor;

    /// <summary>Gets the anchor type that determines the origin and scale.</summary>
    public AnchorType Type { get; init; } = AnchorType.Image;

    /// <summary>
    ///     Gets the offset from the anchor origin, expressed in anchor-type units.
    ///     Defaults to <c>(0, 0)</c> — the origin itself.
    /// </summary>
    public Vector2 Ratio { get; init; } = Vector2.Zero;

    /// <summary>
    ///     Gets the pivot point within the crop rectangle where the anchor lands.
    ///     Defaults to <c>(0.5, 0.5)</c> — the center of the crop.
    /// </summary>
    public Vector2 Pivot { get; init; } = new(0.5f, 0.5f);
}

/// <summary>
///     Configures an interest content-aware crop.
/// </summary>
/// <remarks>
///     The resolver uses the specified <see cref="Type" /> to identify the most
///     important regions (e.g., faces, saliency) and calculates the optimal crop area.
/// </remarks>
public sealed record InterestOption : RoiOption
{
    /// <inheritdoc />
    public override RoiMode Mode => RoiMode.Interest;

    /// <summary>Gets the interest crop strategy.</summary>
    public InterestType Type { get; init; } = InterestType.Attention;
}