using System.Data;
using FinanceTracker.Domain.UnitOfWorks;
using Microsoft.Data.SqlClient;

namespace FinanceTracker.Infrastructure.UnitOfWorks;

public abstract class UnitOfWork<TDbConnection> : IUnitOfWork where TDbConnection : class, IDbConnection, new()
{
    private TDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public string? ConnectionString { get; protected set; }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = new TDbConnection { ConnectionString = ConnectionString };
                _connection.Open();
            }
            else if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            return _connection;
        }
    }

    public IDbTransaction? Transaction => _transaction;

    public void BeginTransaction()
    {
        if (_transaction == null)
        {
            _transaction = Connection.BeginTransaction();
        }
    }

    public void Commit()
    {
        try
        {
            _transaction?.Commit();
        }
        catch
        {
            _transaction?.Rollback();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public void Rollback()
    {
        try
        {
            _transaction?.Rollback();
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
                _transaction = null;
                _connection = null;
            }
            _disposed = true;
        }
    }
}
