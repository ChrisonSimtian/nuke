using System;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers the duration column of the end-of-run summary table (<see cref="Host.FormatDuration"/>).
/// See #550. The old formatter replaced the substring <c>0:00</c> with the <c>&lt; 1sec</c>
/// sentinel, so it also matched inside <c>10:00</c> and rendered <c>1&lt; 1sec</c>.
/// </summary>
public class TargetDurationFormattingSpecs
{
    [Theory]
    [InlineData(0, "< 1sec")]
    [InlineData(0.5, "< 1sec")]
    public void Durations_under_one_second_render_the_sentinel(double seconds, string expected)
    {
        Host.FormatDuration(TimeSpan.FromSeconds(seconds)).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "0:01")]
    [InlineData(30, "0:30")]
    [InlineData(59, "0:59")]
    [InlineData(60, "1:00")]
    [InlineData(599, "9:59")]
    public void Durations_under_ten_minutes_render_minutes_and_seconds(int seconds, string expected)
    {
        Host.FormatDuration(TimeSpan.FromSeconds(seconds)).Should().Be(expected);
    }

    [Theory]
    [InlineData(10, "10:00")]
    [InlineData(20, "20:00")]
    [InlineData(30, "30:00")]
    [InlineData(90, "90:00")]
    public void Whole_ten_minute_durations_are_not_mistaken_for_the_sentinel(int minutes, string expected)
    {
        Host.FormatDuration(TimeSpan.FromMinutes(minutes)).Should().Be(expected);
    }

    [Fact]
    public void Durations_past_an_hour_keep_counting_in_minutes()
    {
        Host.FormatDuration(TimeSpan.FromMinutes(125) + TimeSpan.FromSeconds(5)).Should().Be("125:05");
    }

    [Fact]
    public void The_sentinel_only_applies_below_one_second()
    {
        Host.FormatDuration(TimeSpan.FromSeconds(1)).Should().NotContain("< 1sec");
    }
}
