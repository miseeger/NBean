using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using NBean.Enums;
using NBean.Exceptions;
using NBean.Interfaces;
using NBean.Models;

namespace NBean
{
    public partial class BeanApi : IBeanApi
    {
        private readonly ConnectionContainer _connectionContainer;

        private static object _detailsLock = new object();
        private volatile IDatabaseDetails _details;

        private static object _dbLock = new object();
        private volatile IDatabaseAccess _db;

        private static object _keyUtilLock = new object();
        private volatile KeyUtil _keyUtil;

        private static object _storageLock = new object();
        private volatile DatabaseStorage _storage;

        private static object _crudLock = new object();
        private volatile IBeanCrud _crud;

        private static object _factoryLock = new object();
        private volatile IBeanFactory _factory;

        private static object _finderLock = new object();
        private volatile IBeanFinder _finder;

        private static object _hiveLock = new object();
        private volatile IHive _hive;


        public DbConnection Connection => _connectionContainer.Connection;

        public object CurrentUser
        {
            get => Hive["CurrentUser"];
            set => Hive["CurrentUser"] = value;
        }

        public IBeanOptions BeanOptions => Factory.Options;


        public static List<BeanObserver> InitialObservers { get; set; } = new List<BeanObserver>();

        private readonly IDictionary<string, Action<Bean, object[]>> _beanActions = 
            new Dictionary<string, Action<Bean, object[]>>();
        private readonly IDictionary<string, Action<BeanApi, object[]>> _actions = 
            new Dictionary<string, Action<BeanApi, object[]>>();
        private readonly IDictionary<string, Func<Bean, object[], object>> _beanFunctions = 
            new Dictionary<string, Func<Bean, object[], object>>();
        private readonly IDictionary<string, Func<BeanApi, object[], object>> _functions = 
            new Dictionary<string, Func<BeanApi, object[], object>>();


        // ----- Ctors --------------------------------------------------------

        public BeanApi(string connectionString, DbProviderFactory factory)
        {
            _connectionContainer = new ConnectionContainer.LazyImpl(connectionString, factory.CreateConnection);
        }


        public BeanApi(DbConnection connection)
        {
            _connectionContainer = new ConnectionContainer.SimpleImpl(connection);
        }


        public BeanApi(string connectionString, Type connectionType)
        {
            _connectionContainer = new ConnectionContainer.LazyImpl(connectionString,
                () => (DbConnection)Activator.CreateInstance(connectionType));
        }


        // ----- API-Tools (non-thread-safe Singletons !!!) ---------------------------

        private IDatabaseDetails Details
        {
            get
            {
                if (_details != null)
                    return _details;

                lock (_detailsLock)
                {
                    if (_details == null)
                        _details = CreateDetails();
                }

                return _details;
            }
        }


        private IDatabaseAccess Db
        {
            get
            {
                if (_db != null)
                    return _db;

                lock (_dbLock)
                {
                    if (_db == null)
                        _db = new DatabaseAccess(Connection, Details);
                }

                return _db;
            }
        }


        private KeyUtil KeyUtil
        {
            get
            {
                if (_keyUtil != null)
                    return _keyUtil;

                lock (_keyUtilLock)
                {
                    if (_keyUtil == null)
                        _keyUtil = new KeyUtil();
                }

                return _keyUtil;
            }
        }


        private DatabaseStorage Storage
        {
            get
            {
                if (_storage != null)
                    return _storage;

                lock (_storageLock)
                {
                    if (_storage == null)
                        _storage = new DatabaseStorage(Details, Db, KeyUtil);
                }

                return _storage;
            }
        }


        private IBeanCrud Crud
        {
            get
            {
                if (_crud != null)
                    return _crud;

                lock (_crudLock)
                {
                    if (_crud == null)
                    {
                        _crud = new BeanCrud(Storage, Db, KeyUtil, Factory);
                    }
                }

                if (!_crud.HasObservers())
                {
                    InitializeObservers();
                }

                return _crud;
            }
        }


        private IBeanFactory Factory
        {
            get
            {
                if (_factory != null)
                    return _factory;

                lock (_factoryLock)
                {
                    if (_factory == null)
                    {
                        _factory = new BeanFactory();
                    }
                }

                return _factory;
            }
        }


        private IBeanFinder Finder
        {
            get
            {
                if (_finder != null)
                    return _finder;

                lock (_finderLock)
                {
                    if (_finder == null)
                    {
                        _finder = new DatabaseBeanFinder(Details, Db, Crud);
                    }
                }

                return _finder;
            }
        }


        public IHive Hive
        {
            get
            {
                if (_hive != null)
                    return _hive;

                lock (_hiveLock)
                {
                    if (_hive == null)
                    {
                        _hive = new Hive();
                    }
                }

                return _hive;
            }
        }


        // ----- Helper methods -----------------------------------------------

