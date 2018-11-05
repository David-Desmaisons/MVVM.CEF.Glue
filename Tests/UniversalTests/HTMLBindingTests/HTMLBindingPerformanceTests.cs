﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neutronium.Core;
using Neutronium.Core.WebBrowserEngine.JavascriptObject;
using Neutronium.Example.ViewModel;
using Tests.Infra.IntegratedContextTesterHelper.Windowless;
using Tests.Infra.WebBrowserEngineTesterHelper.HtmlContext;
using Xunit;
using Xunit.Abstractions;
using System.Windows.Input;
using MoreCollection.Extensions;
using Neutronium.Core.Binding.GlueObject;
using Newtonsoft.Json;
using System.Text;
using Neutronium.Core.Test.Helper;

namespace Tests.Universal.HTMLBindingTests
{
    public abstract class HtmlBindingPerformanceTests : HtmlBindingBase
    {
        protected const int DelayForTimeOut = 100;
        private const string PlateformDependant = "Plateform Dependant";
        private readonly Dictionary<TestPerformanceKind, int> _ExpectedTimeInMilliSeconds;
        protected HtmlBindingPerformanceTests(IWindowLessHTMLEngineProvider testEnvironment, ITestOutputHelper output, Dictionary<TestPerformanceKind, int> expectedTimeInMilliSeconds) : base(testEnvironment, output)
        {
            _ExpectedTimeInMilliSeconds = expectedTimeInMilliSeconds;
        }

        protected PerformanceHelper GetPerformanceCounter(string description, int numberOfOperations)
            => PerformanceHelper.OperationPerSec(_TestOutputHelper, description, numberOfOperations);

        private class TwoList
        {
            public TwoList()
            {
                L1 = new List<Skill>();
                L2 = new List<Skill>();
            }
            public List<Skill> L1 { get; }
            public List<Skill> L2 { get; }
        }

        private void CheckVsExpectation(long value, TestPerformanceKind context)
        {
            var expected = GetExpected(context);
            _Logger.Info($"Time expectation for the task: {expected}");
            TimeSpan.FromMilliseconds(value).Should().BeLessThan(expected);
        }

        private TimeSpan GetExpected(TestPerformanceKind context)
        {
            return TimeSpan.FromMilliseconds(_ExpectedTimeInMilliSeconds[context]);
        }

        [Fact(Skip = PlateformDependant)]
        public Task Stress_TwoWay_Collection_CreateBinding()
        {
            return Test_HTMLBinding_Stress_Collection_CreateBinding(JavascriptBindingMode.TwoWay, TestPerformanceKind.TwoWay_Collection_CreateBinding, TestContext.Simple);
        }

        [Fact(Skip = PlateformDependant)]
        public Task Stress_OneWay_Collection_CreateBinding()
        {
            return Test_HTMLBinding_Stress_Collection_CreateBinding(JavascriptBindingMode.OneWay, TestPerformanceKind.OneWay_Collection_CreateBinding, TestContext.Simple);
        }

        [Fact(Skip = PlateformDependant)]
        public Task Stress_OneTime_Collection_CreateBinding()
        {
            return Test_HTMLBinding_Stress_Collection_CreateBinding(JavascriptBindingMode.OneTime, TestPerformanceKind.OneTime_Collection_CreateBinding, TestContext.Simple);
        }

        private Task Test_HTMLBinding_Stress_Collection_CreateBinding(JavascriptBindingMode imode, TestPerformanceKind context, TestContext ipath = TestContext.Index)
        {
            int r = 100;
            var datacontext = new TwoList();
            datacontext.L1.AddRange(Enumerable.Range(0, r).Select(i => new Skill()));

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var test = new TestInContext()
            {
                Path = ipath,
                Bind = (win) => HtmlBinding.Bind(win, datacontext, imode),
                Test = (mb) =>
                {
                    stopWatch.Stop();
                    var ts = stopWatch.ElapsedMilliseconds;
                    _Logger.Info($"Perf: {((double)(ts)) / 1000} sec for {r} iterations");

                    var js = mb.JsRootObject;

                    var col = GetSafe(() => GetCollectionAttribute(js, "L1"));
                    col.GetArrayLength().Should().Be(r);

                    CheckVsExpectation(ts, context);
                }
            };
            return RunAsync(test);
        }

        [Fact(Skip = PlateformDependant)]
        public async Task Bind_ShouldBeRobust()
        {
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = _ => { }
            };

            for (var i = 0; i < 150; i++)
            {
                _Logger.Info($"Runing interaction {i}");
                await RunAsync(test);
            }
        }

