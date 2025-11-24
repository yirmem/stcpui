using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace stcpui.Repository;

public interface IRepository<T>
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<int> InsertAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<int> UpSertAsync(T entity);
    Task<bool> DeleteAsync(int id);
}
public class BaseRepository<T> : IRepository<T> where T : class
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    public BaseRepository(IDbConnection connection)
    {
        _connection = connection;
        _tableName = typeof(T).Name;
    }

    // 异步方法实现
    public async Task<T> GetByIdAsync(int id)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var sql = $"SELECT * FROM {_tableName}";
        return await _connection.QueryAsync<T>(sql);
    }

    public async Task<int> InsertAsync(T entity)
    {
        // 动态构建插入语句
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id" && p.CanWrite);
        
        var columnNames = properties.Select(p => p.Name);
        var parameterNames = properties.Select(p => "@" + p.Name);
        
        var sql = $"INSERT INTO {_tableName} ({string.Join(", ", columnNames)}) " +
                 $"VALUES ({string.Join(", ", parameterNames)}); " +
                 "SELECT last_insert_rowid();";
        
        return await _connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        // 动态构建更新语句
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id" && p.CanWrite);
        
        var setClause = string.Join(", ", 
            properties.Select(p => $"{p.Name} = @{p.Name}"));
        
        var sql = $"UPDATE {_tableName} SET {setClause} WHERE Id = @Id";
        
        var affectedRows = await _connection.ExecuteAsync(sql, entity);
        return affectedRows > 0;
    }

    public async Task<int> UpSertAsync(T entity)
    {
        var updated = await UpdateAsync(entity);
        if (updated)
        {
            return 1;
        }
    
        // 如果更新失败（记录不存在），则插入
        return await InsertAsync(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";
        var affectedRows = await _connection.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }

    // 同步方法实现（如果接口中有定义）
    public T GetById(int id)
    {
        return GetByIdAsync(id).GetAwaiter().GetResult();
    }

    public IEnumerable<T> GetAll()
    {
        return GetAllAsync().GetAwaiter().GetResult();
    }

    public int Insert(T entity)
    {
        return InsertAsync(entity).GetAwaiter().GetResult();
    }

    public bool Update(T entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult();
    }

    public bool Delete(int id)
    {
        return DeleteAsync(id).GetAwaiter().GetResult();
    }
}