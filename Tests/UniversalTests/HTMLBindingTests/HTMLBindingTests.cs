﻿using AutoFixture.Xunit2;
using FluentAssertions;
using Neutronium.Core;
using Neutronium.Core.Binding.GlueObject;
using Neutronium.Core.Exceptions;
using Neutronium.Core.Infra.Reflection;
using Neutronium.Core.Test.Helper;
using Neutronium.Core.WebBrowserEngine.JavascriptObject;
using Neutronium.Example.ViewModel;
using Neutronium.Example.ViewModel.Infra;
using Neutronium.MVVMComponents;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Tests.Infra.IntegratedContextTesterHelper.Windowless;
using Tests.Infra.WebBrowserEngineTesterHelper.HtmlContext;
using Tests.Infra.WebBrowserEngineTesterHelper.Window;
using Tests.Universal.HTMLBindingTests.Helper;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Universal.HTMLBindingTests
{
    public abstract partial class HtmlBindingTests : HtmlBindingBase
    {
        protected HtmlBindingTests(IWindowLessHTMLEngineProvider testEnvironment, ITestOutputHelper output)
            : base(testEnvironment, output)
        {
        }

        [Fact]
        public async Task InvokeAsync_No_Function()
        {
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.OneWay),
                Test = (binding) =>
                {
                    var js = binding.JsRootObject;
                    var res = js.InvokeAsync("NotFound", binding.Context).Result;
                    res.IsUndefined.Should().BeTrue();
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task GetElements_Should_Throw_ArgumentException()
        {
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.OneWay),
                Test = (binding) =>
                {
                    var js = binding.JsRootObject;
                    IJavascriptObject[] res = null;
                    Action act = () => res = js.GetArrayElements();
                    act.Should().Throw<ArgumentException>();
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task OneWay_JSON_ToString()
        {
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.OneWay),
                Test = (binding) =>
                {
                    var jsbridge = ((HtmlBinding)binding).JsBrideRootObject;
                    var alm = jsbridge.ToString();

                    JsArray arr = null;
                    jsbridge.VisitDescendantsSafe(glue =>
                    {
                        arr = arr ?? glue as JsArray;
                        return glue != null;
                    });

                    dynamic m = JsonConvert.DeserializeObject<dynamic>(jsbridge.ToString());
                    ((string)m.LastName).Should().Be("Desmaisons");
                    ((string)m.Name).Should().Be("O Monstro");

                    binding.ToString().Should().Be(alm);
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task Bind_WithoutFramework_ShouldThrowException()
        {
            using (Tester(TestContext.EmptyWithJs))
            {
                var vm = new object();
                NeutroniumException ex = null;

                try
                {
                    await HtmlBinding.Bind(_ViewEngine, new object(), JavascriptBindingMode.OneTime);
                }
                catch (AggregateException agregate)
                {
                    ex = agregate.Flatten().InnerException as NeutroniumException;
                }
                catch (NeutroniumException myex)
                {
                    ex = myex;
                }

                ex.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Bind_WithoutCorrectJs_ShouldThrowException()
        {
            using (Tester(TestContext.AlmostEmpty))
            {
                var vm = new object();
                NeutroniumException ex = null;

                try
                {
                    await HtmlBinding.Bind(_ViewEngine, _DataContext, JavascriptBindingMode.OneTime);
                }
                catch (NeutroniumException myex)
                {
                    ex = myex;
                }

                ex.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task OneTime_CheckContext()
        {
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.OneTime),
                Test = (mb) =>
                {
                    mb.Context.Should().NotBeNull();
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task OneTime()
        {
            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.OneTime),
                Test = async (mb) =>
                {
                    var jsbridge = (mb as HtmlBinding).JsBrideRootObject;
                    var js = mb.JsRootObject;

                    string JSON = JsonConvert.SerializeObject(_DataContext);
                    string alm = jsbridge.ToString();

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("O Monstro");

                    string res2 = GetStringAttribute(js, "LastName");
                    res2.Should().Be("Desmaisons");

                    _DataContext.Name = "23";
                    await Task.Delay(200);

                    string res3 = GetStringAttribute(js, "Name");
                    res3.Should().Be("O Monstro");

                    string res4 = GetLocalCity(js);
                    res4.Should().Be("Florianopolis");

                    _DataContext.Local.City = "Paris";
                    await Task.Delay(200);

                    //onetime does not update javascript from  C# 
                    res4 = GetLocalCity(js);
                    res4.Should().Be("Florianopolis");

                    string res5 = GetFirstSkillName(js);
                    res5.Should().Be("Langage");

                    _DataContext.Skills[0].Name = "Ling";
                    await Task.Delay(200);

                    //onetime does not update javascript from  C# 
                    res5 = GetFirstSkillName(js);
                    res5.Should().Be("Langage");

                    //onetime does not update C# from javascript
                    var stringName = Create(() => _WebView.Factory.CreateString("resName"));
                    SetAttribute(js, "Name", stringName);
                    await Task.Delay(200);
                    _DataContext.Name.Should().Be("23");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task OneWay()
        {
            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.OneWay),
                Test = async (mb) =>
                {
                    var jsbridge = (mb as HtmlBinding).JsBrideRootObject;
                    var js = mb.JsRootObject;

                    string JSON = JsonConvert.SerializeObject(_DataContext);
                    string alm = jsbridge.ToString();

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("O Monstro");

                    string res2 = GetStringAttribute(js, "LastName");
                    res2.Should().Be("Desmaisons");

                    DoSafeUI(() =>
                    {
                        _DataContext.Name = "23";
                    });
                    await Task.Delay(200);

                    string res3 = GetStringAttribute(js, "Name");
                    res3.Should().Be("23");

                    string res4 = GetLocalCity(js);
                    res4.Should().Be("Florianopolis");

                    DoSafeUI(() =>
                    {
                        _DataContext.Local.City = "Paris";
                    });
                    await Task.Delay(300);

                    res4 = GetLocalCity(js);
                    ((string)res4).Should().Be("Paris");

                    string res5 = GetFirstSkillName(js);
                    res5.Should().Be("Langage");

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills[0].Name = "Ling";
                    });
                    await Task.Delay(300);

                    res5 = GetFirstSkillName(js);
                    res5.Should().Be("Ling");

                    var stringName = Create(() => _WebView.Factory.CreateString("resName"));
                    SetAttribute(js, "Name", stringName);
                    await Task.Delay(200);
                    _DataContext.Name.Should().Be("23");
                }
            };

            await RunAsync(test);
        }

        private string GetLocalCity(IJavascriptObject js)
        {
            return GetStringAttribute(GetAttribute(js, "Local"), "City"); ;
        }

        private string GetFirstSkillName(IJavascriptObject javascriptObject)
        {
            var firstSkill = GetCollectionAttribute(javascriptObject, "Skills").GetValue(0);
            return GetStringAttribute(firstSkill, "Name");
        }

        private class Dummy
        {
            internal Dummy()
            {
                Int = 5;
            }
            public int Int { get; set; }
            public int Explosive { get { throw new Exception(); } }
        }


        [Fact]
        public async Task OneWay_Property_With_Exception()
        {
            var dt = new Dummy();

            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, dt, JavascriptBindingMode.OneWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;

                    int res = GetIntAttribute(js, "Int");
                    res.Should().Be(5);
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task RootViewModel_CanBeExtended_ByComputedProperties()
        {
            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.OneWay),
                Test = (mb) =>
                {
                    var js = GetRootViewModel();

                    string res = GetStringAttribute(js, "completeName");
                    res.Should().Be("O Monstro Desmaisons");
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task Null_Property()
        {
            _DataContext.MainSkill.Should().BeNull();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetAttribute(js, "MainSkill");
                    res.IsNull.Should().BeTrue();

                    DoSafeUI(() => _DataContext.MainSkill = new Skill()
                    {
                        Name = "C++",
                        Type = "Info"
                    });

                    await Task.Delay(200);

                    res = GetAttribute(js, "MainSkill");
                    DoSafe(() =>
                    {
                        res.IsNull.Should().BeFalse();
                        res.IsObject.Should().BeTrue();
                    });


                    string inf = GetStringAttribute(res, "Type");
                    inf.Should().Be("Info");

                    DoSafeUI(() => _DataContext.MainSkill = null);
                    await Task.Delay(200);

                    res = GetAttribute(js, "MainSkill");

                    //GetSafe(()=>res.IsNull).Should().BeTrue();
                    //Awesomium limitation can not test on isnull
                    //Todo: create specific test
                    var obj = default(object);
                    var boolres = GetSafe(() => _WebView.Converter.GetSimpleValue(res, out obj));
                    boolres.Should().BeTrue();
                    obj.Should().BeNull();
                }
            };
            await RunAsync(test);
        }

        [Fact]
        public async Task Circular_reference()
        {
            var datacontext = new Neutronium.Example.ViewModel.ForNavigation.Couple();
            var my = new Neutronium.Example.ViewModel.ForNavigation.Person()
            {
                Name = "O Monstro",
                LastName = "Desmaisons",
                Local = new Local() { City = "Florianopolis", Region = "SC" }
            };
            my.Couple = datacontext;
            datacontext.One = my;

            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;

                    var One = GetAttribute(js, "One");

                    string res = GetStringAttribute(One, "Name");
                    res.Should().Be("O Monstro");

                    string res2 = GetStringAttribute(One, "LastName");
                    res2.Should().Be("Desmaisons");

                    //Test no stackoverflow in case of circular refernce
                    var jsbridge = (mb as HtmlBinding).JsBrideRootObject;
                    string alm = jsbridge.ToString();
                    alm.Should().NotBeNull();
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task TwoWay_UpdateJSFromCSharp()
        {
            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("O Monstro");

                    DoSafeUI(() =>
                    {
                        _DataContext.Name = "23";
                    });

                    await Task.Delay(350);

                    string res3 = GetStringAttribute(js, "Name");
                    res3.Should().Be("23");
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task TwoWay_UpdateCSharpFromJS()
        {
            var datacontext = new SimpleViewModel() { Name = "teste0" };

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("teste0");

                    var expected = "teste1";
                    var stringValue = Create(() => _WebView.Factory.CreateString(expected));
                    SetAttribute(js, "Name", stringValue);

                    await Task.Delay(50);

                    datacontext.Name.Should().Be(expected);
                    _Logger.Info("Test ended sucessfully");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Works_WithNullable()
        {
            var dataContext = new NullableTestViewModel
            {
                Bool = true,
                Int32 = 25
            };

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, dataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetBoolAttribute(js, nameof(dataContext.Bool));
                    res.Should().Be(true);

                    var res2 = GetIntAttribute(js, nameof(dataContext.Int32));
                    res2.Should().Be(25);

                    var res3 = GetAttribute(js, nameof(dataContext.Decimal));
                    res3.IsNull.Should().BeTrue();

                    //Test Two Way Decimal: null => value
                    var newDecimal = Create(() => _WebView.Factory.CreateDouble(0.5));
                    SetAttribute(js, nameof(dataContext.Decimal), newDecimal);

                    await Task.Delay(50);
                    var newValue = GetDoubleAttribute(js, nameof(dataContext.Decimal));
                    newValue.Should().Be(0.5);

                    await Task.Delay(50);

                    DoSafeUI(() =>
                    {
                        dataContext.Decimal.Should().Be(0.5m);
                    });

                    //Test Two Way bool value => null
                    var nullJs = Create(() => _WebView.Factory.CreateNull());
                    SetAttribute(js, nameof(dataContext.Bool), nullJs);

                    await Task.Delay(50);
                    var boolValue = GetAttribute(js, nameof(dataContext.Bool));
                    boolValue.IsNull.Should().BeTrue();

                    await Task.Delay(50);

                    DoSafeUI(() =>
                    {
                        dataContext.Bool.Should().NotHaveValue();
                    });


                    //Test Two Way int value => value
                    var intValueJS = Create(() => _WebView.Factory.CreateInt(54));
                    SetAttribute(js, nameof(dataContext.Int32), intValueJS);

                    await Task.Delay(50);
                    var intValue = GetIntAttribute(js, nameof(dataContext.Int32));
                    intValue.Should().Be(54);

                    await Task.Delay(50);

                    DoSafeUI(() =>
                    {
                        dataContext.Int32.Should().Be(54);
                    });
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task TwoWay()
        {
            _DataContext.MainSkill.Should().BeNull();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("O Monstro");

                    string res2 = GetStringAttribute(js, "LastName");
                    res2.Should().Be("Desmaisons");

                    DoSafeUI(() =>
                    {
                        _DataContext.Name = "23";
                    });

                    await Task.Delay(50);

                    string res3 = GetStringAttribute(js, "Name");
                    res3.Should().Be("23");

                    string res4 = GetLocalCity(js);
                    res4.Should().Be("Florianopolis");

                    DoSafeUI(() =>
                    {
                        _DataContext.Local.City = "Paris";
                    });
                    await Task.Delay(50);

                    res4 = GetLocalCity(js);
                    ((string)res4).Should().Be("Paris");

                    string res5 = GetFirstSkillName(js);
                    res5.Should().Be("Langage");

                    DoSafeUI(() =>
                    {
                        _DataContext.Skills[0].Name = "Ling";
                    });
                    await Task.Delay(50);

                    res5 = GetFirstSkillName(js);
                    res5.Should().Be("Ling");

                    //Test Two Way
                    var stringName = Create(() => _WebView.Factory.CreateString("resName"));
                    SetAttribute(js, "Name", stringName);

                    await Task.Delay(150);
                    string resName = GetStringAttribute(js, "Name");
                    resName.Should().Be("resName");

                    await Task.Delay(150);

                    _DataContext.Name.Should().Be("resName");

                    DoSafeUI(() =>
                    {
                        _DataContext.Name = "nnnnvvvvvvv";
                    });

                    await Task.Delay(50);
                    res3 = GetStringAttribute(js, "Name");
                    ((string)res3).Should().Be("nnnnvvvvvvv");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Nested()
        {
            _DataContext.MainSkill.Should().BeNull();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("O Monstro");

                    string res2 = GetStringAttribute(js, "LastName");
                    res2.Should().Be("Desmaisons");

                    var local = GetAttribute(js, "Local");
                    string city = GetStringAttribute(local, "City");
                    city.Should().Be("Florianopolis");

                    DoSafeUI(() =>
                    {
                        _DataContext.Local.City = "Foz de Iguaçu";
                    });

                    await Task.Delay(100);
                    var local3 = GetAttribute(js, "Local");
                    string city3 = GetStringAttribute(local, "City");
                    city3.Should().Be("Foz de Iguaçu");

                    DoSafeUI(() =>
                    {
                        _DataContext.Local = new Local() { City = "Paris" };
                    });

                    await Task.Delay(100);
                    var local2 = GetAttribute(js, "Local");
                    string city2 = GetStringAttribute(local2, "City");
                    city2.Should().Be("Paris");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Listens_to_Nested_Changes_After_Property_Updates_CSharp_Updates()
        {
            _DataContext.MainSkill.Should().BeNull();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var local = new Local
                    {
                        City = "JJC"
                    };

                    DoSafeUI(() =>
                    {
                        _DataContext.Local = local;
                    });

                    await Task.Delay(100);

                    DoSafeUI(() =>
                    {
                        local.City = "Floripa";
                    });

                    await Task.Delay(100);

                    var js = mb.JsRootObject;

                    var jsLocal = GetAttribute(js, "Local");
                    string city = GetStringAttribute(jsLocal, "City");
                    city.Should().Be("Floripa");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Listens_to_Nested_Changes_After_Property_Updates_Javascript_Updates()
        {
            _DataContext.MainSkill.Should().BeNull();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var local = new Local
                    {
                        City = "JJC"
                    };

                    DoSafeUI(() =>
                    {
                        _DataContext.Local = local;
                    });

                    await Task.Delay(100);

                    var js = mb.JsRootObject;

                    var jsLocal = GetAttribute(js, "Local");

                    var stringName = Create(() => _WebView.Factory.CreateString("Floripa"));
                    SetAttribute(jsLocal, "City", stringName);

                    await Task.Delay(100);

                    _DataContext.Local.City.Should().Be("Floripa");
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task TwoWay_Enum()
        {
            _DataContext.MainSkill.Should().BeNull();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetAttribute(js, "PersonalState");
                    string dres = GetSafe(() => res.GetValue("displayName").GetStringValue());
                    dres.Should().Be("Married");

                    DoSafeUI(() =>
                    {
                        _DataContext.PersonalState = PersonalState.Single;
                    });
                    await Task.Delay(50);

                    res = GetAttribute(js, "PersonalState");
                    dres = GetSafe(() => res.GetValue("displayName").GetStringValue());
                    dres.Should().Be("Single");
                }
            };

            await RunAsync(test);

        }

        [Fact]
        public async Task TwoWay_Enum_Round_Trip()
        {
            _DataContext.Name = "toto";

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, _DataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetAttribute(js, "PersonalState");
                    string dres = GetSafe(() => res.GetValue("displayName").GetStringValue());
                    dres.Should().Be("Married");

                    DoSafeUI(() =>
                    {
                        _DataContext.PersonalState = PersonalState.Single;
                    });
                    await Task.Delay(50);

                    res = GetAttribute(js, "PersonalState");
                    dres = GetSafe(() => res.GetValue("displayName").GetStringValue());
                    dres.Should().Be("Single");

                    var othervalue = GetCollectionAttribute(js, "States");

                    var di = othervalue.GetValue(2);
                    string name = di.GetValue("displayName").GetStringValue();
                    name.Should().Be("Divorced");

                    SetAttribute(js, "PersonalState", di);
                    await Task.Delay(100);

                    _DataContext.PersonalState.Should().Be(PersonalState.Divorced);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Listens_to_property_update_during_property_changes_update_from_js()
        {
            var dataContext = new PropertyUpdatingTestViewModel();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, dataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetStringAttribute(js, "Property1");
                    res.Should().Be("1");

                    res = GetStringAttribute(js, "Property2");
                    res.Should().Be("2");

                    SetAttribute(js, "Property1", _WebView.Factory.CreateString("a"));

                    await Task.Delay(100);

                    res = GetStringAttribute(js, "Property1");
                    res.Should().Be("a");

                    res = GetStringAttribute(js, "Property2");
                    res.Should().Be("a", "Neutronium listen to object during update");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Listens_to_property_update_during_property_changes_update_from_Csharp()
        {
            var dataContext = new PropertyUpdatingTestViewModel();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, dataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetStringAttribute(js, "Property1");
                    res.Should().Be("1");

                    res = GetStringAttribute(js, "Property2");
                    res.Should().Be("2");

                    DoSafeUI(() =>
                    {
                        dataContext.Property1 = "a";
                    });

                    await Task.Delay(50);

                    res = GetStringAttribute(js, "Property1");
                    res.Should().Be("a");

                    res = GetStringAttribute(js, "Property2");
                    res.Should().Be("a", "Neutronium listen to object during update");
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Maps_ExpandoObject()
        {
            dynamic dynamicDataContext = new ExpandoObject();
            dynamicDataContext.ValueInt = 1;
            dynamicDataContext.ValueString = "string";
            dynamicDataContext.ValueBool = true;

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, dynamicDataContext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var resInt = GetIntAttribute(js, "ValueInt");
                    resInt.Should().Be(1);

                    var resString = GetStringAttribute(js, "ValueString");
                    resString.Should().Be("string");

                    var resBool = GetBoolAttribute(js, "ValueBool");
                    resBool.Should().BeTrue();

                    DoSafeUI(() => { dynamicDataContext.ValueInt = 110; });

                    await Task.Delay(50);

                    resInt = GetIntAttribute(js, "ValueInt");
                    resInt.Should().Be(110);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Maps_DynamicObject()
        {
            var dynamicDataContext = new TestDynamicObject();

            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, dynamicDataContext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;

                    var resInt = GetIntAttribute(js, "Property5");
                    resInt.Should().Be(5);

                    resInt = GetIntAttribute(js, "Property0");
                    resInt.Should().Be(0);

                    resInt = GetIntAttribute(js, "Property1");
                    resInt.Should().Be(1);
                }
            };

            await RunAsync(test);
        }

        private class SimplePerson : ViewModelBase
        {
            private PersonalState _PersonalState;
            public PersonalState PersonalState
            {
                get => _PersonalState;
                set => Set(ref _PersonalState, value, "PersonalState");
            }
        }

        [Fact]
        public async Task TwoWay_Enum_NotMapped()
        {
            var datacontext = new SimplePerson
            {
                PersonalState = PersonalState.Single
            };

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetAttribute(js, "PersonalState");
                    string dres = GetSafe(() => res.GetValue("displayName").GetStringValue());
                    dres.Should().Be("Single");


                    DoSafeUI(() =>
                    {
                        datacontext.PersonalState = PersonalState.Married;
                    });
                    await Task.Delay(50);

                    res = GetAttribute(js, "PersonalState");
                    dres = GetSafe(() => res.GetValue("displayName").GetStringValue());
                    dres.Should().Be("Married");
                }

            };

            await RunAsync(test);
        }


        [Fact]
        public async Task TwoWay_Set_Object_From_Javascript()
        {
            var datacontext = new Couple();
            var p1 = new Person() { Name = "David" };
            datacontext.One = p1;
            var p2 = new Person() { Name = "Claudia" };
            datacontext.Two = p2;

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res1 = GetAttribute(js, "One");
                    string n1 = GetStringAttribute(res1, "Name");
                    n1.Should().Be("David");

                    var res2 = GetAttribute(js, "Two");
                    res2.Should().NotBeNull();
                    var n2 = GetStringAttribute(res2, "Name");
                    n2.Should().Be("Claudia");

                    var jsValue = GetAttribute(js, "Two");
                    SetAttribute(js, "One", jsValue);

                    await Task.Delay(100);

                    var res3 = GetAttribute(js, "One");
                    res3.Should().NotBeNull();
                    string n3 = GetStringAttribute(res3, "Name");
                    n3.Should().Be("Claudia");

                    await Task.Delay(100);

                    datacontext.One.Should().Be(p2);

                    var res4 = GetAttribute(res3, "ChildrenNumber");
                    res4.IsNull.Should().BeTrue();

                    var five = Create(() => _WebView.Factory.CreateInt(5));
                    SetAttribute(res3, "ChildrenNumber", five);
                    await Task.Delay(100);

                    datacontext.One.ChildrenNumber.Should().Be(5);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Set_Null_From_Javascript()
        {
            var datacontext = new Couple();
            var p1 = new Person() { Name = "David" };
            datacontext.One = p1;
            var p2 = new Person() { Name = "Claudia" };
            datacontext.Two = p2;

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res1 = GetAttribute(js, "One");
                    res1.Should().NotBeNull();
                    string n1 = GetStringAttribute(res1, "Name");
                    n1.Should().Be("David");

                    var res2 = GetAttribute(js, "Two");
                    res2.Should().NotBeNull();
                    string n2 = GetStringAttribute(res2, "Name");
                    n2.Should().Be("Claudia");

                    var nullSO = Create(() => _WebView.Factory.CreateNull());
                    SetAttribute(js, "One", nullSO);

                    await Task.Delay(100);
                    var res3 = GetAttribute(js, "One");
                    //GetSafe(() => res3.IsNull).Should().BeTrue();
                    //Init case of awesomium an object is used on JS side
                    //Todo: create specific test

                    await Task.Delay(100);

                    datacontext.One.Should().BeNull();
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Set_Object_From_Javascipt_Survive_MissUse()
        {
            var datacontext = new Couple();
            var p1 = new Person() { Name = "David" };
            datacontext.One = p1;
            var p2 = new Person() { Name = "Claudia" };
            datacontext.Two = p2;

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var res1 = GetAttribute(js, "One");
                    res1.Should().NotBeNull();
                    string n1 = GetStringAttribute(res1, "Name");
                    n1.Should().Be("David");

                    var res2 = GetAttribute(js, "Two");
                    res2.Should().NotBeNull();
                    string n2 = GetStringAttribute(res2, "Name");
                    n2.Should().Be("Claudia");

                    var StringJS = _WebView.Factory.CreateString("Dede");
                    SetAttribute(js, "One", StringJS);

                    string res3 = GetStringAttribute(js, "One");
                    res3.Should().Be("Dede");

                    await Task.Delay(100);

                    datacontext.One.Should().Be(p1);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_Set_Object_From_Javascipt_Survive_MissUse_NoReset_OnAttribute()
        {
            var datacontext = new Couple();
            var p1 = new Person() { Name = "David" };
            datacontext.One = p1;
            var p2 = new Person() { Name = "Claudia" };
            datacontext.Two = p2;

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var res1 = GetAttribute(js, "One");
                    res1.Should().NotBeNull();
                    string n1 = GetStringAttribute(res1, "Name");
                    n1.Should().Be("David");

                    var res2 = GetAttribute(js, "Two");
                    res2.Should().NotBeNull();
                    string n2 = GetStringAttribute(res2, "Name");
                    n2.Should().Be("Claudia");

                    var trueJs = _WebView.Factory.CreateObject(ObjectObservability.None);
                    SetAttribute(js, "One", trueJs);

                    var res3 = GetAttribute(js, "One");
                    res3.IsObject.Should().BeTrue();

                    await Task.Delay(100);
                    datacontext.One.Should().Be(p1);
                }
            };
            await RunAsync(test);
        }

        [Fact]
        public async Task Property_Survive_Missuse_Of_NotifyPropertyChanged()
        {
            var command = Substitute.For<ICommand>();
            var datacontexttest = new FakeTestViewModel() { Command = command };

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontexttest, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("NameTest");

                    var stringJS = _WebView.Factory.CreateString("NewName");
                    SetAttribute(js, "Name", stringJS);
                    res = GetStringAttribute(js, "Name");
                    res.Should().Be("NewName");

                    await Task.Delay(100);
                    datacontexttest.Name.Should().Be("NameTest");

                    var resf = GetSafe(() => js.HasValue("UselessName"));
                    resf.Should().BeFalse();

                    Action safe = () => DoSafeUI(() => datacontexttest.InconsistentEventEmit());

                    safe.Should().NotThrow("Inconsistent Name in property should not throw exception");
                }
            };

            await RunAsync(test);
        }

        private void CheckIntValue(IJavascriptObject js, string pn, int value)
        {
            IJavascriptObject res = GetAttribute(js, pn);
            res.Should().NotBeNull();
            res.IsNumber.Should().BeTrue();
            res.GetIntValue().Should().Be(0);
        }

        [Fact]
        public async Task TwoWay_CLR_Type_FromCtojavascript()
        {
            var command = Substitute.For<ISimpleCommand>();
            var datacontext = new ClrTypesTestViewModel();

            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;
                    js.Should().NotBeNull();

                    CheckIntValue(js, "Int64", 0);
                    CheckIntValue(js, "Int32", 0);
                    CheckIntValue(js, "Int16", 0);

                    CheckIntValue(js, "Uint16", 0);
                    CheckIntValue(js, "Uint32", 0);
                    CheckIntValue(js, "Uint64", 0);

                    CheckIntValue(js, "Double", 0);
                    CheckIntValue(js, "Decimal", 0);
                    CheckIntValue(js, "Float", 0);

                    CheckIntValue(js, "Byte", 0);
                    CheckIntValue(js, "Sbyte", 0);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_CLR_Type_From_CSharp()
        {
            var datacontext = new ClrTypesTestViewModel
            {
                Int64 = 32,
                Uint64 = 456,
                Int32 = 231,
                Uint32 = 77,
                Int16 = 55,
                Uint16 = 23,
                Float = 18.19f,
                Double = 55.88,
                Decimal = 47.89m,
                Byte = 12,
                Sbyte = -17
            };

            var test = new TestInContext()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;
                    js.Should().NotBeNull();

                    var res = GetIntAttribute(js, "Int64");
                    res.Should().Be((int)datacontext.Int64);

                    res = GetIntAttribute(js, "Uint64");
                    res.Should().Be((int)datacontext.Uint64);

                    res = GetIntAttribute(js, "Int32");
                    res.Should().Be(datacontext.Int32);

                    res = GetIntAttribute(js, "Uint32");
                    res.Should().Be((int)datacontext.Uint32);

                    res = GetIntAttribute(js, "Int16");
                    res.Should().Be(datacontext.Int16);

                    res = GetIntAttribute(js, "Uint16");
                    res.Should().Be(datacontext.Uint16);

                    var res2 = GetDoubleAttribute(js, "Float");
                    res2.Should().Be(datacontext.Float);

                    res2 = GetDoubleAttribute(js, "Double");
                    res2.Should().Be(datacontext.Double);

                    res2 = GetDoubleAttribute(js, "Decimal");
                    res2.Should().Be((double)datacontext.Decimal);

                    res = GetIntAttribute(js, "Byte");
                    res.Should().Be(datacontext.Byte);

                    res = GetIntAttribute(js, "Sbyte");
                    res.Should().Be(datacontext.Sbyte);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_CLR_Type_FromjavascripttoCto()
        {
            var datacontext = new ClrTypesTestViewModel();

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    js.Should().NotBeNull();

                    SetAttribute(js, "Int64", _WebView.Factory.CreateInt(32));
                    await Task.Delay(200);
                    datacontext.Int64.Should().Be(32);

                    SetAttribute(js, "Uint64", _WebView.Factory.CreateInt(456));
                    await Task.Delay(200);
                    datacontext.Uint64.Should().Be(456);

                    SetAttribute(js, "Int32", _WebView.Factory.CreateInt(5));
                    await Task.Delay(200);
                    datacontext.Int32.Should().Be(5);

                    SetAttribute(js, "Uint32", _WebView.Factory.CreateInt(67));
                    await Task.Delay(200);
                    datacontext.Uint32.Should().Be(67);

                    SetAttribute(js, "Int16", _WebView.Factory.CreateInt(-23));
                    await Task.Delay(200);
                    datacontext.Int16.Should().Be(-23);

                    SetAttribute(js, "Uint16", _WebView.Factory.CreateInt(9));
                    await Task.Delay(200);
                    datacontext.Uint16.Should().Be(9);

                    SetAttribute(js, "Float", _WebView.Factory.CreateDouble(888.78));
                    await Task.Delay(200);
                    datacontext.Float.Should().Be(888.78f);

                    SetAttribute(js, "Double", _WebView.Factory.CreateDouble(866.76));
                    await Task.Delay(200);
                    datacontext.Double.Should().Be(866.76);

                    SetAttribute(js, "Decimal", _WebView.Factory.CreateDouble(0.5));
                    await Task.Delay(200);
                    datacontext.Decimal.Should().Be(0.5m);

                    SetAttribute(js, "Byte", _WebView.Factory.CreateInt(10));
                    await Task.Delay(200);
                    datacontext.Byte.Should().Be(10);

                    SetAttribute(js, "Sbyte", _WebView.Factory.CreateInt(-12));
                    await Task.Delay(200);
                    datacontext.Sbyte.Should().Be(-12);
                }
            };

            await RunAsync(test);
        }

        private class VMwithlong : ViewModelTestBase
        {
            private long _LongValue;
            public long longValue
            {
                get => _LongValue;
                set => Set(ref _LongValue, value);
            }
        }

        [Fact]
        public async Task TwoWay_Long_ShouldOK()
        {
            var datacontext = new VMwithlong() { longValue = 45 };

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var doublev = GetDoubleAttribute(js, "longValue");
                    doublev.Should().Be(45);

                    var intValue = 24524;
                    var jsInt = Create(() => _WebView.Factory.CreateInt(intValue));
                    SetAttribute(js, "longValue", jsInt);
                    await Task.Delay(100);

                    datacontext.longValue.Should().Be(24524);

                    doublev = GetDoubleAttribute(js, "longValue");
                    long half = 24524;
                    doublev.Should().Be(half);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task StringBinding()
        {
            var datacontext = new VMWithList<decimal>();

            var test = new TestInContext()
            {
                Bind = (win) => Neutronium.Core.StringBinding.Bind(win, "{\"LastName\":\"Desmaisons\",\"Name\":\"O Monstro\",\"BirthDay\":\"0001-01-01T00:00:00.000Z\",\"PersonalState\":\"Married\",\"Age\":0,\"Local\":{\"City\":\"Florianopolis\",\"Region\":\"SC\"},\"MainSkill\":{},\"States\":[\"Single\",\"Married\",\"Divorced\"],\"Skills\":[{\"Type\":\"French\",\"Name\":\"Langage\"},{\"Type\":\"C++\",\"Name\":\"Info\"}]}"),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;

                    mb.Root.Should().BeNull();

                    mb.Context.Should().NotBeNull();

                    string res = GetStringAttribute(js, "Name");
                    res.Should().Be("O Monstro");

                    string res2 = GetStringAttribute(js, "LastName");
                    res2.Should().Be("Desmaisons");

                    string res4 = GetLocalCity(js);
                    res4.Should().Be("Florianopolis");

                    var res5 = GetFirstSkillName(js);
                    res5.Should().Be("Langage");
                }
            };
            await RunAsync(test);
        }

        [Fact]
        public async Task HTML_Without_Correct_js_ShouldThrowException_2()
        {
            using (Tester(TestContext.AlmostEmpty))
            {
                var vm = new object();
                NeutroniumException ex = null;

                try
                {
                    await HtmlBinding.Bind(_ViewEngine, vm, JavascriptBindingMode.OneTime);
                }
                catch (NeutroniumException myex)
                {
                    ex = myex;
                }

                ex.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task TwoWay_rebinds_with_updated_objects()
        {
            var child = new BasicTestViewModel();
            var datacontext = new BasicTestViewModel { Child = child };

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    DoSafeUI(() => datacontext.Child = null);

                    await Task.Delay(300);

                    var third = new BasicTestViewModel();
                    child.Child = third;

                    DoSafeUI(() => datacontext.Child = child);

                    await Task.Delay(300);

                    var child1 = GetAttribute(js, "Child");
                    var child2 = GetAttribute(child1, "Child");

                    var value = GetIntAttribute(child2, "Value");
                    value.Should().Be(-1);
                }
            };

            await RunAsync(test);
        }

        [Theory, AutoData]
        public async Task Binding_manages_updates_ocurring_during_binding(int value)
        {
            var datacontext = new Person()
            {
                Name = "O Monstro",
                LastName = "Desmaisons",
                Local = new Local() { City = "Florianopolis", Region = "SC" },
                PersonalState = PersonalState.Married
            };
            datacontext.Skills.Add(new Skill() { Name = "Langage", Type = "French" });
            datacontext.Skills.Add(new Skill() { Name = "Info", Type = "C++" });
            var dispatcher = WpfThread.GetWpfThread().Dispatcher;
            var timer = new DispatcherTimer(DispatcherPriority.Send, dispatcher) { Interval = TimeSpan.FromMilliseconds(2) };

            void OnTimerOnTick(object o, EventArgs e)
            {
                datacontext.Count = value;
                timer.Tick -= OnTimerOnTick;
                timer.Stop();
            }

            timer.Tick += OnTimerOnTick;


            var test = new TestInContextAsync()
            {
                Bind = (win) =>
                {
                    timer.Start();
                    return Bind(win, datacontext, JavascriptBindingMode.TwoWay);
                },
                Test = async (mb) =>
                {
                    await Task.Delay(200);

                    var js = mb.JsRootObject;
                    var countJs = GetIntAttribute(js, "Count");
                    countJs.Should().Be(value);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task Binding_updates_to_last_value()
        {
            var datacontext = new Person();
            var lastValue = 2;

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    DoSafeUI(() =>
                    {
                        datacontext.Count = -1;
                        datacontext.Count = 1560;
                        datacontext.Count = 26;
                        datacontext.Count = lastValue;
                    });

                    await Task.Delay(200);

                    var js = mb.JsRootObject;
                    var countJs = GetIntAttribute(js, "Count");
                    countJs.Should().Be(lastValue);
                }
            };

            await RunAsync(test);
        }

        private class SmartVM : ViewModelTestBase
        {
            private int _MagicNumber;
            public int MagicNumber
            {
                get => _MagicNumber;
                set
                {
                    if (value == 9)
                    {
                        value = 42;
                    }
                    Set(ref _MagicNumber, value);
                }
            }
        }

        [Fact]
        public async Task TwoWay_RespectsPropertyValidation()
        {
            var datacontext = new SmartVM
            {
                MagicNumber = 8
            };

            var test = new TestInContextAsync()
            {
                Bind = (win) => HtmlBinding.Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    var res = GetIntAttribute(js, "MagicNumber");
                    res.Should().Be(8);

                    var intValue = 9;
                    var jsInt = Create(() => _WebView.Factory.CreateInt(intValue));
                    SetAttribute(js, "MagicNumber", jsInt);
                    await Task.Delay(100);

                    datacontext.MagicNumber.Should().Be(42);
                    await Task.Delay(100);

                    res = GetIntAttribute(js, "MagicNumber");
                    res.Should().Be(42);
                }
            };

            await RunAsync(test);
        }
    }
}