        [Fact(Skip = PlateformDependant)]
        public async Task Stress_Collection_Update_From_Javascript()
        {
            int r = 100;
            var datacontext = new TwoList();
            datacontext.L1.AddRange(Enumerable.Range(0, r).Select(i => new Skill()));

            var test = new TestInContextAsync()
            {
                Path = TestContext.Simple,
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var col1 = GetCollectionAttribute(js, "L1");
                    col1.GetArrayLength().Should().Be(r);

                    var col2 = GetCollectionAttribute(js, "L2");
                    col2.GetArrayLength().Should().Be(0);

                    var l2c = GetAttribute(js, "L2");
                    l2c.Should().NotBeNull();

                    var javascript = "window.app = function(value,coll){var args = []; args.push(0); args.push(0); for (var i = 0; i < value.length; i++) { args.push(value[i]);} coll.splice.apply(coll, args);  console.log(value.length); console.log(coll.length);};";
                    IJavascriptObject res = null;
                    bool ok = _WebView.Eval(javascript, out res);
                    ok.Should().BeTrue();

                    bool notok = true;
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    DoSafe(() => Call(_WebView.GetGlobal(), "app", col1, l2c));
                    while (notok)
                    {
                        await Task.Delay(100);
                        notok = datacontext.L2.Count != r;
                    }
                    stopWatch.Stop();
                    long ts = stopWatch.ElapsedMilliseconds;

                    _Logger.Info($"Perf: {((double)(ts)) / 1000} sec for {r} iterations");
                    CheckVsExpectation(ts, TestPerformanceKind.TwoWay_Collection_Update_From_Javascript);
                }
            };

