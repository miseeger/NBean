using System;
using System.Collections.Generic;
using System.Linq;
using NBean.Models;
using NBean.Plugins;
using Xunit;

namespace NBean.Tests
{
    using BeanRuleList = List<BeanRule>;

    public class ValidatorTests
    {

        [Fact]
        public void CreatesValidatorAndGetsRules()
        {
            var validator = new Validator();

            Assert.Empty(validator.GetRules());

            validator = new Validator(new Dictionary<string, BeanRuleList>()
            {
                {
                    "TestBean", new BeanRuleList()
                    {
                        new BeanRule()
                        {
                            Sequence = 10,
                            Test = (b) => true,
                            Message = "You shall always pass!"
                        }
                    }
                }
            });

            Assert.Single(validator.GetRules());
            Assert.Single(validator.GetRules("TestBean"));
            Assert.Empty(validator.GetRules("ToastBean"));
        }

        [Fact]
        public void AddsRules()
        {
            var validator = new Validator();

            validator.AddRule("TestBean",
                new BeanRule()
                {
                    Sequence = 10,
                    Test = (b) => true,
                    Message = "You shall always pass!"
                }
            );

            validator.AddRule("TestBean",
                new BeanRule()
                {
                    Sequence = 20,
                    Test = (b) => true,
                    Message = "You shall always pass!"
                }
            );

            Assert.Equal(2, validator.GetRules("TestBean").Count);

            validator.AddRules("TestBean2",
                new BeanRuleList()
                {
                    new BeanRule()
                    {
                        Sequence = 10,
                        Test = (b) => false,
                        Message = "You shall not pass!"
                    },
                    new BeanRule()
                    {
                        Sequence = 20,
                        Test = (b) => true,
                        Message = "You shall always pass!"
                    }
                }
            );

            validator.AddRule("TestBean2",
                new BeanRule()
                {
                    Sequence = 30,
                    Test = (b) => true,
                    Message = "You shall always pass!"
                }
            );

            Assert.Equal(2, validator.GetRules("TestBean").Count);
            Assert.Equal(3, validator.GetRules("TestBean2").Count);
        }


        [Fact]
        public void ClearRules()
        {
            var validator = new Validator();

            validator.AddRules("TestBean",
                new BeanRuleList()
                {
                    new BeanRule()
                    {
                        Sequence = 10,
                        Test = (b) => false,
                        Message = "You shall not pass!"
                    },
                    new BeanRule()
                    {
                        Sequence = 20,
                        Test = (b) => true,
                        Message = "You shall always pass!"
                    }
                }
            );

            validator.AddRules("TestBean2",
                new BeanRuleList()
                {
                    new BeanRule()
                    {
                        Sequence = 10,
                        Test = (b) => false,
                        Message = "You shall not pass!"
                    },
                    new BeanRule()
                    {
                        Sequence = 20,
                        Test = (b) => true,
                        Message = "You shall always pass!"
                    }
                }
            );

            validator.ClearRules("TestBean");
            validator.ClearAll();

            Assert.Empty(validator.GetRules("TestBean"));
            Assert.Empty(validator.GetRules());
        }


        [Fact]
        public void Validates()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                var validator = new Validator();

                validator.AddRules("TestBean",
                    new BeanRuleList()
                    {
                        new BeanRule()
                        {
                            Sequence = 10,
                            Test = (b) => b.Get<string>("Name").Length <= 16,
                            Message = "Name is too long (max. 16 characters)."
                        },
                        new BeanRule()
                        {
                            Sequence = 20,
                            Test = (b) => b.Get<long>("Value") >= 18 && b.Get<long>("Value") <= 66,
                            Message = "Value must be between 18 and 66."
                        }
                    }
                );

                var testBean = api.Dispense("TestBean")
                    .Put("Name", "This is my veeeery long name")
                    .Put("Value", 42);

                var (result, message) = validator.Validate(testBean);

                Assert.False(result);
                Assert.Equal("Name is too long (max. 16 characters).\r\n", message);
            }
        }

    }

}
