using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using Npgsql;

namespace DBcs;

public class DBcs
{
    /// <summary>
    /// in this case, override CreateDataSource()
    /// </summary>
    public DBcs()
    {
    }
    public DBcs(string connectionString)
    {
        this.connectionString = connectionString;
        dataSource = CreateDataSource();
    }
    /// <summary>
    /// From DbDataSource
    /// </summary>
    /// <param name="dataSource">with valid connection string</param>
    public DBcs(DbDataSource dataSource)
    {
        this.dataSource = dataSource;
        this.connectionString = dataSource.ConnectionString;
    }

    private string connectionString { get; set; } = "";
    private DbDataSource? dataSource { get; set; }

    #region simple usage
    /// <summary>
    ///     Automatically fills Command parameters
    ///     from parameterObject properties
    ///     and executes NonQuery
    /// </summary>
    /// <param name="sqlQuery">use @ as parameter prefix</param>
    /// <param name="parameterObject">Object with data for parameters</param>
    /// <param name="commandType"></param>
    public async Task<int> RunNonQueryAsync(string sqlQuery, object? parameterObject = null, CommandType commandType = CommandType.Text)
    {
        var parameters = ExtractParameters(sqlQuery);
        using var cmd = CreateAndFillCommand(parameterObject, parameters, sqlQuery, commandType);
        using var conn = CreateConnection();
        // cmd.Connection = conn;
        await conn.OpenAsync();

        var ret = await cmd.ExecuteNonQueryAsync();
        return ret;
    }

    /// <summary>
    ///     Automatically fills Command parameters
    ///     from parameterObject properties
    ///     and executes Scalar
    /// </summary>
    /// <param name="sqlQuery"></param>
    /// <param name="parameterObject"></param>
    /// <param name="commandType"></param>
    /// <returns></returns>
    public async Task<object?> RunScalarAsync(string sqlQuery, object? parameterObject = null, CommandType commandType = CommandType.Text)
    {
        var parameters = ExtractParameters(sqlQuery);

        using var cmd = CreateAndFillCommand(parameterObject, parameters, sqlQuery, commandType);
        using var conn = CreateConnection();
        //    cmd.Connection = conn;
        await conn.OpenAsync();
        var ret = await cmd.ExecuteScalarAsync();
        return ret;
    }

    public async Task<T?> RunQuerySingleOrDefaultAsync<T>(string sqlQuery, object? parameterObject = null, CommandType commandType = CommandType.Text)
    where T : new()
    {
        var parameters = ExtractParameters(sqlQuery);
        var cmd = CreateAndFillCommand(parameterObject, parameters, sqlQuery, commandType);
        cmd.CommandType = commandType;

        T ret = new();
        using var conn = CreateConnection();
        //  cmd.Connection = conn;
        await conn.OpenAsync();
        using var dr = await cmd.ExecuteReaderAsync();
        if (await dr.ReadAsync())
        {
            var t = new T();
            FillObject(t, dr);

            ret = t;
        }
        return ret;
    }

    public async Task RunQueryWithCallBackAsync<T>(string sqlQuery, Action<T>? rowLoaded, object? parameterObject = null, CommandType commandType = CommandType.Text)
    where T : new()
    {
        await RunQuery<T>(sqlQuery, rowLoaded, parameterObject, commandType);
    }
    public async Task<IList<T>?> RunQueryAsync<T>(string sqlQuery, object? parameterObject = null, CommandType commandType = CommandType.Text)
    where T : new()
    {
        return await RunQuery<T>(sqlQuery, null, parameterObject, commandType);
    }


