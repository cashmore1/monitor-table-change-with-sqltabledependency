﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.IntegrationTest.Helpers;
using TableDependency.IntegrationTest.Helpers.SqlServer;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
    public class SchemaNotDboTestSqlServerModelAlternateNaming
    {
        public string Name { get; set; }
    }

    [TestClass]
    public class UseSchemaOtherThanDboTestSqlServerAlternateNaming
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;
        private const string TableName = "Customers";
        private const string SchemaName = "test_schema";
        private static int _counter;
        private static Dictionary<string, Tuple<SchemaNotDboTestSqlServerModel, SchemaNotDboTestSqlServerModel>> _checkValues = new Dictionary<string, Tuple<SchemaNotDboTestSqlServerModel, SchemaNotDboTestSqlServerModel>>();

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TableName}' AND TABLE_SCHEMA = '{SchemaName}'";
                    var exists = (int)sqlCommand.ExecuteScalar();
                    if (exists > 0)
                    {
                        sqlCommand.CommandText = $"DROP TABLE {SchemaName}.{TableName}";
                        sqlCommand.ExecuteNonQuery();
                    }                   

                    sqlCommand.CommandText = $"CREATE TABLE {SchemaName}.{TableName} ([Name] [nvarchar](50) NULL)";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TableName}' AND TABLE_SCHEMA = '{SchemaName}'";
                    var exists = (int)sqlCommand.ExecuteScalar();
                    if (exists > 0)
                    {
                        sqlCommand.CommandText = $"DROP TABLE {SchemaName}.{TableName}";
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void TableWithTest()
        {
            SqlTableDependency<SchemaNotDboTestSqlServerModel> tableDependency = null;
            string naming = null;

            try
            {
                tableDependency = new SqlTableDependency<SchemaNotDboTestSqlServerModel>(_connectionString, SchemaName + "." + TableName,dataBaseObjectNamePrefix:Constants.NAMINGTOKEN);
                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.Start();
                naming = tableDependency.DataBaseObjectsNamingConvention;

                Thread.Sleep(5000);

                var t = new Task(ModifyTableContent);
                t.Start();
                t.Wait(20000);
            }
            finally
            {
                tableDependency?.Dispose();
            }

            Assert.AreEqual(_counter, 3);
            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Name, _checkValues[ChangeType.Insert.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Update.ToString()].Item2.Name, _checkValues[ChangeType.Update.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Name, _checkValues[ChangeType.Delete.ToString()].Item1.Name);
            Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(naming));
            Assert.IsTrue(naming.Contains(Constants.NAMINGTOKEN), $"The naming convention of [ {Constants.NAMINGTOKEN} ] was not found in the object naming where it belong.");
        }

        private static void TableDependency_Changed(object sender, RecordChangedEventArgs<SchemaNotDboTestSqlServerModel> e)
        {
            _counter++;

            switch (e.ChangeType)
            {
                case ChangeType.Insert:
                    _checkValues[ChangeType.Insert.ToString()].Item2.Name = e.Entity.Name;
                    break;
                case ChangeType.Update:
                    _checkValues[ChangeType.Update.ToString()].Item2.Name = e.Entity.Name;
                    break;
                case ChangeType.Delete:
                    _checkValues[ChangeType.Delete.ToString()].Item2.Name = e.Entity.Name;
                    break;
            }
        }

        private static void ModifyTableContent()
        {
            _checkValues.Add(ChangeType.Insert.ToString(), new Tuple<SchemaNotDboTestSqlServerModel, SchemaNotDboTestSqlServerModel>(new SchemaNotDboTestSqlServerModel { Name = "Christian" }, new SchemaNotDboTestSqlServerModel()));
            _checkValues.Add(ChangeType.Update.ToString(), new Tuple<SchemaNotDboTestSqlServerModel, SchemaNotDboTestSqlServerModel>(new SchemaNotDboTestSqlServerModel { Name = "Velia" }, new SchemaNotDboTestSqlServerModel()));
            _checkValues.Add(ChangeType.Delete.ToString(), new Tuple<SchemaNotDboTestSqlServerModel, SchemaNotDboTestSqlServerModel>(new SchemaNotDboTestSqlServerModel { Name = "Velia" }, new SchemaNotDboTestSqlServerModel()));

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"INSERT INTO {SchemaName}.{TableName} ([Name]) VALUES ('{_checkValues[ChangeType.Insert.ToString()].Item1.Name}')";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(500);

                    sqlCommand.CommandText = $"UPDATE {SchemaName}.{TableName} SET [Name] = '{_checkValues[ChangeType.Update.ToString()].Item1.Name}'";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(500);

                    sqlCommand.CommandText = $"DELETE FROM {SchemaName}.{TableName}";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(500);
                }
            }
        }
    }
}