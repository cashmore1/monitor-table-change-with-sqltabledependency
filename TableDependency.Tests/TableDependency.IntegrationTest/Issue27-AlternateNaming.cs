﻿using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.EventArgs;
using TableDependency.IntegrationTest.Helpers;
using TableDependency.IntegrationTest.Helpers.SqlServer;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
    //internal class Issue27Model
    //{
    //    public string Id { get; set; }
    //    public string Message { get; set; }
    //}

    [TestClass]
    public class Issue27AlternateNaming
    {
        private const string TableName = "Issue27Model";
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('[{TableName}]', 'U') IS NOT NULL DROP TABLE [dbo].[{TableName}]";
                    sqlCommand.ExecuteNonQuery();
                    sqlCommand.CommandText = $"CREATE TABLE [{TableName}]([Id] [int] NULL, [Message] [VARCHAR](100) NULL)";
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


        public TestContext TestContext { get; set; }


        [TestCategory("SqlServer")]
        [TestMethod]
        public void Issue27Tesst()
        {
            try
            {
                string objectNaming;

                using (var tableDependency = new SqlTableDependency<Issue27Model>(ConnectionString, TableName, dataBaseObjectNamePrefix: Constants.NAMINGTOKEN))
                {
                    tableDependency.OnChanged += TableDependency_Changed;
                    tableDependency.Start();
                    objectNaming = tableDependency.DataBaseObjectsNamingConvention;

                    Thread.Sleep(5000);                    
                }

                Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(objectNaming));
                Assert.IsTrue(SqlServerHelper.AreAllEndpointDisposed(objectNaming));
                Assert.IsTrue(objectNaming.Contains(Constants.NAMINGTOKEN), $"The naming convention of [ {Constants.NAMINGTOKEN} ] was not found in the object naming where it belong.");
            }
            catch (Exception exception)
            {
                TestContext.WriteLine(exception.Message);
                Assert.Fail();
            }
        }

        private static void TableDependency_Changed(object sender, RecordChangedEventArgs<Issue27Model> e)
        {

        }
    }
}