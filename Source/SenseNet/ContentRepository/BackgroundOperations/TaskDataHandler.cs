using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.BackgroundOperations
{
	public static class TaskDataHandler
	{

        private static string ConnectionString
        {
            get { return RepositoryConfiguration.TaskDatabaseConnectionString; }
        }

//        const string REGISTERTASKSQL = @"IF EXISTS (SELECT * FROM Tasks WHERE TaskKey = @TaskKey)
//   SELECT null
//ELSE BEGIN
//	INSERT INTO Tasks (Type, [Order], RegisteredAt, TaskKey, TaskData) VALUES (@Type, @Order, GETUTCDATE(), @TaskKey, @TaskData)
//	SELECT @@IDENTITY
//END";
        const string REGISTERTASKSQL = @"DECLARE @T TABLE (Id int)
DECLARE @Id int
INSERT INTO Tasks (Type, [Order], RegisteredAt, [Hash], TaskData)
OUTPUT INSERTED.Id INTO @T
VALUES (@Type, @Order, GETUTCDATE(), @Hash, @TaskData)
SELECT TOP 1 @Id = Id FROM @T

IF @Id IS NULL BEGIN
	UPDATE Tasks SET [Order] = @Order, TaskData = @TaskData, RegisteredAt = GETUTCDATE()
	OUTPUT INSERTED.Id INTO @T
	WHERE [Hash] = @Hash AND [Order] > @Order
	SELECT TOP 1 @Id = Id FROM @T
END
SELECT @Id
";

		const string DELETETASKSQL = @"DELETE FROM Tasks WHERE Id = @Id";

		const string REFRESHLOCKSQL = @"UPDATE Tasks SET LastLockUpdate = GETUTCDATE() WHERE Id = @Id";

		const string GETANDLOCKSQL = @"UPDATE T SET LockedBy = @LockedBy, LastLockUpdate = GETUTCDATE()
OUTPUT inserted.*
FROM (SELECT TOP 1 * FROM Tasks
	  WHERE (LockedBy IS NULL OR LastLockUpdate < @TimeLimit) AND Type IN ('{0}')
	  ORDER BY [Order], RegisteredAt) T
";

        const string GETDEADTASKSSQL = @"SELECT COUNT(8) FROM Tasks WHERE LockedBy IS NULL OR LastLockUpdate < @TimeLimit";

		public static SnTask RegisterTask(string type, TaskPriority priority, string taskDataSerialized)
		{
			double order = double.NaN;
			switch (priority)
			{
				case TaskPriority.Immediately: order = 1.0; break;
				case TaskPriority.Important: order = 10.0; break;
				case TaskPriority.Normal: order = 100.0; break;
				case TaskPriority.Unimportant: order = 1000.0; break;
				default:
					throw new NotImplementedException("Unknown TaskPriority: " + priority);
			}
			return RegisterTask(type, order, taskDataSerialized);
		}
		private static SnTask RegisterTask(string type, double order, string taskDataSerialized)
		{
            using (var cn = new SqlConnection(ConnectionString))
			{
				using (var cm = new SqlCommand(REGISTERTASKSQL, cn) { CommandType = System.Data.CommandType.Text })
				{
                    var hash = (type + taskDataSerialized).GetHashCode();
					cm.Parameters.Add("@Type", SqlDbType.NVarChar, 450).Value = type;
					cm.Parameters.Add("@Order", SqlDbType.Float).Value = order;
                    cm.Parameters.Add("@Hash", SqlDbType.Int).Value = hash;
                    cm.Parameters.Add("@TaskData", SqlDbType.NText).Value = taskDataSerialized;

					cn.Open();
					var result = cm.ExecuteScalar();
					if (result == DBNull.Value)
						return null;
                    return new SnTask
                    {
                        Id = Convert.ToInt32(result),
                        Order = order,
                        TaskData = taskDataSerialized,
                        RegisteredAt = DateTime.UtcNow,
                        Hash = hash,
                        Type = type,
                    };
				}
			}
		}
		public static void DeleteTask(int id)
		{
			using (var cn = new SqlConnection(ConnectionString))
			{
				using (var cm = new SqlCommand(DELETETASKSQL, cn) { CommandType = System.Data.CommandType.Text })
				{
					cm.Parameters.Add("@Id", SqlDbType.Int).Value = id;

					cn.Open();
					cm.ExecuteNonQuery();
				}
			}
		}
		public static void RefreshLock(int taskId)
		{
			using (var cn = new SqlConnection(ConnectionString))
			{
				using (var cm = new SqlCommand(REFRESHLOCKSQL, cn) { CommandType = System.Data.CommandType.Text })
				{
					cm.Parameters.Add("@Id", SqlDbType.Int).Value = taskId;

					cn.Open();
					cm.ExecuteNonQuery();
				}
			}
		}
		public static SnTask GetNextAndLock(string agentName, string[] capabilities)
		{
			var timeLimit = DateTime.UtcNow.AddSeconds(-RepositoryConfiguration.TaskExecutionTimeoutInSeconds);
            Debug.WriteLine("#TaskManager> GetNextAndLock timelimit: " + timeLimit.ToString("yyyy-MM-dd HH:mm:ss"));
            var sql = String.Format(GETANDLOCKSQL, String.Join("', '", capabilities));
			using (var cn = new SqlConnection(ConnectionString))
			{
                using (var cm = new SqlCommand(sql, cn) { CommandType = System.Data.CommandType.Text })
				{
					cm.Parameters.Add("@LockedBy", SqlDbType.NVarChar, 450).Value = agentName;
					cm.Parameters.Add("@TimeLimit", SqlDbType.DateTime).Value = timeLimit;

					cn.Open();
					var reader = cm.ExecuteReader();
					while (reader.Read())
					{
						return new SnTask
						{
							Id = reader.GetInt32(reader.GetOrdinal("Id")),
							Type = reader.GetString(reader.GetOrdinal("Type")),
							Order = reader.GetDouble(reader.GetOrdinal("Order")),
							RegisteredAt = reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
							LockedBy = GetSafeString(reader, reader.GetOrdinal("LockedBy")),
							LastLockUpdate = GetSafeDateTime(reader, reader.GetOrdinal("LastLockUpdate")),
                            Hash = reader.GetInt32(reader.GetOrdinal("Hash")),
                            TaskData = GetSafeString(reader, reader.GetOrdinal("TaskData")),
                        };
					}
					return null;
				}
			}
		}
        public static int GetDeadTaskCount()
        {
            var timeLimit = DateTime.UtcNow.AddSeconds(-RepositoryConfiguration.TaskExecutionTimeoutInSeconds);
            Debug.WriteLine("#TaskManager> GetDeadTasks, timelimit: " + timeLimit.ToString("yyyy-MM-dd HH:mm:ss"));

            using (var cn = new SqlConnection(ConnectionString))
            using (var cm = new SqlCommand(GETDEADTASKSSQL, cn) { CommandType = System.Data.CommandType.Text })
            {
                cm.Parameters.Add("@TimeLimit", SqlDbType.DateTime).Value = timeLimit;
                cn.Open();
                return (int)cm.ExecuteScalar();
            }
        }


		internal static string Serialize(object data)
		{
			if (data != null)
				return null;

			var serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;

			using (var sw = new StringWriter())
			{
				using (JsonWriter writer = new JsonTextWriter(sw))
					serializer.Serialize(writer, data);
				return sw.GetStringBuilder().ToString();
			}
		}

		private static string GetSafeString(SqlDataReader reader, int index)
		{
			if (reader.IsDBNull(index))
				return null;
			return reader.GetString(index);
		}
		private static DateTime? GetSafeDateTime(SqlDataReader reader, int index)
		{
			if (reader.IsDBNull(index))
				return null;
			return reader.GetDateTime(index);
		}
    }
}