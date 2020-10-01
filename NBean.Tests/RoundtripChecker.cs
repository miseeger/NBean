using System.Collections.Generic;
using Xunit;

using NBean.Interfaces;

namespace NBean.Tests {

    class RoundtripChecker {
        IDatabaseAccess _db;
        DatabaseStorage _storage;

        public RoundtripChecker(IDatabaseAccess db, DatabaseStorage storage) {
            _db = db;
            _storage = storage;
        }

        public void Check(object before, object after) {
            var id = _storage.Store("foo", new Dictionary<string, object> { 
                    { "p", before }
                });

            try {
                var loaded = _storage.Load("foo", id);
                Assert.Equal(after, loaded.GetSafe("p"));

                if(after != null)
                    Assert.Equal(after.GetType(), loaded["p"].GetType());
            } finally {
                _db.Exec("drop table foo");
                _storage.InvalidateSchema();            
            }
        }

    }
}
