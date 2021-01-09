using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

using NBean.Interfaces;
using NBean.Plugins;

namespace NBean.Tests {

    public class BeanCrudTests {

        [Fact]
        public void Dispense_Default() {
            IBeanFactory factory = new BeanFactory();
            var crud = new BeanCrud(null, null, null, factory);
            var bean = crud.Dispense("test");
            Assert.Equal("test", bean.GetKind());
            Assert.Equal(typeof(Bean), bean.GetType());
        }

        [Fact]
        public void Dispense_Hooks() {
            IBeanFactory factory = new BeanFactory();
            factory.Options.ValidateGetColumns = false;
            var crud = new BeanCrud(null, null, null, factory);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();
            Assert.Equal("tracer", bean.GetKind());

            Assert.Equal("ad:", bean.TraceLog);
            Assert.Equal("ad:", observer.TraceLog);
            Assert.Same(bean, observer.LastBean);
        }

        [Fact]
        public void Store() {
            IBeanFactory factory = new BeanFactory();
            factory.Options.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil(), factory);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();

            // ----- Insert
            var id = crud.Store(bean);
            Assert.Equal(0L, id);
            Assert.Equal(0L, bean["id"]);
            Assert.Equal($"ad: bs: bi: ai:{id} as:{id}", bean.TraceLog);
            Assert.Equal($"ad: bs: bi: ai:{id} as:{id}", observer.TraceLog);

            // ------ Update
            bean.Put("p1", "test");
            observer.TraceLog = "";
            bean.TraceLog = "";
            crud.Store(bean);
            Assert.Equal(0L, bean["id"]);
            Assert.Equal($"bs:{id} bu:{id} au:{id} as:{id}", bean.TraceLog);
            Assert.Equal($"bs:{id} bu:{id} au:{id} as:{id}", observer.TraceLog);
        }


        [Fact]
        public void Load() {
            IBeanFactory factory = new BeanFactory();
            factory.Options.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil(), factory);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            // Load non-existing bean
            Assert.Null(crud.Load("any", 123));
            Assert.Empty(observer.TraceLog);

            var bean = crud.Dispense<Tracer>();
            bean.Put("p1", "test");

            var id = crud.Store(bean);
            observer.TraceLog = "";

