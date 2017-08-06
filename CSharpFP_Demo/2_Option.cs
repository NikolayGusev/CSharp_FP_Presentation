using NUnit.Framework;
using System;
using static CSharpFP_Demo.Option;

namespace CSharpFP_Demo
{
    // OptionNone вместе с implicit operator Option<T>(OptionNone _) используется для простоты
    // создания значиний None. При попытке использовать OptionNone вместо Option<T>, OptionNone
    // автоматически сконвертируется в требуемый Option<T>.

    public struct Option<T>
    {
        public bool HasValue { get; }
        private T _value;

        public Option(T value)
        {
            _value = value;
            HasValue = true;
        }

        public static implicit operator Option<T>(OptionNone _) => new Option<T>();

        public TR Match<TR>(Func<T, TR> some, Func<TR> none) => HasValue ? some(_value) : none();
        public T GetValueOrDefault(T defValue) => HasValue ? _value : defValue;
        public T GetValueOrDefault(Func<T> defValue) => HasValue ? _value : defValue();
    }

    public struct OptionNone { }

    public static class Option
    {
        private static readonly OptionNone _noneInstance = new OptionNone();

        public static OptionNone None => _noneInstance;
        public static Option<T> NoneOf<T>() => new Option<T>();
        public static Option<T> Some<T>(T value) => new Option<T>(value);
        public static Option<T> OptionFromNullable<T>(this T value) => value != null ? Some(value) : None;

        public static Option<TR> Select<T, TR>(this Option<T> option, Func<T, TR> selector)
            => option.Match(some: value => Some(selector(value)),
                            none: () => None);

        public static Option<TR> SelectMany<T, TR>(this Option<T> option, Func<T, Option<TR>> selector)
            => option.Match(some: value => selector(value),
                            none: () => None);

        public static Option<TR> SelectMany<T, T2, TR>(this Option<T> source,
                                                       Func<T, Option<T2>> f,
                                                       Func<T, T2, TR> selector)
            => source.SelectMany(t1 => f(t1).Select(t2 => selector(t1, t2)));

    }


    [TestFixture]
    public class Options
    {
        [Test]
        public void BasicCreation()
        {
            Option<int> some1 = Option.Some(1);
            // вариант с using static Option
            Option<string> some2 = Some("1");


            Option<string> o3 = Option.None;

            // вариант с using static Option
            Option<string> o4 = None;


            // From nullable value
            string str = null;
            Option<string> o5 = str.OptionFromNullable();
        }

        [Test]
        public void Match_1()
        {
            var o = Some(42);
            var res1 = o.Match(some: i => i.ToString(),
                               none: () => "No value");

            Assert.That(res1, Is.EqualTo("42"));
        }

        [Test]
        public void Match_2()
        {
            Option<int> o = None;
            var res1 = o.Match(some: i => i.ToString(),
                               none: () => "No value");

            Assert.That(res1, Is.EqualTo("No value"));
        }

        // См. также 5_Linq.cs
    }
}