    /// <summary>
    ///     Returns List of object created from Query
    ///     Method adds parameters to Query
    ///     from parameterObject properties
    /// </summary>
    /// <param name="parameterObject"></param>
    /// <param name="sqlQuery"></param>
    /// <param name="commandType"></param>
    /// <typeparam name="T">Type of List elements. Must have ctor without parameters</typeparam>
    /// <returns>List of objects</returns>
    /// <exception cref="ArgumentNullException">parameterObject may not be null</exception>
    private async Task<IList<T>?> RunQuery<T>(string sqlQuery, Action<T>? rowLoaded = null, object? parameterObject = null, CommandType commandType = CommandType.Text)
        where T : new()
    {

        var parameters = ExtractParameters(sqlQuery);
        var cmd = CreateAndFillCommand(parameterObject, parameters, sqlQuery, commandType);
        cmd.CommandType = commandType;

        List<T> ret = [];
        using var conn = CreateConnection();
        //    cmd.Connection = conn;
        await conn.OpenAsync();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            var t = new T();
            FillObject(t, dr);
            if (rowLoaded == null)
            {
                ret.Add(t);
            }
            else
            {
                rowLoaded(t);
            }
        }
        if (rowLoaded == null)
            return ret;
        else
            return null;
    }
    #endregion

    #region DDL generation
    public string GetDDLCodeString(Type[] types, string[] tableNames)
    {
        Class2DDL c = new Class2DDL();
        return c.GetDdl(types, tableNames);
    }
    public string GetDDLCodeString(Type type, string tableName)
    {
        return GetDDLCodeString(new[] { type }, new[] { tableName }); 
    }

    #endregion

    #region class text generation
    /// <summary>
    ///     returns C# code for class
    ///     which can hold Query results
    /// </summary>
    /// <param name="className"></param>
    /// <param name="query"></param>
    /// <returns>C# code</returns>
    public async Task<string> GetClassCodeString(string className, string query)
    {
        return await GetClassCodeString(new[] { className }, query);
    }

    /// <summary>
    ///     Returns C# code for class(es)
    ///     which can hold Query results.
    ///     You can specify multiple queries.
    ///     Method will try to generate class
    ///     code for every SELECT.
    /// </summary>
    /// <param name="classNames"></param>
    /// <param name="query"></param>
    /// <returns>C# code</returns>
    public async Task<string> GetClassCodeString(string[] classNames, string query, CommandType commandType = CommandType.Text)
    {
        // get all foreign keys
        string sql = @"
               select
               kcu.table_schema as table_schema,
               kcu.table_name as table_name,
               kcu.column_name as column_name,
               ccu.table_schema referenced_table_schema,
               ccu.table_name as referenced_table_name,
               ccu.column_name as referenced_column_name,
               kcu.constraint_name
               from
               information_schema.key_column_usage as kcu
               inner join
               information_schema.referential_constraints as rc on kcu.constraint_name=rc.constraint_name
               inner join  
               information_schema.constraint_column_usage as ccu on kcu.constraint_name=ccu.constraint_name
               ;";
        IList<ForeignKey>? foreignKeys = await RunQuery<ForeignKey>(sql);
        List<DatabaseTable> databaseTables = new List<DatabaseTable>();


        var ret = $"using System.ComponentModel.DataAnnotations.Schema;{Environment.NewLine}";

        using var conn = CreateConnection();
        using var cmd = CreateCommand();
        //     cmd.Connection = conn;
        cmd.CommandText = query;
        cmd.CommandType = commandType;
        await conn.OpenAsync();
        var classCounter = 0;
        using var dr = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo);
        do
        {

            if (!dr.Read()) continue;
            DatabaseTable databaseTable = new DatabaseTable();
            databaseTables.Add(databaseTable);

            DataTable? dt = dr.GetSchemaTable();
            string tableName = $"{dt.Rows[0].ItemArray[10]}.{dt.Rows[0].ItemArray[11]}";
            databaseTable.TableName = $"{dt.Rows[0].ItemArray[11]}";
            databaseTable.FullTableName = tableName;
            databaseTable.ClassName = classNames[classCounter];

            for (var column = 0; column < dr.FieldCount; column++)
            {
                var databaseTableRow = new DatabaseTableRow();
                databaseTable.Rows.Add(databaseTableRow);
                bool isKey = (bool)dt.Rows[column].ItemArray[dt.Rows[column].Table.Columns["IsKey"].Ordinal];
                bool allowDBNull = (bool)dt.Rows[column].ItemArray[dt.Rows[column].Table.Columns["AllowDBNull"].Ordinal];

                databaseTableRow.IsDBNull = allowDBNull;
                databaseTableRow.IsKey = isKey;
                if (isKey)
                    databaseTable.PrimaryKeys.Add(databaseTableRow);

                var name = dr.GetName(column);
                databaseTableRow.Name = name;


                databaseTableRow.PropertyName = Utility.SnakeToCamel(name);


                var fieldType = dr.GetFieldType(column);
                var typeName = fieldType.Name;
                if (typeName == "Array")
                {
                    if (!dr.IsDBNull(column))
                        typeName = GetFriendlyTypeName(dr[column].ToString()?.Replace("System.", ""));
                }
                else
                {
                    typeName = GetFriendlyTypeName(typeName);
                }
                databaseTableRow.DotNetPropertyType = fieldType.FullName;
                databaseTableRow.PropertyType = typeName;

                //is FK?
                ForeignKey? fk = foreignKeys?.SingleOrDefault(k => k.TableFullName == tableName && k.ColumnName == name);

                if (fk != null)
                {
                    databaseTableRow.ForeignKey = fk;
                    databaseTable.ForeignKeys.Add(databaseTableRow);
                }
            }

            classCounter++;
        } while (dr.NextResult());

        //once we have all tables go back and update foreigh keys
        foreach (DatabaseTable databaseTable in databaseTables)
        {
            foreach (var fk in databaseTable.ForeignKeys)
            {
                if (fk.ForeignKey != null)
                {
                    string findTable = fk.ForeignKey.ReferencedFulllTableName;
                    DatabaseTable tmp = databaseTables.SingleOrDefault(t => t.FullTableName == findTable);
                    if (tmp != null)
                    {
                        fk.ForeignKey.ReferencedTable = tmp;
                        // here let's add collection to refernced table
                        fk.ForeignKey.ReferencedTable.Rows.Add(new DatabaseTableRow
                        {
                            IsDBNull = fk.IsDBNull,
                            IsKey = false,
                            PropertyType = $"List<{databaseTable.ClassName}>",
                            Name = databaseTable.TableName,
                            PropertyName = Utility.SnakeToCamel(databaseTable.TableName),
                            Comment = "// Collection of class that references via FK",
                            IsInDb = false
                        }
                        );
                    }
                }
            }

        }
        foreach (DatabaseTable databaseTable in databaseTables)
        {
            ret += databaseTable.GenerateClassText();
        }

        return ret;
    }

   
    #endregion

    #region advanced

    public async Task RunAndFillReferenceTypesWithCallbackAsync<T>(
        string sqlQuery, Action<T> rowLoaded, object? parameterObject = null, CommandType commandType = CommandType.Text)
      where T : new()
    {
        await RunAndFillReferenceTypes<T>(sqlQuery, rowLoaded, parameterObject, commandType);
    }
    public async Task<List<T>> RunAndFillReferenceTypesAsync<T>(
        string sqlQuery, object? parameterObject = null, CommandType commandType = CommandType.Text)
     where T : new()
    {
        return await RunAndFillReferenceTypes<T>(sqlQuery, null, parameterObject, commandType);
    }

    /// <summary>
    ///     Loads data from query including collections
    ///     indirectly specified by query.
    ///     Make sure query is ordered first by main
    ///     object and then by
    ///     collection 1, collection 2 .. collection n
    ///     Properties that are collection must be
    ///     named similar to tables. Snake case of
    ///     table names will be normalized to be
    ///     comparable to property names. For example
    ///     property QuizQuestions will be created
    ///     from table quiz_questions
    /// </summary>
    /// <param name="sqlQuery"></param>
    /// <param name="commandType"></param>
    /// <typeparam name="T">Must have constructor without params.</typeparam>
    /// <returns></returns>
    private async Task<List<T>> RunAndFillReferenceTypes<T>(
        string sqlQuery, Action<T>? rowLoaded = null, object? parameterObject = null, CommandType commandType = CommandType.Text)
        where T : new()
    {

        List<T> ret = [];
        TableColumnsCollection<T> tables = new TableColumnsCollection<T>();
        tables.ReturnCollection = ret;

        var parameters = ExtractParameters(sqlQuery);
        using var cmd = CreateAndFillCommand(parameterObject, parameters, sqlQuery, commandType);

        cmd.CommandText = sqlQuery;
        cmd.CommandType = commandType;

        using var conn = CreateConnection();
        //      cmd.Connection = conn;
        await conn.OpenAsync();
        using var dr = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo);
        while (await dr.ReadAsync())
        {
            UpdateTables<T>(dr, tables, rowLoaded);
        }
        var u = tables.ReturnCollection[tables.ReturnCollection.Count - 1];
        if (rowLoaded != null)
        {
            rowLoaded(u);
            tables.ReturnCollection.Remove(u);
        }

        tables.Reset();
        tables = null;
        return ret;
    }


    private void UpdateTables<T>(
        IDataReader dr,
        TableColumnsCollection<T> tables,
        Action<T>? rowLoaded = null
    ) where T : new()
    {
        var schemaTable = dr.GetSchemaTable();

        for (var column = 0; column < dr.FieldCount; column++)
        {
            var fromTable = schemaTable?.Rows[column].ItemArray[
                schemaTable.Columns["BaseTableName"]!.Ordinal];
            if (fromTable != null)
            {
                TableColumns table = tables.AddTable(fromTable.ToString(), column, dr, typeof(T));
                foreach (PropertyInfo p in typeof(T).GetProperties())
                {
                    string prop = PreparePropertyName(p.Name);
                    string tname = PrepareDbName(fromTable.ToString());
                    if (prop == tname)
                    {
                        table.PropName = p.Name;
                    }
                }

            }
        }

        CheckForChanges<T>(dr, tables, rowLoaded);
    }
    private void CheckForChanges<T>(IDataReader dr, TableColumnsCollection<T> tables, Action<T>? rowLoaded = null) where T : new()
    {
        Type rootType = typeof(T);
        bool called = false;
        foreach (var table in tables)
            if (
                (table.TmpValues != table.Values)
                ||
                (called == true)
                )
            {
                TableChanged<T>(
                    this,
                    new TableChangedEventArgs
                    {
                        Table = table,
                        DataReader = dr,
                        RootType = rootType,
                    }
                    , rowLoaded,
                    tables
                );
                table.Values = table.TmpValues;
                table.TmpValues = "";
                // once change is called, we call it for all sub elements
                called = true;
            }

        foreach (var table in tables)
            table.TmpValues = "";
    }
    private void TableChanged<T>(object sender, TableChangedEventArgs e, Action<T>? rowLoaded, TableColumnsCollection<T> t) where T : new()
    {
        //      Console.WriteLine($"TableChanged {e.Table.Name}");
        //    TableColumnsCollection<T> t = (TableColumnsCollection<T>)sender;
        if (e.Table.TableIndex == 0)
        {
            if (t.ReturnCollection.Count > 0)
            {
                var u = t.ReturnCollection[t.ReturnCollection.Count - 1];
                if (rowLoaded != null)
                {
                    rowLoaded(u);
                    t.ReturnCollection.Remove(u);
                }
            }
            T obj2Fill = new T();
            t.ReturnCollection.Add(obj2Fill);
            FillComplexObject(e.Table, obj2Fill, e.DataReader);
            t[0].Object = obj2Fill;
            ////root
            //var ctor = e.RootType.GetConstructors();
            //var o = ctor[0].Invoke(new object[] { });
            //var prop = sender.GetType().GetProperty("ReturnCollection");
            //var prop1 = prop.GetValue(sender);
            //var add = prop1.GetType().GetMethod("Add");
            //add.Invoke(sender, new[] { o });
        }
        else
        {
            //its a property
            //find it in object
            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            foreach (var prop in propertyInfos)
            {
                //   Console.WriteLine($"AAAAAA -{GetTypesTable(prop)}-{PrepareDbName(e.Table.Name)}");
                if (GetTypesTable(prop) == PrepareDbName(e.Table.Name))
                {
                    var o = prop.GetValue(t[0].Object);
                    if (o == null)
                    {
                        //Create new
                        var ctors = prop.PropertyType.GetConstructors();
                        o = ctors[0].Invoke(new object[] { });
                        prop.SetValue(t[0].Object, o);
                    }
                    // is it a collection?
                    if (IsList(o))
                    {
                        var typeOfList = prop.PropertyType.GetGenericArguments();
                        var add = o.GetType().GetMethod("Add");
                        var ctors1 = typeOfList[0].GetConstructors();
                        var o1 = ctors1[0].Invoke(new object[] { });
                        add.Invoke(o, new[] { o1 });
                        FillComplexObject(e.Table, o1, e.DataReader);
                    }
                    else
                    {
                        FillComplexObject(e.Table, o, e.DataReader);
                    }
                }
            }
        }
    }
    private bool IsList(object o)
    {
        if (o == null) return false;
        return o is IList &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
    }

    private bool IsDictionary(object o)
    {
        if (o == null) return false;
        return o is IDictionary &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
    }
    private string GetTypesTable(PropertyInfo p)
    {

        string ret = PreparePropertyName(p.Name);

        object[] attrs = p.PropertyType.GetCustomAttributes(true);
        foreach (object attr in attrs)
        {
            TableAttribute talbeAttr = attr as TableAttribute;
            if (talbeAttr != null)
            {
                string propName = p.Name;
                string table = PrepareDbName(talbeAttr.Name);
                return table;

            }
        }

        return ret;
    }

    private void FillComplexObject<T>(TableColumns table, T obj2Fill, IDataReader dr)
    {
        PropertyInfo[] propertyInfos = obj2Fill.GetType().GetProperties();
        foreach (var column in table.ColumnIndices)
        {
            var name = dr.GetName(column);
            var preparedName = PrepareDbName(name);
            foreach (var prop in propertyInfos)
                if (prop.CanWrite)
                    if (prop.Name.ToLower() == preparedName)
                    {
                        var val = dr.GetValue(column);
                        var dbType = val.GetType();
                        var propType = prop.PropertyType;

                        val = ConvertDbType(val, propType, dbType);

                        if (
                            // query returned not null
                            val != DBNull.Value ||
                            // or it returned null, but null is allowed
                            (val == DBNull.Value && Utility.IsMarkedAsNullable(prop))
                        )
                            if (val != DBNull.Value)
                                prop.SetValue(
                                    obj2Fill,
                                    val
                                );
                    }
        }
    }

    private void FillObject<T>(T obj2Fill, IDataReader dr)
    {
        PropertyInfo[] propertyInfos = obj2Fill.GetType().GetProperties();
        for (var column = 0; column < dr.FieldCount; column++)
        {
            var name = dr.GetName(column);
            var preparedName = PrepareDbName(name);
            foreach (var prop in propertyInfos)
                if (prop.CanWrite)
                    if (PreparePropertyName(prop.Name) == preparedName)
                    {
                        var val = dr.GetValue(column);
                        var dbType = val.GetType();
                        var propType = prop.PropertyType;
                        val = ConvertDbType(val, propType, dbType);

                        if (
                            // query returned not null
                            val != DBNull.Value ||
                            // or it returned null, but null is allowed
                            (val == DBNull.Value && Utility.IsMarkedAsNullable(prop))
                        )
                            prop.SetValue(
                                obj2Fill,
                                val
                            );
                    }
        }
    }

    
    #endregion

    #region helpers
    private DbCommand CreateAndFillCommand(object? parameterObject, string[] parameters, string sqlQuery,
        CommandType commandType = CommandType.Text)
    {
        var cmd = CreateCommand();
        cmd.CommandText = sqlQuery;
        cmd.CommandType = commandType;
        if (parameterObject != null)
        {
            PropertyInfo[] propertyInfos = parameterObject.GetType().GetProperties();
            foreach (var p in parameters)
            {
                //prepare param for comparison with obj props
                var pPrepared = PrepareDbName(p);

                //try to find appropriate properties
                foreach (var pi in propertyInfos)
                    if (pi.CanRead)
                        if (PreparePropertyName(pi.Name) == pPrepared)
                            //add param ..
                            cmd.Parameters.Add(
                                CreateParameter(cmd, p, pi.GetValue(parameterObject))
                            );
            }
        }
        return cmd;
    }

    /// <summary>
    ///     Add code for converting more advanced types here.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="propertyType"></param>
    /// <param name="dbType"></param>
    /// <returns></returns>
    public virtual object ConvertDbType(object value, Type propertyType, Type dbType)
    {
        #region converter from string[] in db to List<string> property

        if (dbType == typeof(string[]))
            if (propertyType == typeof(List<string>))
                value = ((string[])value).ToList();

        #endregion

        return value;
    }

    private static string[] ExtractParameters(string sqlQuery)
    {
        // get parameters from sqlQuery
        var regex = new Regex(@"\@\w+",
            RegexOptions.Multiline
            | RegexOptions.IgnoreCase
        );
        var parameters = regex.Matches(sqlQuery)
            .Select(m => m.Value)
            .ToArray();
        return parameters;
    }

    private string PreparePropertyName(string propertyName)
    {
        //usually Camel case so we dont need to do much
        return propertyName.ToLower();
    }

    private string PrepareDbName(string propertyDbName)
    {
        //can be snake case
        //for value my_table_name returns mytablename
        return propertyDbName.Replace("_", "")
            .ToLower().Replace("@", "");
    }

    private static string GetFriendlyTypeName(string type)
    {
        var ret = type.ToLower() switch
        {
            "sbyte" => "sbyte",
            "byte" => "byte",
            "int16" => "short",
            "uint16" => "ushort",
            "int32" => "int",
            "uint32" => "uint",
            "int64" => "long",
            "uint64" => "ulong",
            "intptr" => "nint",
            "uintptr" => "unint",
            "single" => "float",
            "double" => "double",
            "decimal" => "decimal",
            "boolean" => "bool",
            "char" => "char",
            "string" => "string",
            "string[]" => "string[]",
            _ => type
        };

        return ret;
    }

    #endregion

    #region virtual
    public virtual DbCommand CreateCommand()
    {
        if (dataSource == null)
            throw new ArgumentException();
        return dataSource.CreateCommand();
    }
    public virtual DbConnection CreateConnection()
    {
        if (dataSource == null)
            throw new ArgumentException();
        return dataSource.CreateConnection();
    }
    public virtual DbDataSource CreateDataSource()
    {
        return NpgsqlDataSource.Create(connectionString);
    }

    public virtual IDbDataParameter CreateParameter(DbCommand command, string name, object? value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        return p;
    }
    #endregion

}