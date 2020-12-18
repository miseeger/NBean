using System;
using Xunit;

namespace NBean.Tests
{
    public class StringExtensionTests
    {

        [Fact]
        public void TestsNumeric()
        {
            AssertExtensions.WithCulture("en-US", () =>
            {
                Assert.True("12345".IsNumeric());
                Assert.True("12345678901234567890".IsNumeric());
                Assert.True("1234.56".IsNumeric());
                Assert.True("1234.567890123".IsNumeric());
                Assert.False("12345678x9".IsNumeric());
                Assert.False("123x9.9".IsNumeric());
                Assert.False("12,3456".IsNumeric());
            });

            AssertExtensions.WithCulture("de-DE", () =>
            {
                Assert.True("12345".IsNumeric());
                Assert.True("12345678901234567890".IsNumeric());
                Assert.True("1234,56".IsNumeric());
                Assert.True("1234,567890123".IsNumeric());
                Assert.False("12345678x9".IsNumeric());
                Assert.False("123x9,9".IsNumeric());
                Assert.False("12.3456".IsNumeric());
            });
        }


        [Fact]
        public void TestQuotedString()
        {
            Assert.True("'QuotedString'".IsQuotedString());
            Assert.False("NotQuotedString'".IsQuotedString());
            Assert.False("NotQuotedString".IsQuotedString());
            Assert.False("123456".IsQuotedString());
        }


        [Fact]
        public void TestQuotedDateTime()
        {
            AssertExtensions.WithCulture("en-US", () =>
            {
                Assert.True("'2020-12-14 09:00:00'".IsQuotedDateTime());
                Assert.True("'12/14/2020'".IsQuotedDateTime());
                Assert.False("2020-12-14 09:00:00'".IsQuotedDateTime());
                Assert.False("12/14/2020".IsQuotedDateTime());
                Assert.False("NotQuotedString".IsQuotedDateTime());
                Assert.False("123456'".IsQuotedDateTime());
            });

            AssertExtensions.WithCulture("de-DE", () =>
            {
                Assert.True("'2020-12-14 09:00:00'".IsQuotedDateTime());
                Assert.True("'14.12.2020'".IsQuotedDateTime());
                Assert.False("2020-12-14 09:00:00'".IsQuotedDateTime());
                Assert.False("14.12.2020".IsQuotedDateTime());
                Assert.False("NotQuotedString".IsQuotedDateTime());
                Assert.False("123456'".IsQuotedDateTime());
            });
        }


