using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Fallout.Common.Execution;
using Xunit;

namespace Fallout.Common.Specs.Execution;

public class ExecutableTargetFactorySpecs
{
    [Fact]
    public void Test()
    {
        var build = new TestBuild();
        var targets = ExecutableTargetFactory.CreateAll(build, x => x.A);

        var a = targets.Single(x => x.Name == nameof(TestBuild.A));
        var b = targets.Single(x => x.Name == nameof(TestBuild.B));
        var c = targets.Single(x => x.Name == nameof(TestBuild.C));
        var d = targets.Single(x => x.Name == nameof(TestBuild.D));

        targets.Single(x => x.IsDefault).Should().Be(a);

        a.Factory.Should().Be(build.A);
        a.Description.Should().Be(build.Description);
        a.DelegateRequirements.Should().Equal(build.Requirement);
        a.Actions.Should().Equal(build.Action);
        a.AllDependencies.Should().BeEmpty();

        b.DependencyBehavior.Should().Be(DependencyBehavior.Execute);
        b.StaticConditions.Should().ContainSingle(x => x.Delegate.Equals(build.StaticCondition));
        b.ExecutionDependencies.Should().Equal(d);
        b.TriggerDependencies.Should().Equal(c);
        b.AllDependencies.Should().NotBeEmpty();

        c.Triggers.Should().Equal(b);
        c.TriggerDependencies.Should().Equal(d);
        c.ExecutionDependencies.Should().Equal(b);
        c.OrderDependencies.Should().Equal(d);
        c.AllDependencies.Should().NotBeEmpty();

        d.DependencyBehavior.Should().Be(DependencyBehavior.Skip);
        d.DynamicConditions.Should().ContainSingle(x => x.Delegate.Equals(build.DynamicCondition));
        d.OrderDependencies.Should().Equal(b);
        d.Triggers.Should().Equal(c);
        d.AllDependencies.Should().NotBeEmpty();
    }

    private class TestBuild : FalloutBuild
    {
        public string Description = "description";
        public Action Action = () => { };
        public Expression<Func<bool>> Requirement = () => true;
        public Func<bool> StaticCondition = () => true;
        public Func<bool> DynamicCondition = () => false;

        public Target A => _ => _
            .Description(Description)
            .Requires(Requirement)
            .Executes(Action);

        public Target B => _ => _
            .WhenSkipped(DependencyBehavior.Execute)
            .OnlyWhenStatic(StaticCondition)
            .DependsOn(D)
            .DependentFor(C);

        public Target C => _ => _
            .Triggers(B)
            .TriggeredBy(D);

        public Target D => _ => _
            .WhenSkipped(DependencyBehavior.Skip)
            .OnlyWhenDynamic(DynamicCondition)
            .After(B)
            .Before(C);
    }

    [Fact]
    public void TestInheritance()
    {
        var build = new TestFinalBuild();
        var targets = ExecutableTargetFactory.CreateAll(build);

        var shared = targets.Should().ContainSingle(x => x.Name == nameof(TestFinalBuild.SharedTarget)).Subject;
        var override_ = targets.Should().ContainSingle(x => x.Name == nameof(TestFinalBuild.SpecificTarget)).Subject;
        var explicitBase = targets.Should().ContainSingle(x => x.Name == nameof(ITestSharedBuild.ExplicitSharedTarget)).Subject;
        var explicit_ = targets.Should().ContainSingle(x => x.Name == nameof(IAnotherSharedBuild.ExplicitTarget)).Subject;

        shared.Actions.Should().HaveCount(1);
        shared.ExecutionDependencies.Single().Name.Should().Be(nameof(TestFinalBuild.SpecificTarget));
        shared.Description.Should().Be(nameof(TestFinalBuild.SharedTarget));

        override_.Actions.Should().HaveCount(1);
        override_.OrderDependencies.Single().Name.Should().Be(nameof(TestFinalBuild.SharedTarget));
        override_.Description.Should().Be(nameof(TestFinalBuild.SpecificTarget));

        targets.Should().HaveCount(8);
        targets.Single(x => x.Name == nameof(ITestSharedBuild.AbstractSharedTarget)).Description.Should().Be("RIGHT");
        targets.Single(x => x.Name == nameof(ITestSharedBuild.ExplicitSharedTarget)).Description.Should().Be("RIGHT");
        targets.Single(x => x.Name == nameof(IAnotherSharedBuild.ExplicitTargetWithDefault)).Description.Should().Be("RIGHT");
        targets.Single(x => x.Name == nameof(IAnotherSharedBuild.ExplicitTargetWithoutDefault)).Description.Should().Be("RIGHT");
        targets.Single(x => x.Name == nameof(IAnotherSharedBuild.TargetWithDefault)).Description.Should().Be("RIGHT");
    }

    private interface ITestSharedBuild
        : IFalloutBuild
    {
        Target SharedTarget => _ => _
            .Executes(() => { });

        Target AbstractSharedTarget { get; }
        Target ExplicitSharedTarget => _ => _.Description("WRONG");
    }

    private class TestBaseBuild : FalloutBuild
    {
        public virtual Target SpecificTarget => _ => _
            .Executes(() => { });
    }

    private abstract class TestIntermediateBuild : TestBaseBuild, ITestSharedBuild
    {
        public override Target SpecificTarget => _ => _
            .Base()
            .After(SharedTarget);

        public virtual Target SharedTarget => _ => _
            .Inherit<ITestSharedBuild>(x => x.SharedTarget)
            .DependsOn(SpecificTarget);

        public abstract Target AbstractSharedTarget { get; }
        Target ITestSharedBuild.ExplicitSharedTarget => _ => _.Description("RIGHT");
    }

    private interface IAnotherSharedBuild
        : IFalloutBuild
    {
        Target ExplicitTargetWithDefault => _ => _.Description("WRONG");
        Target ExplicitTargetWithoutDefault { get; }
        Target TargetWithDefault => _ => _.Description("RIGHT");
        Target ExplicitTarget => _ => _;
    }

    private class TestFinalBuild : TestIntermediateBuild, IAnotherSharedBuild
    {
        public override Target SpecificTarget => _ => _
            .Base()
            .Description(nameof(SpecificTarget));

        public override Target SharedTarget => _ => _
            .Base()
            .Description(nameof(SharedTarget));

        public override Target AbstractSharedTarget => _ => _.Description("RIGHT");

        Target IAnotherSharedBuild.ExplicitTargetWithDefault => _ => _.Description("RIGHT");
        Target IAnotherSharedBuild.ExplicitTargetWithoutDefault => _ => _.Description("RIGHT");
        Target IAnotherSharedBuild.ExplicitTarget => _ => _;
    }
}
