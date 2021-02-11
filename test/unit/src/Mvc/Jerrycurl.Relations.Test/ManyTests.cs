using Shouldly;

namespace Jerrycurl.Relations.Test
{
    public class ManyTests
    {
        public void Test_Many_Equality()
        {
            One<int> empty = new One<int>();
            One<int> zero = new One<int>(0);
            One<int> one = new One<int>(1);

            One<int> empty2 = new One<int>();
            One<int> zero2 = new One<int>(0);
            One<int> one2 = new One<int>(1);

            empty.Equals(empty2).ShouldBeTrue();
            empty.Equals(zero).ShouldBeFalse();
            empty.Equals(0).ShouldBeFalse();
            empty.Equals(one).ShouldBeFalse();
            empty.Equals(1).ShouldBeFalse();

            zero.Equals(empty).ShouldBeFalse();
            zero.Equals(zero2).ShouldBeTrue();
            zero.Equals(0).ShouldBeTrue();
            zero.Equals(one).ShouldBeFalse();
            zero.Equals(1).ShouldBeFalse();

            one.Equals(empty).ShouldBeFalse();
            one.Equals(zero).ShouldBeFalse();
            one.Equals(0).ShouldBeFalse();
            one.Equals(one2).ShouldBeTrue();
            one.Equals(1).ShouldBeTrue();
        }

    }
}
