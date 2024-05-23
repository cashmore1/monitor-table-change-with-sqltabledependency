using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;

namespace TableDependency.IntegrationTest.Helpers.SqlServer
{
    internal static class SqlServerHelper
    {
        internal static bool AreAllDbObjectDisposed(string naming)
        {
            // 10 seconds
            Thread.Sleep(10000);
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var connectionString = config.ConnectionStrings.ConnectionStrings["SqlServer2008 sa"].ConnectionString;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"SELECT COUNT(*) FROM sys.objects WITH (NOLOCK) WHERE name = N'tr_{naming}'";
                    var triggerExistis = Convert.ToInt32(sqlCommand.ExecuteScalar());

                    sqlCommand.CommandText = $"SELECT COUNT(*) FROM sys.service_message_types WITH (NOLOCK) WHERE name = N'{naming}_Updated'";
                    var messageExistis = Convert.ToInt32(sqlCommand.ExecuteScalar());

                    sqlCommand.CommandText = $"SELECT COUNT(*) FROM sys.objects WITH (NOLOCK) WHERE name = N'{naming}_QueueActivation'";
                    var procedureExistis = Convert.ToInt32(sqlCommand.ExecuteScalar());

                    return triggerExistis == 0 && messageExistis == 0 && procedureExistis == 0;
                }
            }
        }

        internal static bool AreAllEndpointDisposed(string naming)
        {
            // 10 seconds
            Thread.Sleep(10000);
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var connectionString = config.ConnectionStrings.ConnectionStrings["SqlServer2008 sa"].ConnectionString;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"SELECT COUNT(*) FROM sys.conversation_endpoints WITH (NOLOCK) WHERE [far_service] = '{naming}';";
                    var endpointExistis = Convert.ToInt32(sqlCommand.ExecuteScalar());

                    return endpointExistis == 0;
                }
            }
        }
    }
}