        [Fact]
        public void GetsTypeAndValue()
        {
            AssertExtensions.WithCulture("en-US", () =>
            {
                Assert.True("1".GetTypeAndValue().Item2 is int);
                Assert.True("242".GetTypeAndValue().Item2 is int);
                Assert.True("-121".GetTypeAndValue().Item2 is int);
                Assert.True("640000".GetTypeAndValue().Item2 is int);
                Assert.True("-12121".GetTypeAndValue().Item2 is int);
                Assert.True("-2121212121".GetTypeAndValue().Item2 is int);
                Assert.True("3294967295".GetTypeAndValue().Item2 is uint);
                Assert.True("12121212121212121212".GetTypeAndValue().Item2 is ulong);
                Assert.True("-1212121212121212121".GetTypeAndValue().Item2 is long);
                Assert.True("-12.12345".GetTypeAndValue().Item2 is float);
                Assert.Equal(-12.12345f, "-12.12345".GetTypeAndValue().Item2);
                Assert.True("12.12345".GetTypeAndValue().Item2 is float);
                Assert.Equal(12.12345f, "12.12345".GetTypeAndValue().Item2);
                Assert.True("-12.1234567890123".GetTypeAndValue().Item2 is double);
                Assert.Equal(-12.1234567890123d, "-12.1234567890123".GetTypeAndValue().Item2);
                Assert.True("12.1234567890123".GetTypeAndValue().Item2 is double);
                Assert.Equal(12.1234567890123d, "12.1234567890123".GetTypeAndValue().Item2);
                Assert.True("-12.12345678901234567890123456789".GetTypeAndValue().Item2 is decimal);
                Assert.Equal(-12.123456789012345678901234568m, "-12.12345678901234567890123456789".GetTypeAndValue().Item2);
                Assert.True("12.12345678901234567890123456789".GetTypeAndValue().Item2 is decimal);
                Assert.Equal(12.123456789012345678901234568m, "12.12345678901234567890123456789".GetTypeAndValue().Item2);
                Assert.True("2020-12-14 14:14:14".GetTypeAndValue().Item2 is DateTime);
                Assert.True("12/14/2020".GetTypeAndValue().Item2 is DateTime);
                Assert.True("12,2020".GetTypeAndValue().Item2 is DateTime);
                Assert.True("This is a string!".GetTypeAndValue().Item2 is string);
            });

            AssertExtensions.WithCulture("de-DE", () =>
            {
                Assert.True("1".GetTypeAndValue().Item2 is int);
                Assert.True("242".GetTypeAndValue().Item2 is int);
                Assert.True("-121".GetTypeAndValue().Item2 is int);
                Assert.True("640000".GetTypeAndValue().Item2 is int);
                Assert.True("-12121".GetTypeAndValue().Item2 is int);
                Assert.True("-2121212121".GetTypeAndValue().Item2 is int);
                Assert.True("3294967295".GetTypeAndValue().Item2 is uint);
                Assert.True("12121212121212121212".GetTypeAndValue().Item2 is ulong);
                Assert.True("-1212121212121212121".GetTypeAndValue().Item2 is long);
                Assert.True("-12,12345".GetTypeAndValue().Item2 is float);
                Assert.Equal(-12.12345f, "-12,12345".GetTypeAndValue().Item2);
                Assert.True("12,12345".GetTypeAndValue().Item2 is float);
                Assert.Equal(12.12345f, "12,12345".GetTypeAndValue().Item2);
                Assert.True("-12,1234567890123".GetTypeAndValue().Item2 is double);
                Assert.Equal(-12.1234567890123d, "-12,1234567890123".GetTypeAndValue().Item2);
                Assert.True("12,1234567890123".GetTypeAndValue().Item2 is double);
                Assert.Equal(12.1234567890123d, "12,1234567890123".GetTypeAndValue().Item2);
                Assert.True("-12,12345678901234567890123456789".GetTypeAndValue().Item2 is decimal);
                Assert.Equal(-12.123456789012345678901234568m, "-12,12345678901234567890123456789".GetTypeAndValue().Item2);
                Assert.True("12,12345678901234567890123456789".GetTypeAndValue().Item2 is decimal);
                Assert.Equal(12.123456789012345678901234568m, "12,12345678901234567890123456789".GetTypeAndValue().Item2);
                Assert.True("2020-12-14 14:14:14".GetTypeAndValue().Item2 is DateTime);
                Assert.True("14.12.2020".GetTypeAndValue().Item2 is DateTime);
                Assert.True("12.2020".GetTypeAndValue().Item2 is DateTime);
                Assert.True("This is a string!".GetTypeAndValue().Item2 is string);
            });
        }

