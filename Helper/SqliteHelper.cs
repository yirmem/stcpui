using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace stcpui.Helper;

public class SqliteHelper
{
    // 连接字符串，数据库文件将放在程序基目录下
    private static string _connectionString = "Data Source=stcp.db";

    /// <summary>
    /// 执行非查询SQL（如INSERT, UPDATE, DELETE），返回受影响的行数
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(sql, connection))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                return await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// 执行查询并返回第一行第一列的值
    /// </summary>
    public static async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters = null)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(sql, connection))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                return await command.ExecuteScalarAsync();
            }
        }
    }

    /// <summary>
    /// 执行查询并将结果映射到强类型对象列表
    /// </summary>
    public static async Task<List<T>> QueryAsync<T>(string sql, Dictionary<string, object> parameters = null) where T : new()
    {
        var results = new List<T>();
        
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(sql, connection))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var obj = new T();
                        var properties = typeof(T).GetProperties();
                        
                        foreach (var prop in properties)
                        {
                            try
                            {
                                // 忽略数据库中不存在的字段
                                if (reader.GetOrdinal(prop.Name) < 0) continue;
                                
                                var value = reader[prop.Name] is System.DBNull ? null : reader[prop.Name];
                                prop.SetValue(obj, value);
                            }
                            catch
                            {
                                // 可以记录日志或忽略映射错误
                                continue;
                            }
                        }
                        results.Add(obj);
                    }
                }
            }
        }
        return results;
    }
}