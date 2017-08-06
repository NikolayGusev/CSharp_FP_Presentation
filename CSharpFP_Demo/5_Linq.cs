using FsCheck;
using NUnit.Framework;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;
using static CSharpFP_Demo.Option;
using static CSharpFP_Demo.Result;

namespace CSharpFP_Demo
{
    [TestFixture]
    public class Linq
    {
        // Select помогает изменять значение внутри Option, не извлекая его перед
        // этим:
        [Test]
        public void Option_Select_1()
        {
            Option<int> o1 = Some(42);
            Option<string> o2 = o1.Select(x => $"The answer is {x}");

            Assert.That(o2.HasValue);
            Assert.That(o2.GetValueOrDefault(""), Is.EqualTo("The answer is 42")); 
        }

        // Тот же пример, но для значения None
        [Test]
        public void Option_Select_2()
        {
            Option<int> o1 = None;
            Option<string> o2 = o1.Select(x => $"The answer is {x}");

            Assert.That(o2.HasValue, Is.False);
        }

        // Вместо лямбда-выражения можно применять любые функции! Напрмер:
        public int DoubleAndAddOne(int x) => (x * 2) + 1;

        [Test]
        public void Option_Select_3()
        {
            var o1 = Some(100);
            var o2 = o1.Select(DoubleAndAddOne);

            Assert.That(o2.HasValue);
            Assert.That(o2.GetValueOrDefault(0), Is.EqualTo(201));
        }


        // Select применим и к Result:
        [Test]
        public void Result_Select_1()
        {
            var res1 = SuccessOf<int, string>(100);
            var res2 = res1.Select(DoubleAndAddOne);

            Assert.That(res2.HasValue);
            Assert.That(GetValueUnsafe(res2), Is.EqualTo(201));
        }

        private static T GetValueUnsafe<T, TError>(Result<T, TError> r)
            => r.Match(success: value => value,
                       failure: err => { throw new Exception(); });

        private static TError GetErrorUnsafe<T, TError>(Result<T, TError> r)
            => r.Match(success: value => { throw new Exception(); },
                       failure: err => err);

        [Test]
        public void Result_Select_2()
        {
            var res1 = FailureOf<int, string>("Some error occured");
            var res2 = res1.Select(DoubleAndAddOne);

            Assert.That(res2.HasValue, Is.False);
            Assert.That(GetErrorUnsafe(res2), Is.EqualTo("Some error occured"));
        }


        //
        // SelectMany
        //

        // SelectMany позволяет нам использовать Linq для одновременной работы 
        // с несколькими Option. Напрмер, если нужно сложить три целочисленных значения,
        // завернутых в Option и получить результирующее завернутое в него же, это можно
        // сделать так:
        [Test]
        public void Option_SelectMany_1()
        {
            var option1 = Some(1);
            var option2 = Some(2);
            var option3 = Some(3);

            Option<int> o =
                from a in option1
                from b in option2
                from c in option3
                select a + b + c;

            Assert.That(o.HasValue);
            Assert.That(o.GetValueOrDefault(0), Is.EqualTo(6));
        }

        // Функционально, так же как и в случае IEnumerable<T>, Linq вариант -
        // это удобный и хорошо читаемый вариант, аналогичный вызову SelectMany
        // по цепочке. Предыдущий пример эквивалентен этому:
        [Test]
        public void Option_SelectMany_1_1()
        {
            var option1 = Some(1);
            var option2 = Some(2);
            var option3 = Some(3);

            Option<int> o =
                option1.SelectMany(a => option2.SelectMany(b => option3.Select(c => a + b + c)));

            Assert.That(o.HasValue);
            Assert.That(o.GetValueOrDefault(0), Is.EqualTo(6));
        }

        // Если же одно из значений не было представлено, и его Option был равен None,
        // то результатом операции будет тоже None. Это поведение похоже на поведение
        // стандартного C# оператора '?.' для null значений.
        [Test]
        public void Option_SelectMany_2()
        {
            var option1 = Some(1);
            Option<int> option2 = None;
            var option3 = Some(3);

            Option<int> o =
                from a in option1
                from b in option2
                from c in option3
                select a + b + c;

            Assert.That(o.HasValue, Is.False);
        }

        // Вместо значений типа Option можно использовать функции, возвращающие
        // Option. В этом случае, все функции, после вызова первой функции, вернувшей
        // None вызваны не будут (можно убедиться в дебагере или написать тест). 
        // Например:

        private Option<int> FunctionThatReturnsSome(int i) => Some(i);
        private Option<int> FunctionThatReturnsNone() => None;
        [Test]
        public void Option_SelectMany_3()
        {
            Option<int> o =
                from a in FunctionThatReturnsSome(1)
                from b in FunctionThatReturnsSome(2)
                from c in FunctionThatReturnsNone()
                from d in FunctionThatReturnsSome(4) // Эта функция не будет вызвана
                from e in FunctionThatReturnsNone()  // Эта функция не будет вызвана
                from f in FunctionThatReturnsSome(6) // Эта функция не будет вызвана
                select a + b + c + d + e + f;

            Assert.That(o.HasValue, Is.False);
        }

        // * Если есть потребность запустить все функции, то можно реализовать для Option
        //   функцию join, чтобы можно было использовать его как Applicative Functor
        //   (см. для примера http://bugsquash.blogspot.bg/2011/08/validating-with-applicative-functors.html),
        //   в этом случае в каждая строчка после первой выглядела бы как "from x in optionValue join on 1 equals 1".
        //   В нашем же случае мы используем Option как Monad'у.

