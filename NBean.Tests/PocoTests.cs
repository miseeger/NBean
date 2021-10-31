using System.Collections.Generic;
using NBean.Exceptions;
using NBean.Poco;
using System.Linq;
using Xunit;

namespace NBean.Tests
{

    public class PocoTests
    {
        private readonly BeanApi _api;

        private readonly Bean _bean;
        private readonly IEnumerable<Bean> _beans;

        private readonly PocoBean _pocoBean;
        private readonly IEnumerable<PocoBean> _pocoBeans;


        public PocoTests()
        {
            _api = SQLitePortability.CreateApi();

            _bean = _api.Dispense("Bean")
                .Put("Id", 123)
                .Put("A", 1)
                .Put("B", "abc");

            _beans = new List<Bean>
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


            _pocoBean = new PocoBean()
            {
                Id = 123,
                A = 1,
                B = "abc"
            };

            _pocoBeans = new List<PocoBean>
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

        }


        [Fact]
        public void MapsDictionaryToPoco()
        {
            var dict = new Dictionary<string, object>
            {
                ["Id"] = 123,
                ["A"] = 1,
                ["B"] = "abc"
            };

            var poco = dict.ToPoco<PocoBean>();

            Assert.Equal(poco.Id, _bean["Id"]);
            Assert.Equal(poco.A, _bean["A"]);
            Assert.Equal(poco.B, _bean["B"]);
        }


        [Fact]
        public void MapsBeanToPoco()
        {
            var poco = _bean.ToPoco<PocoBean>();

            Assert.Equal(poco.Id, _bean["Id"]);
            Assert.Equal(poco.A, _bean["A"]);
            Assert.Equal(poco.B, _bean["B"]);
        }


        [Fact]
        public void MapsBeanToPocoWithIgnoreList()
        {
            var poco = _bean.ToPoco<PocoBean>("A,B");

            Assert.Equal(poco.Id, _bean["Id"]);
            Assert.Equal(default, poco.A);
            Assert.Equal(default, poco.B);
        }


        [Fact]
        public void MapsBeanToPocoWithLessProperties()
        {
            var poco = _bean.ToPoco<PocoBean>();

            Assert.Equal(poco.Id, _bean["Id"]);
            Assert.Equal(poco.A, _bean["A"]);
            Assert.Equal(poco.B, _bean["B"]);
        }


        [Fact]
        public void MapsBeanListToPocoList()
        {
            var pocoList = _beans
                .ToPocoList<PocoBean>()
                .ToArray();

            Assert.Equal(3, pocoList.Count());
            Assert.Equal(123, pocoList[0].Id);
            Assert.Equal(2, pocoList[1].A);
            Assert.Equal("ghi", pocoList[2].B);
        }


        [Fact]
        public void MapsBeanListToPocoListWithIgnoreList()
        {
            var pocoList = _beans
                .ToPocoList<PocoBean>("Id")
                .ToArray();

            Assert.Equal(3, pocoList.Count());
            Assert.Null(pocoList[0].Id);
            Assert.Equal(2, pocoList[1].A);
            Assert.Equal("ghi", pocoList[2].B);
        }


        [Fact]
        public void MapsBeanDataToPocoList()
        {
            var pocoList = _beans
                .Export()
                .ToPocoList<PocoBean>()
                .ToArray();

            Assert.Equal(3, pocoList.Count());
            Assert.Equal(123, pocoList[0].Id);
            Assert.Equal(2, pocoList[1].A);
            Assert.Equal("ghi", pocoList[2].B);
        }


        [Fact]
        public void MapsPocoToBean()
        {
            var bean = _pocoBean.ToBean("PocoBean");

            Assert.Equal(bean["Id"], _pocoBean.Id);
            Assert.Equal(bean["A"], _pocoBean.A);
            Assert.Equal(bean["B"], _pocoBean.B);
            Assert.Equal("Id,A,B", string.Join(',', bean.GetDirtyNames()));
        }


        [Fact]
        public void ImportsPocoIntoExistingBeanAndIgnoringNullValues()
        {
            var bean = _bean.Copy();

            bean.ImportPoco(new PocoBean
            {
                Id = 456,
                B = "changed"
            });

            Assert.Equal(456, bean["Id"]);
            Assert.Equal(_bean["A"], bean["A"]);
            Assert.Equal("changed", bean["B"]);
            Assert.Equal("Id,A,B", string.Join(',', bean.GetDirtyNames()));
        }


        [Fact]
        public void MapsPocoListToBeanList()
        {
            var beans = _pocoBeans.ToBeanList("PocoBean").ToArray();

            Assert.Equal(3, beans.Count());
            Assert.Equal("PocoBean", beans[0].GetKind());
            Assert.Equal(123, beans[0]["Id"]);
            Assert.Equal(2, beans[1]["A"]);
            Assert.Equal("ghi", beans[2]["B"]);
        }


        [Fact]
        public void ThrowsCannotMapIEnumerableException()
        {
            Assert.Throws<CannotMapIEnumerableException>(() =>
                _pocoBeans.ToBean("PocoBean"));

            Assert.Throws<CannotMapIEnumerableException>(() =>
                _pocoBeans.ToArray().ToBean("PocoBean"));
        }

    }


    public class PocoBean
    {
        public int? Id { get; set; }
        public int? A  { get; set; }
        public string B { get; set; }   
    }

}
