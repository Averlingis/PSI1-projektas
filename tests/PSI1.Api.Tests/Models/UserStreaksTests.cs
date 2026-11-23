using PSI1.Api.Models;

namespace PSI1.Api.Tests.Models;

public class UserStreaksTests
{
    private static readonly DateOnly Today = new(2026, 11, 22);

    private static User CreateUser(params DateOnly[] activityDates)
    {
        var user = new User();

        foreach (var date in activityDates)
        {
            user.RecordActivity(date);
        }

        return user;
    }

    // ---------- RecordActivity ----------

    [Fact]
    public void RecordActivity_FirstActivity_StartsStreakAtOne()
    {
        var user = CreateUser(Today);

        Assert.Equal(1, user.CurrentStreak);
        Assert.Equal(Today, user.LastActivityDate);
    }

    [Fact]
    public void RecordActivity_SameDayTwice_DoesNotIncrease()
    {
        var user = CreateUser(Today, Today);

        Assert.Equal(1, user.CurrentStreak);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    public void RecordActivity_ConsecutiveDays_IncreasesByOneEachDay(int days)
    {
        var user = new User();

        for (var i = 0; i < days; i++)
        {
            user.RecordActivity(Today.AddDays(i));
        }

        Assert.Equal(days, user.CurrentStreak);
        Assert.Equal(Today.AddDays(days - 1), user.LastActivityDate);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(365)]
    public void RecordActivity_AfterGap_ResetsToOne(int gapDays)
    {
        var user = CreateUser(Today.AddDays(-1), Today);

        user.RecordActivity(Today.AddDays(gapDays));

        Assert.Equal(1, user.CurrentStreak);
        Assert.Equal(Today.AddDays(gapDays), user.LastActivityDate);
    }

    [Fact]
    public void RecordActivity_ResetThenConsecutive_CountsFromNewStart()
    {
        var user = CreateUser(Today, Today.AddDays(1), Today.AddDays(2));

        user.RecordActivity(Today.AddDays(5));
        user.RecordActivity(Today.AddDays(6));

        Assert.Equal(2, user.CurrentStreak);
    }

    [Fact]
    public void RecordActivity_AcrossYearBoundary_CountsAsConsecutive()
    {
        var user = CreateUser(new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 1));

        Assert.Equal(2, user.CurrentStreak);
    }


    // ---------- GetCurrentStreak ----------

    [Fact]
    public void GetCurrentStreak_NoActivity_ReturnsZero()
    {
        Assert.Equal(0, new User().GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_ActiveToday_ReturnsStreak()
    {
        var user = CreateUser(Today.AddDays(-2), Today.AddDays(-1), Today);

        Assert.Equal(3, user.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_LastActiveYesterday_StillAlive()
    {
        var user = CreateUser(Today.AddDays(-2), Today.AddDays(-1));

        Assert.Equal(2, user.GetCurrentStreak(Today));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(100)]
    public void GetCurrentStreak_LastActiveTwoOrMoreDaysAgo_ReturnsZero(int daysAgo)
    {
        var user = CreateUser(Today.AddDays(-daysAgo - 1), Today.AddDays(-daysAgo));

        Assert.Equal(0, user.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_BrokenStreak_DoesNotModifyStoredState()
    {
        var user = CreateUser(Today.AddDays(-5), Today.AddDays(-4));

        Assert.Equal(0, user.GetCurrentStreak(Today));

        Assert.Equal(2, user.CurrentStreak);
        Assert.Equal(Today.AddDays(-4), user.LastActivityDate);
    }

    [Fact]
    public void GetCurrentStreak_AfterBreakAndNewActivity_ReturnsOne()
    {
        var user = CreateUser(Today.AddDays(-5), Today.AddDays(-4));

        user.RecordActivity(Today);

        Assert.Equal(1, user.GetCurrentStreak(Today));
    }
}