        [Fact]
        public void GetsTypeAndValueEx()
        {
            AssertExtensions.WithCulture("en-US", () =>
            {
                Assert.True("242".GetTypeAndValueEx().Item2 is byte);
                Assert.True("-121".GetTypeAndValueEx().Item2 is sbyte);
                Assert.True("64000".GetTypeAndValueEx().Item2 is ushort);
                Assert.True("-12121".GetTypeAndValueEx().Item2 is short);
                Assert.True("3294967295".GetTypeAndValueEx().Item2 is uint);
                Assert.True("-2121212121".GetTypeAndValueEx().Item2 is int);
                Assert.True("12121212121212121212".GetTypeAndValueEx().Item2 is ulong);
                Assert.True("-1212121212121212121".GetTypeAndValueEx().Item2 is long);
                Assert.True("-12.12345".GetTypeAndValueEx().Item2 is float);
                Assert.Equal(-12.12345f, "-12.12345".GetTypeAndValueEx().Item2);
                Assert.True("12.12345".GetTypeAndValueEx().Item2 is float);
                Assert.Equal(12.12345f, "12.12345".GetTypeAndValueEx().Item2);
                Assert.True("-12.1234567890123".GetTypeAndValueEx().Item2 is double);
                Assert.Equal(-12.1234567890123d, "-12.1234567890123".GetTypeAndValueEx().Item2);
                Assert.True("12.1234567890123".GetTypeAndValueEx().Item2 is double);
                Assert.Equal(12.1234567890123d, "12.1234567890123".GetTypeAndValueEx().Item2);
                Assert.True("-12.12345678901234567890123456789".GetTypeAndValueEx().Item2 is decimal);
                Assert.Equal(-12.123456789012345678901234568m, "-12.12345678901234567890123456789".GetTypeAndValueEx().Item2);
                Assert.True("12.12345678901234567890123456789".GetTypeAndValueEx().Item2 is decimal);
                Assert.Equal(12.123456789012345678901234568m, "12.12345678901234567890123456789".GetTypeAndValueEx().Item2);
                Assert.True("2020-12-14 14:14:14".GetTypeAndValueEx().Item2 is DateTime);
                Assert.True("12/14/2020".GetTypeAndValueEx().Item2 is DateTime);
                Assert.True("12,2020".GetTypeAndValueEx().Item2 is DateTime);
                Assert.True("This is a string!".GetTypeAndValueEx().Item2 is string);
            });

            AssertExtensions.WithCulture("de-DE", () =>
            {
                Assert.True("242".GetTypeAndValueEx().Item2 is byte);
                Assert.True("-121".GetTypeAndValueEx().Item2 is sbyte);
                Assert.True("64000".GetTypeAndValueEx().Item2 is ushort);
                Assert.True("-12121".GetTypeAndValueEx().Item2 is short);
                Assert.True("3294967295".GetTypeAndValueEx().Item2 is uint);
                Assert.True("-2121212121".GetTypeAndValueEx().Item2 is int);
                Assert.True("12121212121212121212".GetTypeAndValueEx().Item2 is ulong);
                Assert.True("-1212121212121212121".GetTypeAndValueEx().Item2 is long);
                Assert.True("-12,12345".GetTypeAndValueEx().Item2 is float);
                Assert.Equal(-12.12345f, "-12,12345".GetTypeAndValueEx().Item2);
                Assert.True("12,12345".GetTypeAndValueEx().Item2 is float);
                Assert.Equal(12.12345f, "12,12345".GetTypeAndValueEx().Item2);
                Assert.True("-12,1234567890123".GetTypeAndValueEx().Item2 is double);
                Assert.Equal(-12.1234567890123d, "-12,1234567890123".GetTypeAndValueEx().Item2);
                Assert.True("12,1234567890123".GetTypeAndValueEx().Item2 is double);
                Assert.Equal(12.1234567890123d, "12,1234567890123".GetTypeAndValueEx().Item2);
                Assert.True("-12,12345678901234567890123456789".GetTypeAndValueEx().Item2 is decimal);
                Assert.Equal(-12.123456789012345678901234568m, "-12,12345678901234567890123456789".GetTypeAndValueEx().Item2);
                Assert.True("12,12345678901234567890123456789".GetTypeAndValueEx().Item2 is decimal);
                Assert.Equal(12.123456789012345678901234568m, "12,12345678901234567890123456789".GetTypeAndValueEx().Item2);
                Assert.True("2020-12-14 14:14:14".GetTypeAndValueEx().Item2 is DateTime);
                Assert.True("14.12.2020".GetTypeAndValueEx().Item2 is DateTime);
                Assert.True("12.2020".GetTypeAndValueEx().Item2 is DateTime);
                Assert.True("This is a string!".GetTypeAndValueEx().Item2 is string);
            });
        }

    }

}
