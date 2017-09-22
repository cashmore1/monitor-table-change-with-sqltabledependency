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
    public class DatabaseObjectCleanUpTestSqlServerModelAlternateNaming
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime Born { get; set; }
        public int Quantity { get; set; }
    }

    [TestClass]
    public class DatabaseObjectAutoCleanUpTestSqlServerAlternateNaming
    {
        private static string _dbObjectsNaming;
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;
        private static string TableName = "AAADCheck_Model";

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

                    sqlCommand.CommandText =
                        $"CREATE TABLE [{TableName}]( " +
                        "[Id][int] IDENTITY(1, 1) NOT NULL, " +
                        "[First Name] [nvarchar](50) NOT NULL, " +
                        "[Second Name] [nvarchar](50) NOT NULL, " +
                        "[Born] [datetime] NULL)";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"DELETE FROM [{TableName}]";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [ClassCleanup()]
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
            }
        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void DatabaseObjectCleanUpTest()
        {
            var domaininfo = new AppDomainSetup();
            domaininfo.ApplicationBase = Environment.CurrentDirectory;
            var adevidence = AppDomain.CurrentDomain.Evidence;
            var domain = AppDomain.CreateDomain("RunsInAnotherAppDomain_Check_DatabaseObjectCleanUp", adevidence, domaininfo);

            var otherDomainObject = (RunsInAnotherAppDomain_Check_DatabaseObjectCleanUpAlternateNaming)domain.CreateInstanceAndUnwrap(typeof(RunsInAnotherAppDomain_Check_DatabaseObjectCleanUpAlternateNaming).Assembly.FullName, typeof(RunsInAnotherAppDomain_Check_DatabaseObjectCleanUpAlternateNaming).FullName);
            _dbObjectsNaming = otherDomainObject.RunTableDependency(ConnectionString, TableName, Constants.NAMINGTOKEN);

            Thread.Sleep(5000);
            AppDomain.Unload(domain);

            SmallModifyTableContent();

            Thread.Sleep(3 * 60 * 1000);
            Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(_dbObjectsNaming));
            Assert.IsTrue(SqlServerHelper.AreAllEndpointDisposed(_dbObjectsNaming));
            Assert.IsTrue(_dbObjectsNaming.Contains(Constants.NAMINGTOKEN), $"The naming convention of [ {Constants.NAMINGTOKEN} ] was not found in the object naming where it belong.");
        }

        private static void SmallModifyTableContent()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([First Name], [Second Name]) VALUES ('allora', 'mah')";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
    }

    public class RunsInAnotherAppDomain_Check_DatabaseObjectCleanUpAlternateNaming : MarshalByRefObject
    {
        public string RunTableDependency(string connectionString, string tableName, string databaseObjectNamePrefix)
        {
            var mapper = new ModelToTableMapper<DatabaseObjectCleanUpTestSqlServerModel>();
            mapper.AddMapping(c => c.Name, "First Name").AddMapping(c => c.Surname, "Second Name");

            var tableDependency = new SqlTableDependency<DatabaseObjectCleanUpTestSqlServerModel>(connectionString, tableName,
                mapper, dataBaseObjectNamePrefix: databaseObjectNamePrefix);
            tableDependency.OnChanged += (sender, e) => { };
            tableDependency.Start(60, 120);
            return tableDependency.DataBaseObjectsNamingConvention;
        }
    }
}