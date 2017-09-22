﻿using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.IntegrationTest.Helpers;
using TableDependency.IntegrationTest.Helpers.SqlServer;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
    //public class DatabaseObjectAutoCleanUpAfterHugeInsertsTestSqlServerModelAlternateNaming
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public string Surname { get; set; }
    //    public DateTime Born { get; set; }
    //    public int Quantity { get; set; }
    //}

#if DEBUG
    [TestClass]
    public class DatabaseObjectAutoCleanUpAfterHugeInsertsTestSqlServerAn
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;
        private static string TableName = "DbObjAutoCleanUpAfterHugeInsertsTestSSrvModel";

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();

                    sqlCommand.CommandText = $"CREATE TABLE [{TableName}]([Id][int], [First Name] [nvarchar](50), [Second Name] [nvarchar](50))";
                    sqlCommand.ExecuteNonQuery();
                }
                sqlConnection.Close();
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();
                }
                sqlConnection.Close();
            }
        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void DatabaseObjectCleanUpTest()
        {
            var mapper = new ModelToTableMapper<DatabaseObjectAutoCleanUpAfterHugeInsertsTestSqlServerModel>();
            mapper.AddMapping(c => c.Name, "First Name").AddMapping(c => c.Surname, "Second Name");

            var tableDependency = new SqlTableDependency<DatabaseObjectAutoCleanUpAfterHugeInsertsTestSqlServerModel>(ConnectionString, 
                TableName, mapper, dataBaseObjectNamePrefix: Constants.NAMINGTOKEN);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.Start();
            var dbObjectsNaming = tableDependency.DataBaseObjectsNamingConvention;

            Thread.Sleep(5000);

            tableDependency.StopWithoutDisposing();

            Thread.Sleep(1000);

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    for (var i = 0; i < 10000; i++)
                    {
                        sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([First Name], [Second Name]) VALUES ('{i}', '{i}')";
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                sqlConnection.Close();
            }

            Thread.Sleep(1000 * 60 * 3);

            Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(dbObjectsNaming));
            Assert.IsTrue(SqlServerHelper.AreAllEndpointDisposed(dbObjectsNaming));
            Assert.IsTrue(dbObjectsNaming.Contains(Constants.NAMINGTOKEN), $"The naming convention of [ {Constants.NAMINGTOKEN} ] was not found in the object naming where it belong.");
        }

        private static void TableDependency_OnChanged(object sender, EventArgs.RecordChangedEventArgs<DatabaseObjectAutoCleanUpAfterHugeInsertsTestSqlServerModel> e)
        {
        }
    }
#endif
}