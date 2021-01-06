using System.Collections.Generic;
using NBean.Exceptions;
using System.Linq;
using Xunit;

namespace NBean.Tests
{

    public class MapsterTests
    {
        private readonly BeanApi _api;

        public MapsterTests()
        {
            _api = SQLitePortability.CreateApi();
        }


        [Fact]
        public void MapsBeanToPoco()
        {
            var bean = new Bean
            {
                ["Id"] = 123,
                ["A"] = 1,
                ["B"] = "abc"
            };

            var poco = bean.ToPoco<PocoBean>();

            Assert.Equal(poco.Id, bean["Id"]);
            Assert.Equal(poco.A, bean["A"]);
            Assert.Equal(poco.B, bean["B"]);
        }

        
        [Fact]
        public void MapsBeanListToPocoList()
        {
            var beans = new List<Bean>
            {
                new Bean
                {
                    ["Id"] = 123,
                    ["A"] = 1,
                    ["B"] = "abc"
                },
                new Bean
                {
                    ["Id"] = 124,
                    ["A"] = 2,
                    ["B"] = "def"
                },
                new Bean
                {
                    ["Id"] = 125,
                    ["A"] = 3,
                    ["B"] = "ghi"
                }
            };

            var pocoList = beans.ToPoco<PocoBean>().ToArray();

            Assert.Equal(3, pocoList.Count());
            Assert.Equal(123, pocoList[0].Id);
            Assert.Equal(2, pocoList[1].A);
            Assert.Equal("ghi", pocoList[2].B);
        }


        [Fact]
        public void MapsPocoToBean()
        {
            var pocoBean = new PocoBean()
            {
                Id = 123,
                A = 1,
                B = "abc"
            };

            var bean = pocoBean.ToBean("PocoBean");

            Assert.Equal(bean["Id"], pocoBean.Id);
            Assert.Equal(bean["A"], pocoBean.A);
            Assert.Equal(bean["B"], pocoBean.B);
        }


        [Fact]
        public void MapsPocoListToBeanList()
        {
            var pocoBeans = new List<PocoBean>
            {
                new PocoBean
                {
                    Id = 123,
                    A = 1,
                    B = "abc"
                },
                new PocoBean
                {
                    Id = 124,
                    A = 2,
                    B = "def"
                },
                new PocoBean
                {
                    Id = 125,
                    A = 3,
                    B = "ghi"
                }
            };

            var beans = pocoBeans.ToBeanList("PocoBean").ToArray();

            Assert.Equal(3, beans.Count());
            Assert.Equal("PocoBean", beans[0].GetKind());
            Assert.Equal(123, beans[0]["Id"]);
            Assert.Equal(2, beans[1]["A"]);
            Assert.Equal("ghi", beans[2]["B"]);
        }


        [Fact]
        public void ThrowsCannotMapIEnumerableException()
        {
            var pocoBeans = new List<PocoBean>
            {
                new PocoBean
                {
                    Id = 123,
                    A = 1,
                    B = "abc"
                }
            };

            Assert.Throws<CannotMapIEnumerableException>(() => 
                pocoBeans.ToBean("PocoBean"));

            Assert.Throws<CannotMapIEnumerableException>(() =>
                pocoBeans.ToArray().ToBean("PocoBean"));
        }

    }


    public class PocoBean
    {
        public int Id { get; set; }
        public int A  { get; set; }
        public string B { get; set; }   
    }

}