        // Выше описанные примеры используют значения из каждого Option только при вычислении
        // финального результата. На самом деле, ничто не мешает использовать любое значение для
        // вычисления следующего:
        [Test]
        public void Option_SelectMany_4()
        { 
            Option<string> o =
               from a in Some("Hello")
               from b in Some(a + " World")
               from c in Some(b + "!")
               select c;

            Assert.That(o.HasValue);
            Assert.That(o.GetValueOrDefault(""), Is.EqualTo("Hello World!"));
        }



        //
        // Аналогично можно использовать SelectMany и для типа Result.
        //
        public Result<int, string> FunctionThatReturnsSuccess(int i) => Success(i);
        public Result<int, string> FunctionThatReturnsFailure(string error) => Failure(error);

        [Test]
        public void Result_SelectMany_1()
        {
            Result<int, string> res =
               from a in FunctionThatReturnsSuccess(1)
               from b in FunctionThatReturnsSuccess(2)
               from c in FunctionThatReturnsSuccess(3)
               select a + b + c;

            Assert.That(res.HasValue);
            Assert.That(GetValueUnsafe(res), Is.EqualTo(6));
        }

        // В случае, если на пути выполнения цепочки попадется Failure,
        // то выполнение цепочки прервется и в результат запишется первая
        // встреченная ошибка:
        [Test]
        public void Result_SelectMany_2()
        {
            Result<int, string> res =
               from a in FunctionThatReturnsSuccess(1)
               from b in FunctionThatReturnsFailure("Error description")
               from c in FunctionThatReturnsSuccess(3)
               select a + b + c;

            Assert.That(res.HasValue, Is.False);
            Assert.That(GetErrorUnsafe(res), Is.EqualTo("Error description"));
        }


        //
        // Рассмотрим более сложный пример использования Result (без имплементации методов):
        //
        public class Order { }

        public Result<Order, string> ReadOrderFromDb(long orderId) { return Success(new Order()); }
        public Result<Order, string> ConfirmOrder(Order order) { return Success(order); }
        public Result<Unit, string> ValidateOrder(Order confirmedOrder) { return Success(Unit); }
        public Result<Unit, string> UpdateOrder(Order confirmedOrder) { return Success(Unit); }

        // Т.к. ряд функций не возвращает значения, а тип Result требует вернуть значение,
        // применяем специальный тип Unit, который может принимать только одно значение
        // (тогда как boolean, например, принимает два). Это стандартная практика в функциональном
        // программировании. Обычно в функциональных языках используют Unit вместо ключевого слова void,
        // это по сути аналоги, однако Unit можно передавать, как значение.
        // Создаем FSharp Unit. Вместо него можно также использовать Unit из библиотеки Rx
        // или написать свой.
        private static Unit Unit => (Unit)Activator.CreateInstance(typeof(Unit), true);
        
        // Скомбинируем вышепреведенные функции в цепочку вызовов. Если на каком-то из этапов
        // цепочки произойдет ошибка, мы ее получим в переменной res, а выполнение цепочки прервется.
        [Test]
        public void Result_SelectMany_3()
        {
            long orderId = 1;
            Result<Unit, string> res = from order in ReadOrderFromDb(orderId)
                                       from confirmedOrder in ConfirmOrder(order)
                                       from _ in ValidateOrder(confirmedOrder)
                                       from __ in UpdateOrder(confirmedOrder)
                                       select Unit;
        }

        //
        // Другой пример использования Linq - библиотека для написания парсеров Sprache
        //

        public Parser<string> GetIdParser()
            => from leading in Parse.WhiteSpace.Many()
               from first in Parse.Letter.Once()
               from rest in Parse.LetterOrDigit.Many()
               from trailing in Parse.WhiteSpace.Many()
               select new string(first.Concat(rest).ToArray());
        [Test]
        public void Sprache_Test_1()
        {
            Parser<string> idParser = GetIdParser();
        
            var id = idParser.Parse(" abc123  ");
        
            Assert.That(id, Is.EqualTo("abc123"));
        }
        
        // Парсеры можно комбинировать:
        [Test]
        public void Sprache_Test_2()
        {
            Parser<string> idParser = GetIdParser();

            Parser<string> idWithDelimiter =
                from id in idParser
                from delimiter in Parse.Char(';')
                select id;

            var idsParser =
                from leadingIds in idWithDelimiter.Many()
                from lastId in idParser
                select leadingIds.Concat(new List<string> { lastId });

            var ids = idsParser.Parse(" abc123 ;hello   ;  world ").ToList();
            Assert.That(ids.Count, Is.EqualTo(3));
            Assert.That(ids[0], Is.EqualTo("abc123"));
            Assert.That(ids[1], Is.EqualTo("hello"));
            Assert.That(ids[2], Is.EqualTo("world"));
        }

        //
        // Другой интересный пример использования Linq - генерация случайных значений
        // в библиотеке FsCheck. Сама библиотека используется для Property Based Testing'а,
        // что подразумевает необходимость генерации случайных величин.
        //

        public class Person
        {
            public Person(int age, string name) { Age = age; Name = name; }
            public int Age { get; }
            public string Name { get; }
        }

        public class Worker
        {
            public Worker(Person person, decimal salary) { Person = person; Salary = salary; }
            public Person Person { get; }
            public decimal Salary { get; }
        }


        [Test]
        public void FsCheck_Test_1()
        {
            Gen<Person> personGenerator =
                from age in Arb.Default.Int32().Generator
                from name in Arb.Default.String().Generator
                where age >= 0 && age <= 100
                select new Person(age, name);

            // Получившийся генератор можно использовать для
            // построения других генераторов:
            Gen<Worker> workerGenerator =
                from person in personGenerator
                from salary in Arb.Default.Decimal().Generator
                where salary > 0
                select new Worker(person, salary);


            Worker worker = workerGenerator.Eval(100, FsCheck.Random.mkStdGen(DateTime.Now.Ticks));
        }
    }
}
