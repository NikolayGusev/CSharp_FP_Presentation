using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CSharpFP_Demo.Result;

namespace CSharpFP_Demo
{
    // Для простоты можно избавиться от generic параметра TError и считать, что
    // ошибка всегда имеет тип string. Это упростит сигнатуры методов, принимающих и возвращающих
    // Result, но не даст в случае необходимости передавать более подробную информацию об ошибке и
    // делать на основании этой информации выбор, что программа должна делать далее.

    // По аналогии с OptionNone для Result задается два возможных варианта, которые не знаю о generic параметре
    // другой части - Success<T> и Failure<TError>. Оба приводятся к нужному Result<T, TError> автоматически в том
    // месте, где нужен Result<T, TError>.


    public class Result<T, TError>
    {
        public bool HasValue { get; }
        private T _value;
        private TError _error;

        public Result(T value)
        {
            _value = value;
            HasValue = true;
            _error = default(TError);
        }

        public Result(TError error)
        {
            _value = default(T);
            HasValue = false;
            _error = error;
        }

        public static implicit operator Result<T, TError>(Success<T> s) => new Result<T, TError>(s.Value);
        public static implicit operator Result<T, TError>(Failure<TError> f) => new Result<T, TError>(f.Error);

        public TR Match<TR>(Func<T, TR> success, Func<TError, TR> failure) => HasValue ? success(_value) : failure(_error);
    }

    public struct Success<T>
    {
        public Success(T value) { Value = value; }
        public T Value { get; }
    }
    public struct Failure<TError>
    {
        public Failure(TError error) { Error = error; }
        public TError Error { get; }
    }

    public static class Result
    {
        public static Success<T> Success<T>(T value) => new Success<T>(value);
        public static Failure<TError> Failure<TError>(TError error) => new Failure<TError>(error);

        public static Result<T, TError> SuccessOf<T, TError>(T value) => new Result<T, TError>(value);
        public static Result<T, TError> FailureOf<T, TError>(TError error) => new Result<T, TError>(error);


        public static Result<TR, TError> Select<T, TError, TR>(this Result<T, TError> result, Func<T, TR> selector)
            => result.Match<Result<TR, TError>>(success: value => Success(selector(value)),
                                                failure: err => Failure(err));

        public static Result<TR, TError> SelectMany<T, TR, TError>(this Result<T, TError> option, Func<T, Result<TR, TError>> selector)
            => option.Match(success: value => selector(value),
                            failure: err => Failure(err));

        public static Result<TR, TError> SelectMany<T, T2, TR, TError>(this Result<T, TError> source,
                                                       Func<T, Result<T2, TError>> f,
                                                       Func<T, T2, TR> selector)
            => source.SelectMany(t1 => f(t1).Select(t2 => selector(t1, t2)));

    }


    [TestFixture]
    public class Results
    {
        [Test]
        public void BasicCreation()
        {
            Result<int, string> success1 = Result.Success(1);
            // вариант с using static Result
            Result<int, string> success2 = Success(1);


            Result<int, string> failure1 = Result.Failure("Something terrible happened");

            // вариант с using static Result
            Result<int, string> failure2 = Failure("Something terrible happened");
        }

        [Test]
        public void Match_1()
        {
            Result<int, string> result = Success(42);
            var matchValue = result.Match(success: i => i.ToString(),
                                          failure: err => "No value: " + err);

            Assert.That(matchValue, Is.EqualTo("42"));
        }

        [Test]
        public void Match_2()
        {
            Result<int, string> result = Failure("Something terrible happened");

            var matchValue = result.Match(success: i => i.ToString(),
                                          failure: err => "No value: " + err);

            Assert.That(matchValue, Is.EqualTo("No value: Something terrible happened"));
        }
    }


    // См. также 5_Linq.cs
}
