using System;
using System.Text;
using System.Text.RegularExpressions;
using NBean.Enums;

namespace NBean.Plugins
{

    public class SlxStyleKeyProvider : BeanObserver
    {
        private readonly string _defaultKey;


        public SlxStyleKeyProvider(BeanApi api, string defaultKey = "")
        {
            _defaultKey = defaultKey == string.Empty ? "Id" : defaultKey;
            api?.ReplaceAutoIncrement(_defaultKey);
        }


        internal string GetKeyPrefix(string kind)
        {
            if (kind.Length <= 5) 
                return kind.ToUpper().PadRight(5, '#');
            
            var shortened = Regex.Replace(kind.ToUpper(), @"[AEIOU]", string.Empty).ToUpper();

            return shortened.Length <= 5 
                ? shortened.PadRight(5, '#') 
                : shortened.Substring(0, 5);
        }


        internal string GetInitialKey(string kind)
        {
            return $"{GetKeyPrefix(kind)}-A000000000";
        }


        internal string GetNextKey(string key)
        {

            var keyPrefix = key.Substring(0, 6);
            var keyCounter = key.Substring(6, 10);

            if (keyCounter == "ZZZZZZZZZZ")
                throw new ArgumentException("Maximum key counter reached!");

            var counterRestLength = 0;
            
            var sb = new StringBuilder();

            for (var i = 9; i >= 0; i--)
            {
                if (keyCounter[i] == 'Z')
                {
                    sb.Insert(0, "0");
                }
                else if (keyCounter[i] == '9')
                {
                    sb.Insert(0, "A");
                    counterRestLength = i;
                    break;
                }
                else
                {
                    sb.Insert(0, (char)(keyCounter[i] + 1));
                    counterRestLength = i;
                    break;
                }
            }

            var keyCounterRest = counterRestLength > 0 
                ? keyCounter.Substring(0, counterRestLength) 
                : string.Empty;

            return $"{keyPrefix}{keyCounterRest}{sb}";
        }


        public override void BeforeInsert(Bean bean)
        {
            var api = bean.Api;
            var kind = bean.GetKind();

            if (!api.IsKnownKindColumn(kind, _defaultKey))
                return;

            // MariaDb => RANK_TEXT_16 = 5
            // MsSql   => RANK_TEXT_16 = 5
            // PgSql   => RANK_TEXT = 5
            if (api.GetRankOfKindColumn(kind, _defaultKey) != 5 && api.DbType != DatabaseType.Sqlite)
                return;

            var lastKey = bean.Api.Cell<string>(false, $"SELECT MAX(Id) FROM {kind}");
            bean[_defaultKey] = lastKey == null ? GetInitialKey(kind) : GetNextKey(lastKey);
        }
    }

}