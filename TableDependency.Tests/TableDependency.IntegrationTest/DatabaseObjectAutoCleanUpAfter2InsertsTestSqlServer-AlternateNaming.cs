﻿using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.IntegrationTest.Helpers;
using TableDependency.IntegrationTest.Helpers.SqlServer;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
    //public class DatabaseObjectAutoCleanUpAfter2InsertsTestSqlServerModelAlternateNaming
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public string Surname { get; set; }
    //    public DateTime Born { get; set; }
    //    public int Quantity { get; set; }
    //}

#if DEBUG
    [TestClass]
    public class DatabaseObjectAutoCleanUpAfter2InsertsTestSqlServerAn
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;
        private static string TableName = "DbObjectAutoCleanUpAfter2InsertsTestSModel";

        [ClassInitialize]
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
            var mapper = new ModelToTableMapper<DatabaseObjectAutoCleanUpAfter2InsertsTestSqlServerModel>();
            mapper.AddMapping(c => c.Name, "First Name").AddMapping(c => c.Surname, "Second Name");

            var tableDependency = new SqlTableDependency<DatabaseObjectAutoCleanUpAfter2InsertsTestSqlServerModel>(ConnectionString, 
                TableName, mapper, dataBaseObjectNamePrefix: Constants.NAMINGTOKEN);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.Start();
            var dbObjectsNaming = tableDependency.DataBaseObjectsNamingConvention;

            Thread.Sleep(500);

            tableDependency.StopWithoutDisposing();

            Thread.Sleep(500);

            var t = new Task(ModifyTableContent);
            t.Start();

            Thread.Sleep(1000 * 60 * 4);

            Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(dbObjectsNaming));
            Assert.IsTrue(SqlServerHelper.AreAllEndpointDisposed(dbObjectsNaming));

            Assert.IsTrue(dbObjectsNaming.Contains(Constants.NAMINGTOKEN), $"The naming convention of [ {Constants.NAMINGTOKEN} ] was not found in the object naming where it belong.");
        }

        private void TableDependency_OnChanged(object sender, EventArgs.RecordChangedEventArgs<DatabaseObjectAutoCleanUpAfter2InsertsTestSqlServerModel> e)
        {
        }

        private static void ModifyTableContent()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([First Name], [Second Name]) VALUES ('AAAA', 'aaaa')";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(100);

                    sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([First Name], [Second Name]) VALUES ('BBBB', 'bbbb')";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(100);
                }
                sqlConnection.Close();
            }
        }
    }
#endif
}