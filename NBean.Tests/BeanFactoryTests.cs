﻿using System;
using Xunit;

using NBean.Interfaces;
using NBean.Exceptions;

namespace NBean.Tests {

    public class BeanFactoryTests {

        [Fact]
        public void Dispense_ValidateGetColumns_Test() {
            IBeanFactory factory = new BeanFactory();
            object one;
            object two;
            Bean bean;

            Func<bool, Bean> make = validateColumns => {
                factory.Options.ValidateGetColumns = false;
                Bean b = factory.Dispense("test");
                Assert.Equal(typeof(Bean), b.GetType());
                Assert.False(b.ValidateGetColumns);
                b.Put("one", 1);
                return b;
            };

            // With ValidateGetColumns switched off
            bean = make(false);
            one = (int)bean["one"];
            Assert.Equal(1, one);
            one = bean.Get<int>("one");
            Assert.Equal(1, one);
            two = bean.Get<int>("two");
            Assert.Equal(0, two);
            two = bean["two"];
            Assert.Null(two);

            // With ValidateGetColumns switched on
            bean = make(true);
            one = (int)bean["one"];
            Assert.Equal(1, one);
            one = bean.Get<int>("one");
            Assert.Equal(1, one);
            try {
                two = bean["two"];
            } catch (Exception e) {
                Assert.IsType<ColumnNotFoundException>(e);
            }
            try {
                two = bean.Get<int>("two");
            } catch (Exception e) {
                Assert.IsType<ColumnNotFoundException>(e);
            }
        }

    }

}
