using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Dadeni.Core.Tests.Headless
{
    public static class HeadlessTestRunner
    {
        private sealed class Failure
        {
            public string Name;
            public string Message;
        }

        public static int Main(string[] _args)
        {
            string _filter = _args.FirstOrDefault(_argument => !_argument.StartsWith("-"));

            List<Type> _fixtures = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(_type => _type.IsClass && !_type.IsAbstract && _type.GetCustomAttribute<TestFixtureAttribute>() != null)
                .OrderBy(_type => _type.FullName)
                .ToList();

            List<Failure> _failures = new List<Failure>();
            Stopwatch _stopwatch = Stopwatch.StartNew();

            int _passed = 0;
            int _skipped = 0;

            foreach (Type _fixture in _fixtures)
            {
                List<MethodInfo> _tests = _fixture
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(_method => _method.GetCustomAttributes<TestAttribute>().Any() || _method.GetCustomAttributes<TestCaseAttribute>().Any())
                    .OrderBy(_method => _method.Name)
                    .ToList();

                if (_tests.Count == 0)
                    continue;

                bool _printedFixture = false;

                foreach (MethodInfo _test in _tests)
                {
                    foreach (object[] _arguments in GetInvocations(_test))
                    {
                        string _name = $"{_fixture.Name}.{_test.Name}{DescribeArguments(_arguments)}";

                        if (_filter != null && _name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            _skipped++;
                            continue;
                        }

                        if (!_printedFixture)
                        {
                            Console.WriteLine($"  {_fixture.Name}");
                            _printedFixture = true;
                        }

                        string _error = RunOne(_fixture, _test, _arguments);

                        if (_error == null)
                        {
                            _passed++;
                            Console.WriteLine($"    PASS  {_test.Name}{DescribeArguments(_arguments)}");
                        }
                        else
                        {
                            _failures.Add(new Failure { Name = _name, Message = _error });
                            Console.WriteLine($"    FAIL  {_test.Name}{DescribeArguments(_arguments)}");
                        }
                    }
                }
            }

            _stopwatch.Stop();

            Console.WriteLine();

            foreach (Failure _failure in _failures)
            {
                Console.WriteLine($"FAILED  {_failure.Name}");
                Console.WriteLine(Indent(_failure.Message));
                Console.WriteLine();
            }

            string _skippedText = _skipped > 0 ? $", {_skipped} filtered out" : string.Empty;
            Console.WriteLine($"{_passed} passed, {_failures.Count} failed{_skippedText} in {_stopwatch.ElapsedMilliseconds} ms");

            return _failures.Count == 0 ? 0 : 1;
        }

        private static IEnumerable<object[]> GetInvocations(MethodInfo _test)
        {
            TestCaseAttribute[] _cases = _test.GetCustomAttributes<TestCaseAttribute>().ToArray();

            if (_cases.Length == 0)
                return new[] { Array.Empty<object>() };

            return _cases.Select(_case => _case.Arguments);
        }

        private static string RunOne(Type _fixture, MethodInfo _test, object[] _arguments)
        {
            object _instance = null;

            try
            {
                _instance = Activator.CreateInstance(_fixture);

                InvokeMarked<SetUpAttribute>(_fixture, _instance);

                try
                {
                    _test.Invoke(_instance, _arguments);
                }
                finally
                {
                    InvokeMarked<TearDownAttribute>(_fixture, _instance);
                }

                return null;
            }
            catch (TargetInvocationException _exception) when (_exception.InnerException != null)
            {
                return Describe(_exception.InnerException);
            }
            catch (Exception _exception)
            {
                return Describe(_exception);
            }
            finally
            {
                (_instance as IDisposable)?.Dispose();
            }
        }

        private static void InvokeMarked<TAttribute>(Type _fixture, object _instance) where TAttribute : Attribute
        {
            foreach (MethodInfo _method in _fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (_method.GetCustomAttribute<TAttribute>() != null)
                    _method.Invoke(_instance, Array.Empty<object>());
            }
        }

        private static string Describe(Exception _exception)
        {
            if (_exception is AssertionException)
                return _exception.Message.TrimEnd();

            return $"{_exception.GetType().Name}: {_exception.Message}\n{_exception.StackTrace}".TrimEnd();
        }

        private static string DescribeArguments(object[] _arguments)
        {
            if (_arguments == null || _arguments.Length == 0)
                return string.Empty;

            return "(" + string.Join(", ", _arguments.Select(_argument => _argument?.ToString() ?? "null")) + ")";
        }

        private static string Indent(string _text)
        {
            return string.Join("\n", _text.Replace("\r\n", "\n").Split('\n').Select(_line => "    " + _line));
        }
    }
}