            await RunAsync(test);
        }

        [Fact(Skip = PlateformDependant)]
        public async Task Stress_TwoWay_Int()
        {
            var test = new TestInContextAsync()
            {
                Path = TestContext.Simple,
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    int iis = 500;
                    for (int i = 0; i < iis; i++)
                    {
                        _DataContext.Age += 1;
                    }

                    bool notok = true;
                    var tg = _DataContext.Age;
                    await Task.Delay(700);

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    while (notok)
                    {
                        await Task.Delay(100);
                        var doublev = GetIntAttribute(js, "Age");
                        notok = doublev != tg;
                    }
                    stopWatch.Stop();
                    var ts = stopWatch.ElapsedMilliseconds;
                    _Logger.Info($"Perf: {((double)(ts)) / 1000} sec for {iis} iterations");

                    CheckVsExpectation(ts, TestPerformanceKind.TwoWay_Int);
                }
            };

            await RunAsync(test);
        }

        [Fact(Skip = PlateformDependant)]
        public async Task Stress_TwoWay_Collection()
        {

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    col.GetArrayLength().Should().Be(2);

                    Check(col, _DataContext.Skills);

                    _DataContext.Skills.Add(new Skill() { Name = "C++", Type = "Info" });

                    await Task.Delay(150);
                    col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                    Check(col, _DataContext.Skills);

                    _DataContext.Skills[0] = new Skill() { Name = "HTML5", Type = "Info" };
                    int iis = 500;
                    for (int i = 0; i < iis; i++)
                    {
                        _DataContext.Skills.Insert(0, new Skill() { Name = "HTML5", Type = "Info" });
                    }

                    bool notok = true;
                    int tcount = _DataContext.Skills.Count;

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    while (notok)
                    {
                        await Task.Delay(10);
                        col = GetSafe(() => GetCollectionAttribute(js, "Skills"));
                        notok = col.GetArrayLength() != tcount;
                    }
                    stopWatch.Stop();
                    var ts = stopWatch.ElapsedMilliseconds;
                    _Logger.Info($"Perf: {((double)(ts)) / 1000} sec for {iis} iterations");
                    Check(col, _DataContext.Skills);

                    CheckVsExpectation(ts, TestPerformanceKind.TwoWay_Collection);
                }
            };

            await RunAsync(test);
        }

        private class BigCollectionVM<T>
        {
            public static int Limit { get; } = 300000;
            public T[] Values { get; }

            public BigCollectionVM(T value) : this(Enumerable.Repeat(value, Limit))
            {
            }

            public BigCollectionVM(Func<int, T> value) : this(Enumerable.Range(0, 300000).Select(value))
            {
            }

            public BigCollectionVM(IEnumerable<T> values)
            {
                Values = values.ToArray();
            }
        }

        [Fact]
        public async Task Binding_Can_Create_Collection_With_Count_Greater_Than_Max_Stack()
        {
            var value = 3;
            var dataContext = new BigCollectionVM<int>(value);
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, dataContext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetCollectionAttribute(js, "Values");
                    res.GetArrayLength().Should().Be(BigCollectionVM<int>.Limit);

                    var lastElement = res.GetValue(BigCollectionVM<int>.Limit - 1);
                    lastElement.GetIntValue().Should().Be(value);
                }
            };
            await RunAsync(test);
        }

        private class Simple
        {
            public int Id { get; } = 23;
        }

        [Fact]
        public async Task Binding_Can_Create_More_Array_Relashionship_Than_Max_Stack()
        {
            var dataContext = new BigCollectionVM<Simple>(new Simple());
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, dataContext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetCollectionAttribute(js, "Values");
                    res.GetArrayLength().Should().Be(BigCollectionVM<Simple>.Limit);

                    var lastElement = res.GetValue(BigCollectionVM<Simple>.Limit - 1);
                    var id = GetIntAttribute(lastElement, "Id");
                    id.Should().Be(23);
                }
            };
            await RunAsync(test);
        }

        [Fact]
        public async Task Binding_Can_Create_More_Object_Than_Max_Stack()
        {
            var dataContext = new BigCollectionVM<Simple>(_ => new Simple());
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, dataContext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetCollectionAttribute(js, "Values");
                    res.GetArrayLength().Should().Be(BigCollectionVM<Simple>.Limit);

                    var lastElement = res.GetValue(BigCollectionVM<Simple>.Limit - 1);
                    var id = GetIntAttribute(lastElement, "Id");
                    id.Should().Be(23);
                }
            };
            await RunAsync(test);
        }


        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        [InlineData(30)]
        [InlineData(50)]
        public async Task Stress_Big_Vm(int childrenCount)
        {
            var root = new SimpleFatherTestViewModel();
            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, root, JavascriptBindingMode.TwoWay),
                Test = async mb => await StressVm(root, mb, SimpleReadOnlyTestViewModel.BuildBigVm(childrenCount))
            };

            await RunAsync(test);
        }

        private async Task StressVm(SimpleFatherTestViewModel root, IHtmlBinding mb, SimpleReadOnlyTestViewModel bigVm)
        {
            var js = mb.JsRootObject;
            IJavascriptObject other;

            using (var perf = GetPerformanceCounter("Perf to bind Vm"))
            {
                await DoSafeAsyncUI(() => root.Other = bigVm);

                await Task.Delay(DelayForTimeOut);
                perf.DiscountTime = DelayForTimeOut;

                other = await _WebView.EvaluateAsync(() => GetAttribute(js, "Other"));
            }

            other.IsObject.Should().BeTrue();
            var rootJs = mb.JsBrideRootObject;
            ISet<IJsCsGlue> allChildren = new HashSet<IJsCsGlue>();

            using (GetPerformanceCounter("Perf to VisitAllChildren"))
            {
                await DoSafeAsyncUI(() => rootJs.VisitDescendantsSafe(glue => true));
            }

            using (GetPerformanceCounter("Perf to VisitAllChildren Collect"))
            {
                await DoSafeAsyncUI(() => rootJs.VisitDescendantsSafe(glue => allChildren.Add(glue)));
            }

            _TestOutputHelper.WriteLine($"Objects created {allChildren.Count}");

            using (GetPerformanceCounter("Perf Foreach GetAllChildren"))
            {
                await DoSafeAsyncUI(() => allChildren.ForEach(_ => { }));
            }

            List<object> basics = null;
            using (GetPerformanceCounter("Perf Collecting basics"))
            {
                basics = allChildren.Where(g => g.Type == JsCsGlueType.Basic && g.CValue != null).Select(g => g.CValue).ToList();
            }

            string string1, string2, string3, string4;

            using (GetPerformanceCounter("Creating string"))
            {
                string1 = $"[{string.Join(",", basics.Select(JavascriptNamer.GetCreateExpression))}]";
            }

            using (GetPerformanceCounter("Creating string parralel"))
            {
                var builder = new StringBuilder("[");
                var first = true;
                Parallel.ForEach(basics, () => new Builder() , (basic, _, sb) =>
                {
                    if (!sb.First)
                        sb.String.Append(",");
                    sb.String.Append(basic);
                    sb.First = false;
                    return sb;
                }, 
                (sb) =>
                {
                    lock (builder)
                    {
                        if (!first)
                        {
                            builder.Append(",");
                        }
                        first = false;
                        builder.Append(sb.String);
                    }
                });
                builder.Append("]");

                string2 = builder.ToString();
            }

            var array = basics.ToArray();

            using (GetPerformanceCounter("Creating JSON string"))
            {
                string3 = JsonConvert.SerializeObject(array);
            }

            using (GetPerformanceCounter("Creating JSON string bigVm"))
            {
                string4 = JsonConvert.SerializeObject(bigVm);
            }
        }

        private class Builder
        {
            public StringBuilder String { get; }
            public bool First { get; set; } = true;

            public Builder()
            {
                String = new StringBuilder();
            }
        }

        [Theory]
        [InlineData(2000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public async Task Stress_Big_Vm_Commands(int commandsCount)
        {
            var root = new CommandsTestViewModel();
            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, root, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var commands = Enumerable.Range(0, commandsCount).Select(_ => NSubstitute.Substitute.For<ICommand>()).ToArray();

                    using (var perf = GetPerformanceCounter($"Perf to create Vm with {commandsCount} commands"))
                    {
                        perf.DiscountTime = DelayForTimeOut;
                        await DoSafeAsyncUI(() => root.Commands = commands);

                        await Task.Delay(DelayForTimeOut);
                        var other = await _WebView.EvaluateAsync(() => GetAttribute(js, "Commands"));

                        other.IsArray.Should().BeTrue();
                    }
                }
            };

            await RunAsync(test);
        }
    }
}
