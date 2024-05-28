using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.IntegrationTest.Helpers;
using TableDependency.IntegrationTest.Helpers.SqlServer;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
#if DEBUG
    [TestClass]
    public class DatabaseObjectCleanUpSqlServer
    {
        private static  string ConnectionString;// = ConfigurationManager.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;
        private static string TableName = "DatabaseObjectCleanUpSqlServer";
        public static string _dbObjectsNaming;
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionString = config.ConnectionStrings.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;
            Console.WriteLine($"Using ConnetionString {ConnectionString}");
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
            }
        }
        [TestInitialize]
        public void TestInitialize()
        {
            Console.WriteLine(TestContext.DeploymentDirectory);
        }

        [TestCleanup]
        public void EndTest()
        {
            Console.WriteLine(TestContext.TestName);
            Console.WriteLine(TestContext.CurrentTestOutcome);


        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void DatabaseObjectCleanUpTest()
        {
            var tableDependency = new SqlTableDependency<EventForAllColumnsTestSqlServerModel>(ConnectionString, TableName);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.Start();
            _dbObjectsNaming = tableDependency.DataBaseObjectsNamingConvention;

            Thread.Sleep(5000);
            
            tableDependency.StopWithoutDisposing();

            Thread.Sleep(4 * 60 * 1000);
            Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(_dbObjectsNaming));
            Assert.IsTrue(SqlServerHelper.AreAllEndpointDisposed(_dbObjectsNaming));
            Assert.IsFalse(_dbObjectsNaming.Contains(Constants.NAMINGTOKEN), $"The naming convention of [ {Constants.NAMINGTOKEN} ] was found in the object naming where it doesn't belong.");
        }

        private void TableDependency_OnChanged(object sender, EventArgs.RecordChangedEventArgs<EventForAllColumnsTestSqlServerModel> e)
        {
        }
    }
#endif
}