        private void InitializeObservers()
        {
            AddObserver(new BeanApiLinker(this));

            if (!InitialObservers.Any())
                return;

            foreach (var initialObserver in InitialObservers)
            {
                AddObserver(initialObserver);
            }
        }


        internal IDatabaseDetails CreateDetails()
        {
            switch (Connection.GetType().FullName)
            {
                case "System.Data.SQLite.SQLiteConnection":
                case "Microsoft.Data.Sqlite.SqliteConnection":
                    return new SQLiteDetails();
#if !NO_MARIADB
                case "MySql.Data.MySqlClient.MySqlConnection":
                    return new MariaDbDetails();
#endif
#if !NO_MSSQL
                case "System.Data.SqlClient.SqlConnection":
                    return new MsSqlDetails();
#endif
#if !NO_PGSQL
                case "Npgsql.NpgsqlConnection":
                    return new PgSqlDetails();
#endif
            }

            throw new NotSupportedException();
        }


        // ----- Methods ------------------------------------------------------

        /// <summary>
        /// Dispose of any fully managed Database Connections. 
        /// Connections created outside of BeanAPI and passed in need to be manually disposed
        /// </summary>
        public void Dispose()
        {
            _connectionContainer.Dispose();
        }


        // ----- Fluid Mode
#if !DEBUG
        [Obsolete("Use Fluid Mode in DEBUG mode only!")]
#endif
        /// <summary>
        /// Use LimeBean in 'Fluid Mode' which will auto create missing 
        /// configuration (ie. columns) which you are trying to interact with on the Database
        /// </summary>
        public void EnterFluidMode()
        {
            Storage.EnterFluidMode();
        }


        /// <summary>
        /// Exits LimeBean's 'Fluid Mode'.
        /// </summary>
        public void ExitFluidMode()
        {
            Storage.ExitFluidMode();
        }


        /// <summary>
        /// Returns the FluidMode
        /// </summary>
        /// <returns></returns>
        public bool IsFluidMode()
        {
            return Storage.IsFluidMode();
        }


        // ----- Bean related

        /// <summary>
        /// Creates a fully functional "raw" Bean of the given Kind without
        /// processing the whole
        /// dispense process.
        /// </summary>
        /// <param name="kind">Kind of Bean</param>
        /// <returns></returns>
        public Bean CreateRawBean(string kind)
        {
            return Factory.Dispense(kind);
        }


        /// <summary>
        /// Creates a fully functional "raw" Bean of the given Type without
        /// processing the whole
        /// dispense process.
        /// </summary>
        /// <typeparam name="T">Custom Bean Type</typeparam>
        /// <returns></returns>
        public T CreateRawBean<T>() where T : Bean, new()
        {
            return Factory.Dispense<T>();
        }


        internal string GetLinkName(string kind1, string kind2)
        {
            return GetKinds()
                .OrderBy(k => k)
                .FirstOrDefault(k => k == $"{kind1}{kind2}_link" || k == $"{kind2}{kind1}_link") ?? string.Empty;
        }


        /// <summary>
        /// Gets the name of the non compound key of a bean.
        /// </summary>
        /// <param name="kind"></param>
        /// <returns>Name of the bean's Primary Key.</returns>
        public string GetKeyName(string kind)
        {
            var keyNames = KeyUtil.GetKeyNames(kind);

            return keyNames.Count <= 1 ? keyNames.First() : string.Empty;
        }


        /// <summary>
        /// Gets the names of the compound key of a bean.
        /// </summary>
        /// <param name="kind"></param>
        /// <returns>Name of the Properties that make the bean's compound key.</returns>
        public string GetCompoundKeyNames(string kind)
        {
            var keyNames = KeyUtil.GetKeyNames(kind);

            return keyNames.Count > 1
                ? string.Join(";", keyNames.OrderBy(n => n))
                : string.Empty;
        }


        /// <summary>
        /// Gets the key value of a not compound key. If a compound key is
        /// detected it 
        /// detected
        /// </summary>
        /// <param name="bean"></param>
        /// <returns></returns>
        public object GetNcKeyValue(Bean bean)
        {
            var kind = bean.GetKind();
            var keyNames = KeyUtil.GetKeyNames(kind);

            if (!keyNames.Any())
                return null;

            if (keyNames.Count != 1)
                throw new NotSupportedException();

            return KeyUtil.GetKey(kind, bean.Data);
        }


        /// <summary>
        /// Converts the bean's data to a JSON string. This Method may
        /// recive a props ignorelist which contains all the prop's names
        /// that have to be excluded from the export, to hide confidential
        /// information. the prop's names are comma separated without
        /// any spaces in between. 
        /// </summary>
        /// <param name="bean"></param>
        /// <param name="propsIgnorelist">The comma separated ignorelist of
        /// props (case sensitive)</param>
        /// <param name="toPrettyJson">to get formatted JSON</param>
        /// <returns>JSON string (camelcase).</returns>
        public static string ToJson(Bean bean, string propsIgnorelist = "", bool toPrettyJson = false)
        {
            var jso = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = toPrettyJson
            };

