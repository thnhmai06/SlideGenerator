/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: BrandLockup.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Controls;
using SlideGenerator.Desktop.Services.Theme;

namespace SlideGenerator.Desktop.Components;

/// <summary>
///     Reusable brand mark + wordmark reveal, extracted from the original single-use Splash animation so
///     About (blueprint §5.7, P6) can replay the same identity moment on every visit. Implements the
///     "logo → animation → full → hold" sequence from the product brief as four real, sequential stages —
///     the original Splash-only version played stages 2/3 as one simultaneous 400ms beat with no explicit
///     stage 1 or 4 (the "hold" was faked entirely by <c>App.axaml.cs</c>'s wall-clock splash-duration floor).
/// </summary>
public sealed partial class BrandLockup : UserControl
{
    private static readonly TimeSpan BeforeHold = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan AfterHold = TimeSpan.FromMilliseconds(350);

    /// <summary>
    ///     The full wall-clock duration one <see cref="PlayAsync" /> call takes for a given <c>MotionBrand</c>
    ///     value — <c>App.axaml.cs</c> uses this to size <c>Splash</c>'s minimum visible duration, so the
    ///     hold-before/animate/hold-after sequence is never cut short by the shell swapping in underneath it.
    ///     Zero when <paramref name="motionBrand" /> is zero (reduced motion), matching <see cref="PlayAsync" />
    ///     skipping both holds in that case.
    /// </summary>
    public static TimeSpan GetTotalDuration(TimeSpan motionBrand)
    {
        return motionBrand == TimeSpan.Zero ? TimeSpan.Zero : BeforeHold + motionBrand + AfterHold;
    }

    /// <summary>Constructs the control and loads its XAML.</summary>
    public BrandLockup()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Plays the four-stage reveal — (1) icon alone, held briefly; (2) mark slides left while the
    ///     wordmark fades/slides in; (3) full lockup settled; (4) held briefly — then returns. Safe to call
    ///     again (e.g. About re-entering the page): resets to stage 1 first, so a rapid re-entry replays
    ///     cleanly instead of skipping frames mid-transition.
    /// </summary>
    public async Task PlayAsync(CancellationToken ct = default)
    {
        MarkImage.Classes.Remove("revealed");
        WordImage.Classes.Remove("revealed");

        var duration = ThemeService.GetMotionResource(Application.Current!, "MotionBrand");
        var reduced = duration == TimeSpan.Zero;

        // Stage 1 — icon alone. Also gives the "not revealed" state at least one render pass to land before
        // the transition below has to animate away from it (Avalonia processes pending layout/render while a
        // Task.Delay yields, same effect as the old Dispatcher.Post(..., DispatcherPriority.Loaded) trick).
        if (!reduced) await Task.Delay(BeforeHold, ct).ConfigureAwait(true);

        // Stage 2 — animate. Stage 3 — full (the transition's own end state, landed on once Duration elapses).
        MarkImage.Classes.Add("revealed");
        WordImage.Classes.Add("revealed");
        if (!reduced) await Task.Delay(duration, ct).ConfigureAwait(true);

        // Stage 4 — hold.
        if (!reduced) await Task.Delay(AfterHold, ct).ConfigureAwait(true);
    }
}