            bean = crud.Load<Tracer>(id);
            Assert.Equal("ad: bl: al:" + id, bean.TraceLog);
            Assert.Equal("ad: bl: al:" + id, observer.TraceLog);
            Assert.NotNull(bean["id"]);
            Assert.Equal(id, bean["id"]);
            Assert.Equal("test", bean["p1"]);
        }

        [Fact]
        public void Trash() {
            IBeanFactory factory = new BeanFactory();
            factory.Options.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, new KeyUtil(), factory);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.Dispense<Tracer>();

            observer.TraceLog = bean.TraceLog = "";
            crud.Trash(bean);
            Assert.Empty(bean.TraceLog);
            Assert.Empty(observer.TraceLog);

            var id = crud.Store(bean);

            observer.TraceLog = bean.TraceLog = "";
            crud.Trash(bean);
            Assert.Equal("bt:" + id + " at:" + id, bean.TraceLog);
            Assert.Equal("bt:" + id + " at:" + id, observer.TraceLog);
            Assert.Equal(id, bean["id"]);

            Assert.Null(crud.Load<Tracer>(id));
        }

        [Fact]
        public void RowToBean() {
            IBeanFactory factory = new BeanFactory();
            factory.Options.ValidateGetColumns = false;
            var crud = new BeanCrud(new InMemoryStorage(), null, null, factory);
            var observer = new TracingObserver();
            crud.AddObserver(observer);

            var bean = crud.RowToBean<Tracer>(new Dictionary<string, object> { 
                { "s", "hello" }
            });

            Assert.Null(bean["id"]);
            Assert.Equal("hello", bean["s"]);
            Assert.Equal("ad: bl: al:", bean.TraceLog);
            Assert.Equal("ad: bl: al:", observer.TraceLog);

            observer.TraceLog = "";

            bean = crud.RowToBean<Tracer>(new Dictionary<string, object> { 
                { "id", 123 },
                { "s", "see you" }
            });

            Assert.Equal(123, bean["id"]);
            Assert.Equal("see you", bean["s"]);
            Assert.Equal("ad: bl: al:123", bean.TraceLog);
            Assert.Equal("ad: bl: al:123", observer.TraceLog);

            Assert.Null(crud.Load("temp", null));
        }

        [Fact]
        public void PreventDirectInstantiation() {
            IBeanFactory factory = new BeanFactory();
            var crud = new BeanCrud(null, null, null, factory);
            
            Assert.Throws<InvalidOperationException>(() => {
                crud.Store(new Tracer());    
            });

            Assert.Throws<InvalidOperationException>(() => {
                crud.Trash(new Tracer());
            });
        }

        [Fact]
        public void ConvertsValueToString()
        {
            IBeanFactory factory = new BeanFactory();
            var crud = new BeanCrud(null, null, null, factory);

            var bean = crud.Dispense("foo");
            bean.Import(
                new Dictionary<string, object>()
                {
                    {"null", null},
                    {"bool", true},
                    {"sbyte", sbyte.Parse("123")},
                    {"ssbyte", sbyte.Parse("-123")},
                    {"byte", byte.Parse("123")},
                    {"int", 123},
                    {"long", 123456789L},
                    {"double", 123.4567},
                    {"decimal", 123.45m},
                    {"string", "Hello!"},
                    {"datetime", new DateTime(2000,1,1)},
                    {"guid", Guid.Parse("6161ADAD-72F0-48D1-ACE2-CD98315C9D5B")},
                    {"byte[]", Encoding.UTF8.GetBytes("Hello!")}
                }
            );

            AssertExtensions.WithCulture("de-DE", () =>
            {
                Assert.Equal("#NULL#", bean["null"].FormatValueToString());
                Assert.Equal("true", bean["bool"].FormatValueToString());
                Assert.Equal("123", bean["sbyte"].FormatValueToString());
                Assert.Equal("-123", bean["ssbyte"].FormatValueToString());
                Assert.Equal("123", bean["byte"].FormatValueToString());
                Assert.Equal("123", bean["int"].FormatValueToString());
                Assert.Equal("123456789", bean["long"].FormatValueToString());
                Assert.Equal("123,4567", bean["double"].FormatValueToString());
                Assert.Equal("123,45", bean["decimal"].FormatValueToString());
                Assert.Equal("Hello!", bean["string"].FormatValueToString());
                Assert.Equal("2000-01-01T00:00:00", bean["datetime"].FormatValueToString());
                Assert.Equal("6161ADAD-72F0-48D1-ACE2-CD98315C9D5B", bean["guid"].FormatValueToString());
                Assert.Equal("Hello!", bean["byte[]"].FormatValueToString());
            });
        }

        [Fact]
        public void HandlesObserver()
        {
            var crud = new BeanCrud(new InMemoryStorage(), null, null, null);
            var auditorLight = new AuditorLight();
            var tracer = new TracingObserver();

            crud.AddObserver(auditorLight);
            crud.AddObserver(tracer);
            Assert.True(crud.HasObservers());
            Assert.True(crud.IsObserverLoaded<AuditorLight>());
            Assert.True(crud.IsObserverLoaded<TracingObserver>());

            crud.AddObserver(auditorLight);
            Assert.Equal(auditorLight, crud.GetObserver<AuditorLight>());

            crud.RemoveObserver<TracingObserver>();
            Assert.False(crud.IsObserverLoaded<TracingObserver>());

            Assert.True(crud.HasObservers());
        }


        class Tracer : Bean {

            public Tracer()
                : base("tracer") {
            }

            public string TraceLog = "";

            void Trace(string subject) {
                if(TraceLog.Length > 0)
                    TraceLog += " ";
                TraceLog += subject + ":" + this["id"];
            }

            protected internal override void AfterDispense() {
                Trace("ad");
            }

            protected internal override void BeforeLoad() {
                Trace("bl");
            }

            protected internal override void AfterLoad() {
                Trace("al");
            }

            protected internal override void BeforeStore() {
                Trace("bs");
            }

            protected internal override void BeforeInsert()
            {
                Trace("bi");
            }

            protected internal override void BeforeUpdate()
            {
                Trace("bu");
            }

            protected internal override void AfterStore() {
                Trace("as");
            }

            protected internal override void AfterInsert()
            {
                Trace("ai");
            }

            protected internal override void AfterUpdate()
            {
                Trace("au");
            }

            protected internal override void BeforeTrash() {
                Trace("bt");
            }

            protected internal override void AfterTrash() {
                Trace("at");
            }
        
        }
    }

}