            return JsonSerializer.Serialize(bean.Export(propsIgnorelist), jso);
        }


        /// <summary>
        /// Converts the data of the beans in a Bean listto a JSON string.
        /// This Method may recive a props ignorelist which contains all the
        /// prop's names that have to be excluded from the export, to hide
        /// confidential information. the prop's names are comma separated
        /// without any spaces in between. 
        /// </summary>
        /// <param name="beans"></param>
        /// <param name="propsIgnorelist">The comma separated ignorelist of
        /// props (case sensitive)</param>
        /// <param name="toPrettyJson">to get formatted JSON</param>
        /// <returns>JSON string (camelcase).</returns>
        public static string ToJson(IEnumerable<Bean> beans, string propsIgnorelist = "", bool toPrettyJson = false)
        {
            var jso = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = toPrettyJson
            };

            return JsonSerializer.Serialize(
                beans.Select(bean => bean.Export(propsIgnorelist)).ToList(), jso);
        }


        // ----- Database related

        /// <summary>
        /// Gets the Database field type according to the value-type-mapping of the current
        /// used database.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Database field type.</returns>
        public string GetDbTypeFromValue(object value)
        {
            return Db.GetDbTypeFromValue(value);
        }


        /// <summary>
        /// Retrieves the database type of a Table Column (Bean Property).
        /// </summary>
        /// <param name="tableName">Table-/Bean-Name</param>
        /// <param name="columnName">Column-/Property-Name</param>
        /// <returns></returns>
        public string GetDbTypeOfKindColumn(string tableName, string columnName)
        {
            return Details.GetSqlTypeFromRank(GetRankOfKindColumn(tableName, columnName));
        }


        /// <summary>
        /// Retrieves the internal type rank of a Table Column (Bean Property).
        /// </summary>
        /// <param name="tableName">Table-/Bean-Name</param>
        /// <param name="columnName">Column-/Property-Name</param>
        /// <returns></returns>
        public int GetRankOfKindColumn(string tableName, string columnName)
        {
            return Storage
                .GetColumns(tableName)
                .FirstOrDefault(c => c.Key == columnName)
                .Value;
        }


        /// <summary>
        /// Checks if table / kind exists in database
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public bool IsKnownKind(string kind)
        {
            return Storage.IsKnownKind(kind);
        }


        /// <summary>
        /// Checks if a column of a certain kind/table exists in the database (case sensitive)
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns>true, if column exists in table</returns>
        public bool IsKnownKindColumn(string tableName, string columnName)
        {
            return Storage.GetColumns(tableName).Any(c => c.Key == columnName);
        }


        /// <summary>
        /// Gets all Bean-Kinds (tables) existing in the database.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetKinds()
        {
            return Storage
                .GetTables()
                .OrderBy(t => t)
                .ToList();
        }

        
        /// <summary>
        /// Retrieves all colums of a table (Prpoerties of a Bean) .
        /// </summary>
        /// <param name="tableName">Table-/Bean-Name</param>
        /// <returns></returns>
        public IList<string> GetKindColumns(string tableName)
        {
            return Storage
                .GetColumns(tableName)
                .Keys
                .OrderBy(k => k)
                .ToList();
        }


        /// <summary>
        /// Quotes a string with the quote characters of the current database.
        /// </summary>
        /// <param name="toQuote">String to quote.</param>
        /// <returns></returns>
        public string GetQuoted(string toQuote)
        {
            return Details.QuoteName(toQuote);
        }


        // ----- IBeanCrud ----------------------------------------------------

        /// <summary>
        /// Gets or Sets whether changes to a Bean are tracked per column. Default true. 
        /// When true, only columns which are changed will be updated on Store(). Otherwise all columns are updated
        /// </summary>
        public bool DirtyTracking
        {
            get => Crud.DirtyTracking;
            set => Crud.DirtyTracking = value;
        }


        /// <summary>
        /// Create an empty Bean of a given Kind. All dispensed Beans
        /// get the minimum audit props added if they exist as column
        /// in the underlying database table of the Bean. Keep attention
        /// of case sensitivity, here!
        /// </summary>
        /// <param name="kind">The name of a table to create a Bean for (case sensitive!)</param>
        /// <returns>A Bean representing the requested Kind</returns>
        public Bean Dispense(string kind)
        {
            return Crud.Dispense(kind);
        }


        /// <summary>
        /// Create an empty Bean of a given Bean subclass
        /// </summary>
        /// <typeparam name="T">A subclass of Bean representing a Bean Kind</typeparam>
        /// <returns>A Bean representing the requested Bean Kind</returns>
        public T Dispense<T>() where T : Bean, new()
        {
            return Crud.Dispense<T>();
        }


        /// <summary>
        /// Create a new Bean of a given Kind and populate it with a given data set
        /// </summary>
        /// <param name="kind">The name of a table to create the Bean for</param>
        /// <param name="row">The data to populate the Bean with</param>
        /// <returns>A Bean of the given Kind populated with the given data</returns>
        public Bean RowToBean(string kind, IDictionary<string, object> row)
        {
            return Crud.RowToBean(kind, row);
        }


        /// <summary>
        /// Create a new Bean of a given subclass representing a given Kind, and populate it with a given data set
        /// </summary>
        /// <typeparam name="T">A subclass of Bean representing a Bean Kind</typeparam>
        /// <param name="row">The data to populate the Bean with</param>
        /// <returns>A Bean of the given subclass populated with the given data</returns>
        public T RowToBean<T>(IDictionary<string, object> row) where T : Bean, new()
        {
            return Crud.RowToBean<T>(row);
        }


        /// <summary>
        /// Query a Bean (row) from the Database
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="key">The value of the primary key on the required row</param>
        /// <returns>A new Bean representing the requested row from the database</returns>
        public Bean Load(string kind, object key)
        {
            return Crud.Load(kind, key);
        }


        /// <summary>
        /// Query a Bean (row) of a given subclass from the Database
        /// </summary>
        /// <typeparam name="T">The Bean subclass to query</typeparam>
        /// <param name="key">The value of the primary key on the required row</param>
        /// <returns>A new Bean of the given subclass representing the requested row from the database</returns>
        public T Load<T>(object key) where T : Bean, new()
        {
            return Crud.Load<T>(key);
        }


        /// <summary>
        /// Query a Bean (row) from the Database
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="compoundKey">Object array that makes the compound key</param>
        /// <returns>A new Bean representing the requested row from the database</returns>
        public Bean Load(string kind, params object[] compoundKey)
        {
            return Load(kind, KeyUtil.PackCompoundKey(kind, compoundKey));
        }


        /// <summary>
        /// Query a Bean (row) of a given subclass from the Database
        /// </summary>
        /// <typeparam name="T">The Bean subclass to query</typeparam>
        /// <returns>A new Bean of the given subclass representing the requested row from the database</returns>
        public T Load<T>(params object[] compoundKey) where T : Bean, new()
        {
            return Load<T>(KeyUtil.PackCompoundKey(Bean.GetKind<T>(), compoundKey));
        }


        /// <summary>
        /// Save a given Bean to the database. Insert or Update a record as appropriate
        /// </summary>
        /// <param name="bean">A Bean or subclass thereof</param>
        /// <returns>The primary key(s) for the stored Bean</returns>
        public object Store(Bean bean)
        {
            return Crud.Store(bean);
        }


        /// <summary>
        /// Delete the underlying record of a Bean (row) from the database
        /// </summary>
        /// <param name="bean">A Bean (row) or subclass thereof</param>
        public void Trash(Bean bean)
        {
            Crud.Trash(bean);
        }


        // ----- IObserverSupport ---------------------------------------------

        /// <summary>
        /// Registers a class implementing BeanObserver to receive 
        /// notifications whenever Crud actions are applied to the database
        /// </summary>
        /// <param name="observer">A subclass of BeanObserver</param>
        public void AddObserver(BeanObserver observer)
        {
            Crud.AddObserver(observer);
        }


        /// <summary>
        /// Unregisters a class implementing BeanObserver from receiving
        /// notifications whenever Crud actions are applied to the database
        /// </summary>
        /// <param name="observer">A subclass of BeanObserver</param>
        public void RemoveObserver(BeanObserver observer)
        {
            Crud.RemoveObserver(observer);
        }


        /// <summary>
        /// Gets a loaded Observer of the given Type
        /// </summary>
        /// <typeparam name="T">Observer Type</typeparam>
        /// <returns></returns>
        public object GetObserver<T>()
        {
            return Crud.GetObserver<T>();
        }


        /// <summary>
        /// Removes a loaded Observer of the given Type
        /// </summary>
        /// <typeparam name="T">Observer Type</typeparam>
        /// <returns></returns>
        public void RemoveObserver<T>()
        {
            Crud.RemoveObserver<T>();
        }


        /// <summary>
        /// Checks if an Observer of the given Type is loaded.
        /// </summary>
        /// <typeparam name="T">Observer Type</typeparam>
        /// <returns>true, if the given Observer of Type is loaded</returns>
        public bool IsObserverLoaded<T>()
        {
            return Crud.IsObserverLoaded<T>();
        }


        /// <summary>
        /// Checks if the API has any Observers loaded.
        /// </summary>
        /// <returnstrue, if any Observer is loaded.></returns>
        public bool HasObservers()
        {
            return Crud.HasObservers();
        }


        // ----- IBeanFinder --------------------------------------------------

        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions. Prefer FindIterator() for large data sets. 
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters)
        {
            return Finder.Find(useCache, kind, expr, parameters);
        }


        /// <summary>
        /// Query the database for one or more Beans (rows) of the given subclass which match the given filter conditions. Prefer FindIterator() for large data sets. 
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Finder.Find<T>(useCache, expr, parameters);
        }


        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions. Prefer FindIterator() for large data sets. Uses caching.
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Find(string kind, string expr = null, params object[] parameters)
        {
            return Find(true, kind, expr, parameters);
        }


        /// <summary>
        /// Query the database for one or more Beans (rows) of the given subclass which match the given filter conditions. Prefer FindIterator() for large data sets. Uses caching.
        /// </summary>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T[] Find<T>(string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Find<T>(true, expr, parameters);
        }

        /// <summary>
        /// Paginates a query to the database.
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="pageNo">Number of the data page to return</param>
        /// <param name="perPage">Number or Rows per page</param>
        /// <param name="propsIgnorelist">List of Bean Properties to omit from output</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Paginate(bool useCache, string kind, int pageNo, int perPage = 10, 
            string propsIgnorelist = "", string expr = null, params object[] parameters)
        {
            return Finder.Paginate(useCache, kind, pageNo, perPage, propsIgnorelist, expr, parameters);
        }


        /// <summary>
        /// Paginates a query to the database and prefers the cached query result
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="pageNo">Number of the data page to return</param>
        /// <param name="perPage">Number or Rows per page</param>
        /// <param name="propsIgnorelist">List of Bean Properties to omit from output</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean[] Paginate(string kind, int pageNo, int perPage = 10, string propsIgnorelist = "", 
            string expr = null, params object[] parameters)
        {
            return Finder.Paginate(true, kind, pageNo, perPage, propsIgnorelist, expr, parameters);
        }

        /// <summary>
        /// Paginates a query to the database and returns a Bean subclass.
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="pageNo">Number of the data page to return</param>
        /// <param name="perPage">Number or Rows per page</param>
        /// <param name="propsIgnorelist">List of Bean Properties to omit from output</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public T[] Paginate<T>(bool useCache, int pageNo, int perPage = 10, string propsIgnorelist = "", 
            string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Finder.Paginate<T>(useCache, pageNo, perPage, propsIgnorelist, expr, parameters);
        }


        /// <summary>
        /// Paginates a query to the database, returns a Bean subclass and prefers the cached query result.
        /// </summary>
        /// <param name="pageNo">Number of the data page to return</param>
        /// <param name="perPage">Number or Rows per page</param>
        /// <param name="propsIgnorelist">List of Bean Properties to omit from output</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public T[] Paginate<T>(int pageNo, int perPage = 10, string propsIgnorelist = "", 
            string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Finder.Paginate<T>(true, pageNo, perPage, propsIgnorelist, expr, parameters);
        }


        /// <summary>
        /// Returns a Pagination object that contains paginated Bean data in
        /// Laravel style. The returned Bean's props can be cleansed by
        /// attaching a comma separated list of properties to ignore.
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="propsIgnorelist"></param>
        /// <param name="pageNo">Number of the data page to return</param>
        /// <param name="perPage">Number or Rows per page</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        /// <returns></returns>
        public Pagination LPaginate(bool useCache, string kind, int pageNo = 1, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters)
        {
            return Finder.LPaginate(useCache, kind, pageNo, perPage, propsIgnorelist, expr, parameters);
        }


        /// <summary>
        /// Returns a Pagination object that contains paginated Bean data in
        /// Laravel style. The returned Bean's props can be cleansed by
        /// attaching a comma separated list of properties to ignore.
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="propsIgnorelist"></param>
        /// <param name="pageNo">Number of the data page to return</param>
        /// <param name="perPage">Number or Rows per page</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        /// <returns></returns>
        /// <returns></returns>
        public Pagination LPaginate(string kind, int pageNo = 1, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters)
        {
            return Finder.LPaginate(true, kind, pageNo, perPage, propsIgnorelist, expr, parameters);
        }


        /// <summary>
        /// Query the database for the first Bean (row) which matches the given filter conditions
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters)
        {
            return Finder.FindOne(useCache, kind, expr, parameters);
        }


        /// <summary>
        /// Query the database for the first Bean (rows) of the given subclass which matches the given filter conditions
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Finder.FindOne<T>(useCache, expr, parameters);
        }


        /// <summary>
        /// Query the database for the first Bean (row) which matches the given filter conditions. Uses caching.
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans which meet the given query conditions</returns>
        public Bean FindOne(string kind, string expr = null, params object[] parameters)
        {
            return FindOne(true, kind, expr, parameters);
        }


        /// <summary>
        /// Query the database for the first Bean (rows) of the given subclass which matches the given filter conditions. Uses caching
        /// </summary>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of Beans of the given subclass which meet the given query conditions</returns>
        public T FindOne<T>(string expr = null, params object[] parameters) where T : Bean, new()
        {
            return FindOne<T>(true, expr, parameters);
        }


        /// <summary>
        /// Query the database for one or more Beans (rows) which match the given filter conditions. Recommended for large data sets
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An IEnumerable of Beans which meet the given query conditions</returns>
        public IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters)
        {
            return Finder.FindIterator(kind, expr, parameters);
        }


        /// <summary>
        /// Query the database for one or more Beans (rows) of a given subclass, which match the given filter conditions
        /// </summary>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An IEnumerable of the given Bean subclass which meet the given query conditions</returns>
        public IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Finder.FindIterator<T>(expr, parameters);
        }


        /// <summary>
        /// Count the number of rows which match the given expression on the given Kind
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count(bool useCache, string kind, string expr = null, params object[] parameters)
        {
            return Finder.Count(useCache, kind, expr, parameters);
        }


        /// <summary>
        /// Count the number of rows which match the given filter conditions on the Kind of the given Bean subclass
        /// </summary>
        /// <typeparam name="T">The Bean subclass which contains information of what Kind to Count on</typeparam>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Finder.Count<T>(useCache, expr, parameters);
        }


        /// <summary>
        /// Count the number of rows which match the given filter conditions on the given Kind. Uses caching
        /// </summary>
        /// <param name="kind">Name of the table to query</param>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count(string kind, string expr = null, params object[] parameters)
        {
            return Count(true, kind, expr, parameters);
        }


        /// <summary>
        /// Count the number of rows which match the given filter conditions on the Kind of the given Bean subclass. Uses caching
        /// </summary>
        /// <typeparam name="T">The Bean subclass which contains information of what Kind to Count on</typeparam>
        /// <param name="expr">The SQL Expression to run, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A count of the number of rows matching the given conditions</returns>
        public long Count<T>(string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Count<T>(true, expr, parameters);
        }


        // ----- IDatabaseAccess ----------------------------------------------

        /// <summary>
        /// Event which fires at the point of execution of any database query
        /// </summary>
        public event Action<DbCommand> QueryExecuting
        {
            add => Db.QueryExecuting += value;
            remove => Db.QueryExecuting -= value;
        }


        /// <summary>
        /// Gets or sets the number of recent queries which have their results cached
        /// </summary>
        public int CacheCapacity
        {
            get => Db.CacheCapacity;
            set => Db.CacheCapacity = value;
        }


        public string Database => Db.Database;
        public string Server => Db.Server;
        public string ConnectionString => Db.ConnectionString;
        public DatabaseType DbType => Details.DbType;


        /// <summary>
        /// Execute a given SQL 'Non Query' on the database
        /// </summary>
        /// <param name="sql">The SQL to execute, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>The number of rows affected if applicable, otherwise -1</returns>
        public int Exec(string sql, params object[] parameters)
        {
            return Db.Exec(sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query and return the first column as the specified type. Lazy loads each row when iterated on. 
        /// </summary>
        /// <typeparam name="T">The Type to return each value as</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>The value in the first returned column, as the specified type</returns>
        public IEnumerable<T> ColIterator<T>(string sql, params object[] parameters)
        {
            return Db.ColIterator<T>(sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query and return the first column as an object. Lazy loads each row when iterated on. 
        /// </summary>
        /// <typeparam name="T">The Type to return each value as</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>The value in the first returned column, as the specified type</returns>
        public IEnumerable<object> ColIterator(string sql, params object[] parameters)
        {
            return ColIterator<object>(sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query and return each row as a Dictionary. Lazy loads each row when iterated on. 
        /// </summary>
        /// <param name="sql">A SQL Query, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters"></param>
        /// <returns>A Dictionary representing a single row at a time</returns>
        public IEnumerable<IDictionary<string, object>> RowsIterator(string sql, params object[] parameters)
        {
            return Db.RowsIterator(sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single value, such as a Concat() or Sum(). Uses caching.
        /// </summary>
        /// <typeparam name="T">The type of the value to return</typeparam>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query ideally returning a single column/row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A single value of the specified type</returns>
        public T Cell<T>(bool useCache, string sql, params object[] parameters)
        {
            return Db.Cell<T>(useCache, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single value, such as a Concat() or Sum(). Uses caching
        /// </summary>
        /// <typeparam name="T">The type of the value to return</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column/row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A single value of the specified type</returns>
        public T Cell<T>(string sql, params object[] parameters)
        {
            return Cell<T>(true, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single value as an object, such as a Concat() or Sum(). Uses caching
        /// </summary>
        /// <param name="sql">A SQL Query ideally returning a single column/row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>A single value as an object</returns>
        public object Cell(string sql, params object[] parameters)
        {
            return Cell<object>(sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single column of values as the specified type
        /// </summary>
        /// <typeparam name="T">The type to return the column as</typeparam>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of values representing a column of the specified type</returns>
        public T[] Col<T>(bool useCache, string sql, params object[] parameters)
        {
            return Db.Col<T>(useCache, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single column of values as the specified type. Uses caching
        /// </summary>
        /// <typeparam name="T">The type to return the column as</typeparam>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of values representing a column of the specified type</returns>
        public T[] Col<T>(string sql, params object[] parameters)
        {
            return Col<T>(true, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single column of values as objects. Uses caching
        /// </summary>
        /// <param name="sql">A SQL Query ideally returning a single column, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of values representing a column of the specified type</returns>
        public object[] Col(string sql, params object[] parameters)
        {
            return Col<object>(true, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single row of values as a Dictionary
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query ideally returning a single row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An dictionary representing a row of data</returns>
        public IDictionary<string, object> Row(bool useCache, string sql, params object[] parameters)
        {
            return Db.Row(useCache, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning a single row of values as a Dictionary. Uses cachine
        /// </summary>
        /// <param name="sql">A SQL Query ideally returning a single row, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An dictionary representing a row of data</returns>
        public IDictionary<string, object> Row(string sql, params object[] parameters)
        {
            return Row(true, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning multiple rows of values as dictionaries
        /// </summary>
        /// <param name="useCache">Whether to cache the results of this query, or recall results if already cached</param>
        /// <param name="sql">A SQL Query return multiple rows, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of dictionaries, each representing a row of data</returns>
        public IDictionary<string, object>[] Rows(bool useCache, string sql, params object[] parameters)
        {
            return Db.Rows(useCache, sql, parameters);
        }


        /// <summary>
        /// Execute a SQL Query returning multiple rows of values as dictionaries. Uses caching
        /// </summary>
        /// <param name="sql">A SQL Query return multiple rows, with any parameters placeholdered with {0}, {1} etc</param>
        /// <param name="parameters">An array of parameters to properly parameterise in SQL</param>
        /// <returns>An array of dictionaries, each representing a row of data</returns>
        public IDictionary<string, object>[] Rows(string sql, params object[] parameters)
        {
            return Rows(true, sql, parameters);
        }


        // ----- ITransactionSupport ------------------------------------------

        /// <summary>
        /// Gets or Sets whether a transaction will automatically be used on the CUD aspects of Crud a operation
        /// Implicit Transactions do not occur if a Transaction is currently being handled by this instance of BeanAPI
        /// </summary>
        public bool ImplicitTransactions
        {
            get => Db.ImplicitTransactions;
            set => Db.ImplicitTransactions = value;
        }


        /// <summary>
        /// Gets whether there are any transactions currently being worked on
        /// </summary>
        public bool InTransaction => Db.InTransaction;


        /// <summary>
        /// Gets or sets the IsolationLevel of NBean database transactions
        /// </summary>
        public IsolationLevel TransactionIsolation
        {
            get => Db.TransactionIsolation;
            set => Db.TransactionIsolation = value;
        }


        /// <summary>
        /// Wraps an Action in a database transaction. Anything done here will roll back and throw if any error occurs
        /// </summary>
        /// <param name="action">The process to take place</param>
        public void Transaction(Func<bool> action)
        {
            Db.Transaction(action);
        }


        /// <summary>
        /// Wraps an Action in a database transaction. Anything done here will roll back and throw if any error occurs
        /// </summary>
        /// <param name="action">The process to take place</param>
        public void Transaction(Action action)
        {
            Transaction(() =>
            {
                action();
                return true;
            });
        }


        // ----- IValueRelaxations --------------------------------------------

        /// <summary>
        /// Gets or Sets whether string values being stored to the database have any trailing whitespace trimmed
        /// </summary>
        public bool TrimStrings
        {
            get => Storage.TrimStrings;
            set => Storage.TrimStrings = value;
        }


        /// <summary>
        /// Gets or Sets whether string values being stored to the database are converted to nulls if empty
        /// </summary>
        public bool ConvertEmptyStringToNull
        {
            get => Storage.ConvertEmptyStringToNull;
            set => Storage.ConvertEmptyStringToNull = value;
        }


        /// <summary>
        /// Gets or Sets whether integers are detected and converted from Double/Single/String variables
        /// when storing to the database. This allows fluid mode to guide your use of the schema
        /// </summary>
        public bool RecognizeIntegers
        {
            get => Storage.RecognizeIntegers;
            set => Storage.RecognizeIntegers = value;
        }


        // ----- Custom keys --------------------------------------------------

        /// <summary>
        /// Registers a new Primary Key on the given Kind
        /// </summary>
        /// <param name="kind">The table name</param>
        /// <param name="name">The name of the primary key field</param>
        /// <param name="autoIncrement">Whether the key should auto-increment</param>
        public void Key(string kind, string name, bool autoIncrement)
        {
            KeyUtil.RegisterKey(kind, new[] { name }, autoIncrement);
        }


        /// <summary>
        /// Registers a new multi-column Key on the given Kind
        /// </summary>
        /// <param name="kind">The table name</param>
        /// <param name="names">The names of the primary key fields</param>
        public void Key(string kind, params string[] names)
        {
            KeyUtil.RegisterKey(kind, names, null);
        }


        /// <summary>
        /// Registers a new Primary Key on the given Bean subtype's Kind
        /// </summary>
        /// <param name="name">The name of the primary key field</param>
        /// <param name="autoIncrement">Whether the key should auto-increment</param>
        public void Key<T>(string name, bool autoIncrement) where T : Bean, new()
        {
            Key(Bean.GetKind<T>(), name, autoIncrement);
        }


        /// <summary>
        /// Registers a new Primary Key on the given Bean subtype's Kind
        /// </summary>
        /// <param name="names">The names of the primary key fields</param>
        public void Key<T>(params string[] names) where T : Bean, new()
        {
            Key(Bean.GetKind<T>(), names);
        }


        /// <summary>
        /// Sets whether default Primary Keys auto-increment
        /// </summary>
        /// <param name="autoIncrement">Whether a new Key should auto-increment</param>
        public void DefaultKey(bool autoIncrement)
        {
            KeyUtil.DefaultAutoIncrement = autoIncrement;
        }


        /// <summary>
        /// Sets the default field name for a Primary Key, and whether it should auto-increment
        /// </summary>
        /// <param name="name">The default field name for a Primary Key</param>
        /// <param name="autoIncrement">Whether a default Primary Key should auto-increment</param>
        public void DefaultKey(string name, bool autoIncrement = true)
        {
            KeyUtil.DefaultName = name;
            KeyUtil.DefaultAutoIncrement = autoIncrement;
        }


        /// <summary>
        /// Gets the default field name for the Primary Key that is currently in use.
        /// </summary>
        /// <returns>Primary Key name.</returns>
        public string DefaultKey()
        {
            return KeyUtil.DefaultName;
        }


        /// <summary>
        /// Signs that the standard auto increment behaviour was replaced by a plugin (Observer).
        /// This Method has to be called for any custom key provider like "SlxKeyProvider".
        /// </summary>
        /// <param name="name">The default field name for a Primary Key</param>
        public void ReplaceAutoIncrement(string name)
        {
            DefaultKey(name, false);
            KeyUtil.AutoIncrementReplaced = true;
        }


        //----- IPluginSupport ------------------------------------------------

        internal PluginType PluginIsRegisteredAs(string name)
        {
            return
                _actions.ContainsKey(name)
                    ? PluginType.Action
                    : _functions.ContainsKey(name)
                        ? PluginType.Func
                        : _beanActions.ContainsKey(name)
                            ? PluginType.BeanAction
                            : _beanFunctions.ContainsKey(name)
                                ? PluginType.BeanFunc
                                : PluginType.None;
        }


        private void CheckRegistration(string name)
        {
            var registeredAs = PluginIsRegisteredAs(name);

            if (registeredAs != PluginType.None)
                throw PluginAlreadyRegisteredException.Create(name, registeredAs.ToString());
        }


        /// <summary>
        /// Registers an API Action.
        /// </summary>
        /// <param name="name">Action name</param>
        /// <param name="action">Action to be invoked.</param>
        public void RegisterAction(string name, Action<BeanApi, object[]> action)
        {
            CheckRegistration(name);
            _actions.Add(name, action);
        }


        /// <summary>
        /// Registers an API Function.
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="function">Function to be invoked.</param>
        public void RegisterFunc(string name, Func<BeanApi, object[], object> function)
        {
            CheckRegistration(name);
            _functions.Add(name, function);
        }


        /// <summary>
        /// Registers a Bean Action.
        /// </summary>
        /// <param name="name">Action name</param>
        /// <param name="action">Action to be invoked.</param>
        public void RegisterBeanAction(string name, Action<Bean, object[]> action)
        {
            CheckRegistration(name);
            _beanActions.Add(name, action);
        }

        /// <summary>
        /// Registers a Bean Function.
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="function">Function to be invoked.</param>
        public void RegisterBeanFunc(string name, Func<Bean, object[], object> function)
        {
            CheckRegistration(name);
            _beanFunctions.Add(name, function);
        }


        /// <summary>
        /// Invokes an API Action.
        /// </summary>
        /// <param name="name">Action name</param>
        /// <param name="args">Parameters for this Action</param>
        /// <returns>Return value as object</returns>
        public object Invoke(string name, params object[] args)
        {
            Bean bean = null;

            if (args.Any() && args[0].GetType() == typeof(Bean))
            {
                bean = (Bean)args[0];
                args = args.Skip(1).ToArray();
            }

            switch (PluginIsRegisteredAs(name))
            {
                case PluginType.Action:
                    _actions[name].Invoke(this, args);
                    break;
                case PluginType.Func:
                    return _functions[name].Invoke(this, args);
                case PluginType.BeanAction:
                    if (bean == null)
                        throw BeanIsMissingException.Create(name);
                    _beanActions[name].Invoke(bean, args);
                    break;
                case PluginType.BeanFunc:
                    if (bean == null)
                        throw BeanIsMissingException.Create(name);
                    return _beanFunctions[name].Invoke(bean, args);
                default:
                    throw PluginNotFoundException.Create(name);
            }

            return true;
        }

    }

}
